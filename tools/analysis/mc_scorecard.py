#!/usr/bin/env python3
"""Bundled scorecard queries (docs/backlog.md items 1-21, 12a).

Usage: python3 scorecard.py <run-dir> [<run-dir> ...]
Each run-dir may be a completed run or a mid-run stream/ directory; tables
are unioned across dirs. Sections whose tables are absent everywhere are
skipped (streamed dirs lack the heavyweight event tables).

Fixed window sizes (2500/5000 ticks) are sized for the post-unification
20k-tick horizon: 8 fine / 4 coarse windows per run, stable numbering
across mid-run scoring. Pre-unification runs (100k raw ticks) used
12500/25000; divide old-record tick figures by 5 when comparing.
"""
import os
import sys
import duckdb

DIRS = [d.rstrip("/") for d in sys.argv[1:]]

con = duckdb.connect()

TABLES = {
    "ticks": "tick_snapshots",
    "ents": "entity_snapshots",
    "ee": "entity_events",
    "ge": "gambit_events",
    "gen": "generator_events",
    "sym": "symbols",
    "cfs": "culture_field_cell_snapshots",
    "rel": "relationship_events",
    "move": "movement_events",
}

for view, table in TABLES.items():
    files = [f"{d}/{table}.parquet" for d in DIRS if os.path.exists(f"{d}/{table}.parquet")]
    if not files:
        print(f"note: {table} absent in all dirs; dependent sections will skip")
        continue
    file_list = ", ".join(f"'{f}'" for f in files)
    if view == "sym":
        # symbols carries one duplicate row per kernel run (no run_id
        # column); joins against it multiply counts unless deduplicated.
        con.execute(
            f"CREATE VIEW {view} AS SELECT DISTINCT symbol_kind, id, stable_name "
            f"FROM read_parquet([{file_list}], union_by_name=true)"
        )
    else:
        con.execute(f"CREATE VIEW {view} AS SELECT * FROM read_parquet([{file_list}], union_by_name=true)")


def symkind(probe_name):
    row = con.execute(
        "SELECT symbol_kind FROM sym WHERE stable_name = ? LIMIT 1", [probe_name]
    ).fetchone()
    if row is None:
        raise SystemExit(f"symbol probe not found: {probe_name}")
    return row[0]


SK_KIND = symkind("point_of_interest")
SK_GAMBIT = symkind("settlement_deepens_the_holding")
SK_GENERATOR = symkind("the_seal_shatters")
SK_REL = symkind("holds")
SK_SOURCE = symkind("map_control")


def q(title, sql):
    print(f"\n### {title}")
    sql = (sql.replace("symbol_kind = 1", f"symbol_kind = {SK_KIND}")
              .replace("symbol_kind = 3", f"symbol_kind = {SK_GAMBIT}")
              .replace("symbol_kind = 4", f"symbol_kind = {SK_GENERATOR}")
              .replace("symbol_kind = 2", f"symbol_kind = {SK_REL}")
              .replace("symbol_kind = 6", f"symbol_kind = {SK_SOURCE}"))
    try:
        print(con.execute(sql).df().to_string(index=False))
    except Exception as exc:  # noqa: BLE001 - report and continue
        print(f"QUERY FAILED: {exc}")


HORIZON = con.execute("SELECT max(tick) FROM ticks").fetchone()[0]
WINDOWS = f"(SELECT unnest([{HORIZON//4}, {HORIZON//2}, {3*HORIZON//4}, {HORIZON}]) AS w)"
print(f"run dirs: {DIRS}  horizon: {HORIZON}")

# --- 1. Density plateau: active/covered 0.3-0.5 at horizon -----------------
q("1. Density (active/covered) trajectory", f"""
SELECT t.run_id, w.w AS tick, t.active_entities, t.covered_cells,
       ROUND(t.active_entities * 1.0 / NULLIF(t.covered_cells,0), 3) AS density
FROM ticks t JOIN {WINDOWS} w ON t.tick = w.w ORDER BY t.run_id, w.w
""")

# --- 2. Mortal share of active population ----------------------------------
q("2. Active population by kind at horizon", f"""
SELECT s.stable_name AS kind, e.run_id, count(*) AS active
FROM ents e JOIN sym s ON s.id = e.kind_id AND s.symbol_kind = 1
WHERE e.tick = {HORIZON} AND e.status_code = 1
GROUP BY 1, 2 ORDER BY 1, 2
""")

# --- 3. Artifact/POI net convergence ---------------------------------------
q("3. Net creation-retirement per 2.5k window (artifact/POI)", f"""
SELECT s.stable_name AS kind, e.run_id, (e.tick / 2500)::INT AS win,
       SUM(CASE WHEN e.event_type_code = 1 THEN 1 ELSE 0 END) AS created,
       SUM(CASE WHEN e.event_type_code = 2 THEN 1 ELSE 0 END) AS retired
FROM ee e JOIN sym s ON s.id = e.kind_id AND s.symbol_kind = 1
WHERE s.stable_name IN ('artifact','point_of_interest')
GROUP BY 1, 2, 3 ORDER BY 1, 2, 3
""")

# --- 4/20. Lifespans by kind ------------------------------------------------
q("4/20. Lifespan distribution of retired entities by kind", f"""
WITH born AS (SELECT run_id, entity_id, min(tick) AS b FROM ee
              WHERE current_status_code = 1 GROUP BY 1, 2),
     died AS (SELECT run_id, entity_id, kind_id, min(tick) AS d FROM ee
              WHERE current_status_code = 2 GROUP BY 1, 2, 3)
SELECT s.stable_name AS kind, count(*) AS deaths,
       ROUND(avg(d.d - b.b)) AS avg_life, ROUND(median(d.d - b.b)) AS med_life,
       ROUND(quantile_cont(d.d - b.b, 0.9)) AS p90_life
FROM died d JOIN born b USING (run_id, entity_id)
JOIN sym s ON s.id = d.kind_id AND s.symbol_kind = 1
GROUP BY 1 ORDER BY deaths DESC
""")

# --- 5/12a. Endings + district attribution ---------------------------------
q("5. Ending-generator fires (attribution)", """
SELECT s.stable_name AS generator, g.run_id, count(*) AS fires
FROM gen g JOIN sym s ON s.id = g.generator_id AND s.symbol_kind = 4
WHERE g.event_type_code = 12
  AND s.stable_name IN ('the_seal_shatters','one_word_yields','memorials_join_the_wall',
                        'the_tome_is_read','the_offering_is_spent')
GROUP BY 1, 2 ORDER BY 1, 2
""")
q("5/12a/18. Stage & district gambit executions (attribution)", """
SELECT s.stable_name AS gambit, g.succeeded, count(*) AS n
FROM ge g JOIN sym s ON s.id = g.gambit_id AND s.symbol_kind = 3
WHERE s.stable_name LIKE '%deepens_the_holding%'
   OR s.stable_name LIKE '%crowns_the_holding%'
   OR s.stable_name IN ('settlement_the_hold_withers','artifact_slips_the_grasp',
                        'artifact_enshrines_itself','faction_razes_the_hollow_hold',
                        'settlement_wakes_the_sleeper')
GROUP BY 1, 2 ORDER BY 1, 2
""")

# --- 6. Wild vs held artifacts at horizon ----------------------------------
q("6. Artifact holds at horizon (Holds edges alive)", f"""
WITH holds AS (
  SELECT r.run_id, r.target_id,
         SUM(CASE WHEN r.change_code_id = 1 THEN 1
                  WHEN r.change_code_id = 2 THEN -1 ELSE 0 END) AS alive
  FROM rel r JOIN sym s ON s.id = r.relationship_kind_id AND s.symbol_kind = 2
  WHERE s.stable_name = 'holds' GROUP BY 1, 2)
SELECT e.run_id,
       SUM(CASE WHEN COALESCE(h.alive,0) > 0 THEN 1 ELSE 0 END) AS held,
       SUM(CASE WHEN COALESCE(h.alive,0) <= 0 THEN 1 ELSE 0 END) AS wild
FROM ents e JOIN sym k ON k.id = e.kind_id AND k.symbol_kind = 1
LEFT JOIN holds h ON h.run_id = e.run_id AND h.target_id = e.entity_id
WHERE e.tick = {HORIZON} AND e.status_code = 1 AND k.stable_name = 'artifact'
GROUP BY 1 ORDER BY 1
""")

# --- 9. Semantic stacking ---------------------------------------------------
q("9. Semantic plane stacking at horizon", f"""
SELECT run_id, count(*) AS actives, count(DISTINCT (semantic_q, semantic_r)) AS cells,
       ROUND(count(*) * 1.0 / count(DISTINCT (semantic_q, semantic_r)), 2) AS per_cell,
       max(cnt) AS worst_stack
FROM (SELECT run_id, semantic_q, semantic_r,
             count(*) OVER (PARTITION BY run_id, semantic_q, semantic_r) AS cnt
      FROM ents WHERE tick = {HORIZON} AND status_code = 1)
GROUP BY run_id ORDER BY run_id
""")

# --- 10. Provincial pulse tempo --------------------------------------------
q("10. Generator fires per 2.5k window", """
SELECT run_id, (tick / 2500)::INT AS win, count(*) AS fires
FROM gen WHERE event_type_code = 12 GROUP BY 1, 2 ORDER BY 1, 2
""")

# --- 12. Relationship lifetime totals --------------------------------------
q("12. Relationship lifetime totals vs 65,536 cap", """
SELECT run_id, SUM(CASE WHEN change_code_id = 1 THEN 1 ELSE 0 END) AS created_total
FROM rel GROUP BY 1 ORDER BY 1
""")
q("12b. Gambit cooldown lifetime pairs vs 65,536 capacity", """
SELECT run_id, count(DISTINCT (actor_id, gambit_id)) AS cooldown_pairs
FROM ge GROUP BY 1 ORDER BY 1
""")

# --- 17. Cultural mass headline --------------------------------------------
# Post-parallelization telemetry replaced per-event influence deltas with
# periodic field snapshots; the per-source-lane income statement is no longer
# recorded. The headline is judged from the snapshot mass trajectory instead.
q("17. Total physical raw mass trajectory (field snapshots)", """
SELECT run_id, tick, ROUND(SUM(raw_influence)) AS total_mass,
       count(DISTINCT (q, r)) AS cells_with_mass
FROM cfs WHERE plane_code = 1 AND tick % 2500 = 0
GROUP BY 1, 2 ORDER BY 1, 2
""")
q("17b. Physical mass by culture at horizon", f"""
SELECT run_id, culture_id, ROUND(SUM(raw_influence)) AS mass
FROM cfs WHERE plane_code = 1 AND tick = {HORIZON}
GROUP BY 1, 2 ORDER BY 1, 3 DESC
""")
q("17c. Culture concentration (top-culture mass share) per plane per 2.5k window", """
WITH m AS (
  SELECT run_id, plane_code, (tick / 2500)::INT AS win, culture_id,
         SUM(raw_influence) AS mass
  FROM cfs WHERE tick % 2500 = 0
  GROUP BY 1, 2, 3, 4)
SELECT run_id, plane_code, win,
       ROUND(MAX(mass) / NULLIF(SUM(mass), 0), 2) AS top_share,
       arg_max(culture_id, mass) AS top_culture
FROM m GROUP BY 1, 2, 3 ORDER BY 1, 2, 3
""")

# --- 18. Stage pyramid (reconstructed from promotion/demotion ledger) ------
q("18. Stage distribution at horizon (ledger-reconstructed)", f"""
WITH promo AS (
  SELECT g.run_id, g.actor_id AS eid, count(*) AS ups
  FROM ge g JOIN sym s ON s.id = g.gambit_id AND s.symbol_kind = 3
  WHERE (s.stable_name LIKE '%deepens_the_holding%' OR s.stable_name LIKE '%crowns_the_holding%')
    AND g.succeeded GROUP BY 1, 2),
demo AS (
  SELECT run_id, eid, count(*) AS downs FROM (
    SELECT g.run_id, g.actor_id AS eid
    FROM ge g JOIN sym s ON s.id = g.gambit_id AND s.symbol_kind = 3
    WHERE s.stable_name = 'settlement_the_hold_withers' AND g.succeeded
    UNION ALL
    SELECT n.run_id, n.selected_target_entity_id AS eid
    FROM gen n JOIN sym s ON s.id = n.generator_id AND s.symbol_kind = 4
    WHERE s.stable_name = 'the_seal_shatters' AND n.event_type_code = 12)
  GROUP BY 1, 2)
SELECT k.stable_name AS kind,
       GREATEST(0, LEAST(2, COALESCE(p.ups,0) - COALESCE(d.downs,0))) AS stage,
       count(*) AS n
FROM ents e JOIN sym k ON k.id = e.kind_id AND k.symbol_kind = 1
LEFT JOIN promo p ON p.run_id = e.run_id AND p.eid = e.entity_id
LEFT JOIN demo d ON d.run_id = e.run_id AND d.eid = e.entity_id
WHERE e.tick = {HORIZON} AND e.status_code = 1
  AND k.stable_name IN ('settlement','faction','artifact','canon')
GROUP BY 1, 2 ORDER BY 1, 2
""")

# --- 19. Deepen family share of executions ---------------------------------
q("19. Deepen-family share of gambit executions", """
WITH ex AS (SELECT g.*, s.stable_name FROM ge g
            JOIN sym s ON s.id = g.gambit_id AND s.symbol_kind = 3)
SELECT run_id, count(*) AS total_execs,
       SUM(CASE WHEN stable_name LIKE '%deepens_the_holding%' THEN 1 ELSE 0 END) AS deepen,
       ROUND(SUM(CASE WHEN stable_name LIKE '%deepens_the_holding%' THEN 1 ELSE 0 END) * 100.0
             / count(*), 1) AS deepen_pct
FROM ex GROUP BY 1 ORDER BY 1
""")
q("19b. Top gambits by executions", """
SELECT s.stable_name AS gambit, count(*) AS execs,
       ROUND(avg(CASE WHEN g.succeeded THEN 1.0 ELSE 0 END), 2) AS success_rate
FROM ge g JOIN sym s ON s.id = g.gambit_id AND s.symbol_kind = 3
GROUP BY 1 ORDER BY execs DESC LIMIT 20
""")

# --- 7. Movement volume by kind --------------------------------------------
q("7. Movement events per kind per 5k window", f"""
WITH kinds AS (SELECT run_id, entity_id, min(kind_id) AS kind_id
               FROM ee WHERE event_type_code = 1 GROUP BY 1, 2)
SELECT s.stable_name AS kind, (m.tick / 5000)::INT AS win, count(*) AS moves
FROM move m
JOIN kinds k ON k.run_id = m.run_id AND k.entity_id = m.entity_id
JOIN sym s ON s.id = k.kind_id AND s.symbol_kind = 1
GROUP BY 1, 2 ORDER BY 1, 2
""")
