# The Road Problem

Road-network findings from Monte Carlo run `movement-cost-experiment-10x500`.

The current road system does not yet produce road networks that read like a
real web of travel corridors. It produces two different failures:

1. At very low quality, road traces smear across large parts of the map.
2. At meaningful quality thresholds, the road graph collapses into tiny
   entity-adjacent patches.

## Findings

### 1. Weak traces form blobs, not roads

At `current_quality > 0`, road traces are connected into large weak components:

| Plane | Avg road cells/run | Avg components/run | Largest component share |
| --- | ---: | ---: | ---: |
| Physical | 653.3 | 8.7 | 54.8% |
| Semantic | 597.0 | 1.5 | 97.5% |

This is not a useful road network. The semantic plane in particular forms one
giant low-quality smear.

### 2. Meaningful roads do not form a network

At `current_quality > 10`, the road graph almost disappears:

| Plane | Avg cells/run | Avg components/run | Avg largest component |
| --- | ---: | ---: | ---: |
| Physical | 5.8 | 3.2 | 2.9 cells |
| Semantic | 7.2 | 3.3 | 3.4 cells |

At `current_quality > 25`, physical averages only `1.2` cells/run and semantic
averages `0`.

This means there are not stable hub-to-hub routes, corridors, loops, or network
branches at the quality levels that should matter.

### 3. Meaningful roads are clustered around entities

For `current_quality > 10`, meaningful road cells are very close to final active
entity positions:

| Plane | Within 2 hexes of any entity | Within 5 hexes of any entity |
| --- | ---: | ---: |
| Physical | 79.3% | 100% |
| Semantic | 79.2% | 100% |

Stable-anchor clustering is weaker:

| Plane | Stable anchor definition | Within 2 hexes |
| --- | --- | ---: |
| Physical | settlement | 48.3% |
| Semantic | penguin or artifact | 40.3% |

The roads are therefore mostly local scars around where entities end up, not
paths connecting stable anchors.

### 4. Physical mature roads are mostly oscillation

The strongest physical edges are not shared highways. They are repeated
back-and-forth movement by a single entity:

| Plane | Top repeated edge travel | Entity diversity |
| --- | ---: | ---: |
| Physical | 544 undirected / 272 each direction | 1 transport |
| Semantic | 17 undirected | low/no mature concentration |

This suggests that high-quality physical roads are currently produced by local
shuttling, not by broad network use.

### 5. Semantic roads are still worse than physical roads

The movement-cost experiment slowed both physical and semantic movement, but it
did not create semantic roads. Semantic still has no `quality > 25` road cells,
no mature repeated edges, and no meaningful road web.

The likely missing ingredient is not just road speed. Physical entities have
some settlement convergence. Semantic entities do not yet converge strongly
around comparable stable anchors such as penguins and artifacts.

## Interpretation

The road model is currently detecting movement, but the simulation is not
producing the repeated, shared, anchor-to-anchor movement patterns that a road
network requires.

The current output looks like:

1. Broad weak trace smearing from ordinary movement.
2. Tiny local road patches around entities.
3. Occasional physical high-quality cells caused by one-entity oscillation.
4. Almost no semantic mature-road formation.

The next road work should focus on movement targeting and stable convergence,
especially semantic targeting around durable semantic anchors. Road-speed tuning
alone is unlikely to solve the network problem.

## Relevant Queries

The queries below assume the MC run artifacts are available at:

```bash
RUN_DIR=.artifacts/monte-carlo/movement-cost-experiment-10x500
DB=$RUN_DIR/telemetry.duckdb
```

### Road cells by quality threshold

```sql
-- Final road quality per cell, summarized by plane and threshold.
with latest_cell_roads as (
    select
        run_id,
        plane_code,
        q,
        r,
        current_quality,
        travel_count,
        row_number() over (
            partition by run_id, plane_code, q, r
            order by tick desc
        ) as rn
    from cell_road_events
),
final_cell_roads as (
    select run_id, plane_code, q, r, current_quality, travel_count
    from latest_cell_roads
    where rn = 1
),
thresholded as (
    select
        run_id,
        plane_code,
        count(*) filter (where current_quality > 0) as cells_gt_0,
        count(*) filter (where current_quality > 10) as cells_gt_10,
        count(*) filter (where current_quality > 25) as cells_gt_25,
        max(current_quality) as max_quality
    from final_cell_roads
    group by run_id, plane_code
)
select
    plane_code,
    avg(cells_gt_0) as avg_cells_gt_0,
    avg(cells_gt_10) as avg_cells_gt_10,
    avg(cells_gt_25) as avg_cells_gt_25,
    max(max_quality) as max_quality
from thresholded
group by plane_code
order by plane_code;
```

### Entity proximity for meaningful roads

```sql
-- Distance from final road cells to final active entities.
-- Active status is status_code = 1 in the current telemetry.
with latest_cell_roads as (
    select
        run_id,
        plane_code,
        q,
        r,
        current_quality,
        row_number() over (
            partition by run_id, plane_code, q, r
            order by tick desc
        ) as rn
    from cell_road_events
),
roads as (
    select run_id, plane_code, q, r, current_quality
    from latest_cell_roads
    where rn = 1 and current_quality > 10
),
final_ticks as (
    select run_id, max(tick) as final_tick
    from entity_snapshots
    group by run_id
),
final_entities as (
    select e.*
    from entity_snapshots e
    join final_ticks f
        on f.run_id = e.run_id and f.final_tick = e.tick
    where e.status_code = 1
),
entity_locations as (
    select run_id, 1 as plane_code, entity_id, kind_id,
           physical_q as q, physical_r as r
    from final_entities
    union all
    select run_id, 2 as plane_code, entity_id, kind_id,
           semantic_q as q, semantic_r as r
    from final_entities
),
nearest as (
    select
        roads.run_id,
        roads.plane_code,
        roads.q,
        roads.r,
        min(greatest(
            abs(roads.q - entity_locations.q),
            abs(roads.r - entity_locations.r),
            abs((roads.q + roads.r) - (entity_locations.q + entity_locations.r))
        )) as nearest_entity_distance
    from roads
    join entity_locations
        on entity_locations.run_id = roads.run_id
       and entity_locations.plane_code = roads.plane_code
    group by roads.run_id, roads.plane_code, roads.q, roads.r
)
select
    plane_code,
    count(*) as road_cells,
    avg(case when nearest_entity_distance <= 2 then 1.0 else 0.0 end) as share_within_2,
    avg(case when nearest_entity_distance <= 5 then 1.0 else 0.0 end) as share_within_5
from nearest
group by plane_code
order by plane_code;
```

### Stable-anchor proximity

```sql
-- Physical stable anchors: settlements.
-- Semantic stable anchors: penguins and artifacts.
with latest_cell_roads as (
    select
        run_id,
        plane_code,
        q,
        r,
        current_quality,
        row_number() over (
            partition by run_id, plane_code, q, r
            order by tick desc
        ) as rn
    from cell_road_events
),
roads as (
    select run_id, plane_code, q, r, current_quality
    from latest_cell_roads
    where rn = 1 and current_quality > 10
),
final_ticks as (
    select run_id, max(tick) as final_tick
    from entity_snapshots
    group by run_id
),
final_entities as (
    select e.*
    from entity_snapshots e
    join final_ticks f
        on f.run_id = e.run_id and f.final_tick = e.tick
    where e.status_code = 1
),
anchors as (
    select run_id, 1 as plane_code, entity_id, kind_id,
           physical_q as q, physical_r as r
    from final_entities
    where kind_id = 2 -- settlement
    union all
    select run_id, 2 as plane_code, entity_id, kind_id,
           semantic_q as q, semantic_r as r
    from final_entities
    where kind_id in (4, 5) -- penguin, artifact
),
nearest as (
    select
        roads.run_id,
        roads.plane_code,
        roads.q,
        roads.r,
        min(greatest(
            abs(roads.q - anchors.q),
            abs(roads.r - anchors.r),
            abs((roads.q + roads.r) - (anchors.q + anchors.r))
        )) as nearest_anchor_distance
    from roads
    join anchors
        on anchors.run_id = roads.run_id
       and anchors.plane_code = roads.plane_code
    group by roads.run_id, roads.plane_code, roads.q, roads.r
)
select
    plane_code,
    count(*) as road_cells,
    avg(case when nearest_anchor_distance <= 2 then 1.0 else 0.0 end) as share_within_2,
    avg(case when nearest_anchor_distance <= 5 then 1.0 else 0.0 end) as share_within_5
from nearest
group by plane_code
order by plane_code;
```

### Repeated-edge concentration

```sql
-- Finds the most reused undirected movement edges and whether they are shared.
with canonical_edges as (
    select
        run_id,
        plane_code,
        entity_id,
        case
            when from_q < to_q or (from_q = to_q and from_r <= to_r)
                then from_q else to_q
        end as aq,
        case
            when from_q < to_q or (from_q = to_q and from_r <= to_r)
                then from_r else to_r
        end as ar,
        case
            when from_q < to_q or (from_q = to_q and from_r <= to_r)
                then to_q else from_q
        end as bq,
        case
            when from_q < to_q or (from_q = to_q and from_r <= to_r)
                then to_r else from_r
        end as br
    from movement_events
    where distance > 0
),
edge_counts as (
    select
        run_id,
        plane_code,
        aq,
        ar,
        bq,
        br,
        count(*) as travel_count,
        count(distinct entity_id) as distinct_entities
    from canonical_edges
    group by run_id, plane_code, aq, ar, bq, br
),
entity_kinds as (
    select distinct
        e.run_id,
        e.entity_id,
        s.stable_name as kind_name
    from entity_snapshots e
    join symbols s
        on s.symbol_kind = 1 and s.id = e.kind_id
)
select
    edge_counts.run_id,
    edge_counts.plane_code,
    edge_counts.aq,
    edge_counts.ar,
    edge_counts.bq,
    edge_counts.br,
    edge_counts.travel_count,
    edge_counts.distinct_entities,
    string_agg(distinct entity_kinds.kind_name, ', ' order by entity_kinds.kind_name) as kinds
from edge_counts
join canonical_edges
    on canonical_edges.run_id = edge_counts.run_id
   and canonical_edges.plane_code = edge_counts.plane_code
   and canonical_edges.aq = edge_counts.aq
   and canonical_edges.ar = edge_counts.ar
   and canonical_edges.bq = edge_counts.bq
   and canonical_edges.br = edge_counts.br
join entity_kinds
    on entity_kinds.run_id = canonical_edges.run_id
   and entity_kinds.entity_id = canonical_edges.entity_id
group by
    edge_counts.run_id,
    edge_counts.plane_code,
    edge_counts.aq,
    edge_counts.ar,
    edge_counts.bq,
    edge_counts.br,
    edge_counts.travel_count,
    edge_counts.distinct_entities
order by travel_count desc
limit 25;
```

### Road graph input rows

```sql
-- Final road cells suitable for connected-component inspection by hex adjacency.
with latest_cell_roads as (
    select
        run_id,
        plane_code,
        q,
        r,
        current_quality,
        row_number() over (
            partition by run_id, plane_code, q, r
            order by tick desc
        ) as rn
    from cell_road_events
)
select run_id, plane_code, q, r, current_quality
from latest_cell_roads
where rn = 1 and current_quality > 10
order by run_id, plane_code, q, r;
```
