#!/usr/bin/env python3
"""Generate representative NameForge review samples as CSV.

The script uses the NameForge CLI instead of duplicating runtime generation
logic. It discovers routed kind/subkind/profile combinations from the active
Ice Remembers profile files, then appends explicit causal-loop/context cases.
"""

from __future__ import annotations

import argparse
import csv
import json
import subprocess
import sys
from dataclasses import dataclass, field
from pathlib import Path
from typing import Iterable


DEFAULT_PROJECT = Path("src/IceRemembers/Names/ice-remembers.nameforge.json")
DEFAULT_OUTPUT = Path("reviews/name-review-samples.csv")


@dataclass(frozen=True)
class SampleRequest:
    culture: str
    kind: str
    subkind: str = ""
    scenario: str = "profile_route"
    profile: str = ""
    group: str = ""
    prominence: str = "recognized"
    tags: tuple[str, ...] = field(default_factory=tuple)
    context: tuple[tuple[str, str], ...] = field(default_factory=tuple)

    @property
    def is_contextual(self) -> bool:
        return bool(self.context)


def main() -> int:
    args = parse_args()
    repo_root = Path(__file__).resolve().parents[1]
    project_path = resolve_repo_path(repo_root, args.project)
    output_path = resolve_repo_path(repo_root, args.output)
    cultures = parse_culture_filter(args.culture)

    if not project_path.exists():
        print(f"Project file not found: {project_path}", file=sys.stderr)
        return 2

    cli = resolve_nameforge_cli(repo_root, args.no_build)
    requests = discover_profile_routes(project_path)
    requests.extend(build_context_requests())

    if cultures:
        requests = [request for request in requests if request.culture in cultures]
    if args.only_context:
        requests = [request for request in requests if request.is_contextual]
    if args.only_profile_routes:
        requests = [request for request in requests if not request.is_contextual]

    requests = dedupe_requests(requests)
    if args.limit_routes is not None:
        requests = requests[: args.limit_routes]

    output_path.parent.mkdir(parents=True, exist_ok=True)
    rows_written = write_samples(repo_root, project_path, output_path, cli, requests, args)
    print(
        f"Wrote {rows_written} rows from {len(requests)} NameForge requests to {output_path}",
        file=sys.stderr,
    )
    return 0


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Generate CSV samples for all implemented Ice Remembers NameForge routes."
    )
    parser.add_argument(
        "--project",
        default=str(DEFAULT_PROJECT),
        help=f"NameForge project JSON. Default: {DEFAULT_PROJECT}",
    )
    parser.add_argument(
        "--output",
        default=str(DEFAULT_OUTPUT),
        help=f"CSV output path, or '-' for stdout. Default: {DEFAULT_OUTPUT}",
    )
    parser.add_argument(
        "--count",
        type=int,
        default=12,
        help="Names per discovered profile route. Default: 12.",
    )
    parser.add_argument(
        "--context-count",
        type=int,
        default=None,
        help="Names per explicit context/lineage route. Defaults to --count.",
    )
    parser.add_argument(
        "--culture",
        action="append",
        default=[],
        help="Culture id to include. Repeat or comma-separate. Default: all cultures.",
    )
    parser.add_argument(
        "--seed-prefix",
        default="name-review",
        help="Prefix for deterministic seeds. Default: name-review.",
    )
    parser.add_argument(
        "--no-build",
        action="store_true",
        help="Skip building tools/NameForge first. Useful when a current CLI DLL already exists.",
    )
    parser.add_argument(
        "--fail-fast",
        action="store_true",
        help="Abort on the first NameForge CLI error instead of writing error rows.",
    )
    parser.add_argument(
        "--only-context",
        action="store_true",
        help="Only generate explicit context/lineage review scenarios.",
    )
    parser.add_argument(
        "--only-profile-routes",
        action="store_true",
        help="Only generate discovered profile routes, excluding explicit context/lineage scenarios.",
    )
    parser.add_argument(
        "--limit-routes",
        type=int,
        default=None,
        help="Debug option: only run the first N discovered requests.",
    )
    args = parser.parse_args()
    if args.only_context and args.only_profile_routes:
        parser.error("--only-context and --only-profile-routes cannot be used together")
    return args


def resolve_repo_path(repo_root: Path, value: str | Path) -> Path:
    value_path = Path(value)
    if str(value_path) == "-":
        return value_path
    return value_path if value_path.is_absolute() else repo_root / value_path


def parse_culture_filter(values: Iterable[str]) -> set[str]:
    cultures: set[str] = set()
    for value in values:
        cultures.update(part.strip() for part in value.split(",") if part.strip())
    return cultures


def load_json(path: Path) -> dict:
    with path.open("r", encoding="utf-8") as handle:
        data = json.load(handle)
    if not isinstance(data, dict):
        raise ValueError(f"Expected JSON object in {path}")
    return data


def as_strings(value: object) -> list[str]:
    if value is None:
        return []
    if isinstance(value, list):
        return [str(item).strip() for item in value if str(item).strip()]
    text = str(value).strip()
    return [text] if text else []


def discover_profile_routes(project_path: Path) -> list[SampleRequest]:
    manifest = load_json(project_path)
    root = project_path.parent
    requests: list[SampleRequest] = []

    for include in manifest.get("includes", []):
        if not isinstance(include, dict) or include.get("type") != "profile":
            continue

        culture = str(include.get("cultureId", "")).strip()
        profile_path = root / str(include.get("path", ""))
        if not culture or not profile_path.exists():
            continue

        profile = load_json(profile_path)
        profile_id = str(profile.get("id", "")).strip()
        profile_kinds = as_strings(profile.get("entityKinds"))

        for group in profile.get("strategyGroups", []):
            if not isinstance(group, dict):
                continue

            conditions = group.get("conditions")
            if not isinstance(conditions, dict):
                continue

            kinds = as_strings(conditions.get("entityKinds")) or profile_kinds
            if not kinds:
                continue

            subkinds = as_strings(conditions.get("subtypes")) or [""]
            prominence_values = as_strings(conditions.get("prominence")) or ["recognized"]
            tags = tuple(as_strings(conditions.get("tags")))
            group_name = str(group.get("name", "")).strip()

            for kind in kinds:
                for subkind in subkinds:
                    for prominence in prominence_values:
                        requests.append(
                            SampleRequest(
                                culture=culture,
                                profile=profile_id,
                                group=group_name,
                                kind=kind,
                                subkind=subkind,
                                prominence=prominence,
                                tags=tags,
                            )
                        )

    return requests


def build_context_requests() -> list[SampleRequest]:
    cultures = {
        "aurora_stack": ("penguin", "hero"),
        "nightshelf": ("penguin", "hero"),
        "wake_singers": ("orca", "hero"),
    }
    requests: list[SampleRequest] = []

    for culture, (actor_kind, actor_subkind) in cultures.items():
        requests.extend(
            [
                SampleRequest(
                    culture=culture,
                    kind="artifact",
                    subkind="weapon",
                    scenario="context_generator_owner_artifact",
                    context=context_items(generator_owner_context()),
                ),
                SampleRequest(
                    culture=culture,
                    kind="artifact",
                    subkind="instrument",
                    scenario="context_generator_selected_instrument",
                    context=context_items(generator_owner_context()),
                ),
                SampleRequest(
                    culture=culture,
                    kind="transport",
                    subkind="trade",
                    scenario="context_trade_route_endpoints",
                    context=context_items(trade_route_context()),
                ),
                SampleRequest(
                    culture=culture,
                    kind="transport",
                    subkind="battleship",
                    scenario="context_orca_route_war_transport",
                    context=context_items(orca_route_war_context()),
                ),
                SampleRequest(
                    culture=culture,
                    kind="transport",
                    subkind="scout",
                    scenario="context_war_launches_scout",
                    context=context_items(war_launches_scout_context()),
                ),
                SampleRequest(
                    culture=culture,
                    kind="transport",
                    subkind="citadel",
                    scenario="context_trade_conference_citadel",
                    context=context_items(trade_conference_citadel_context()),
                ),
                SampleRequest(
                    culture=culture,
                    kind="occurrence",
                    subkind="war",
                    scenario="context_orca_vs_penguin_war",
                    context=context_items(orca_route_war_context()),
                ),
                SampleRequest(
                    culture=culture,
                    kind="occurrence",
                    subkind="festival",
                    scenario="context_retrieval_monster_slaying_festival",
                    context=context_items(monster_slaying_festival_context()),
                ),
                SampleRequest(
                    culture=culture,
                    kind=actor_kind,
                    subkind=actor_subkind,
                    scenario="context_monster_stole_artifact_hunter",
                    context=context_items(monster_stole_artifact_context()),
                ),
                SampleRequest(
                    culture=culture,
                    kind="monster",
                    subkind="leviathan",
                    scenario="context_generator_monster_from_place",
                    context=context_items(generator_monster_context()),
                ),
                SampleRequest(
                    culture=culture,
                    kind="faction",
                    subkind="region",
                    scenario="context_regional_faction",
                    context=context_items(regional_context()),
                ),
            ]
        )

    return requests


def context_items(context: dict[str, str]) -> tuple[tuple[str, str], ...]:
    return tuple(sorted(context.items(), key=lambda item: item[0]))


def generator_owner_context() -> dict[str, str]:
    return {
        "producer_kind": "generator",
        "primary_entity_name": "Lightbringer Guldan The Spirit Keeper",
        "primary_entity_reference_name": "Guldan",
        "secondary_entity_name": "The Corpse Current",
        "secondary_entity_reference_name": "Corpse Current",
        "selected_name": "Kaladin The Thaw Witness",
        "selected_reference_name": "Kaladin",
        "reference_name": "Guldan",
        "region": "Corpse Current",
    }


def trade_route_context() -> dict[str, str]:
    return {
        "producer": "trade_routes_convene_negotiation",
        "producer_kind": "gambit",
        "trigger_relationship_source_entity_name": "North Lantern Market",
        "trigger_relationship_source_entity_reference_name": "North Lantern",
        "trigger_relationship_target_entity_name": "Corpse Current Harbor",
        "trigger_relationship_target_entity_reference_name": "Corpse Current",
        "primary_entity_name": "North Lantern Market",
        "primary_entity_reference_name": "North Lantern",
        "secondary_entity_name": "Corpse Current Harbor",
        "secondary_entity_reference_name": "Corpse Current",
        "reference_name": "North Lantern",
        "region": "Corpse Current",
    }


def orca_route_war_context() -> dict[str, str]:
    return {
        "producer": "orca_route_wars_get_names",
        "parent_producer": "orca_raids_trade_route",
        "producer_kind": "generator",
        "trigger_relationship_source_entity_name": "Rughak The Route-Biter",
        "trigger_relationship_source_entity_reference_name": "Rughak",
        "trigger_relationship_target_entity_name": "North Lantern Trade",
        "trigger_relationship_target_entity_reference_name": "North Lantern",
        "primary_entity_name": "Rughak The Route-Biter",
        "primary_entity_reference_name": "Rughak",
        "secondary_entity_name": "North Lantern Trade",
        "secondary_entity_reference_name": "North Lantern",
        "reference_name": "Rughak",
        "region": "North Lantern",
    }


def war_launches_scout_context() -> dict[str, str]:
    return {
        "producer": "war_occurrences_launch_scouts",
        "producer_kind": "generator",
        "primary_entity_name": "The Rughak Against North Lantern War",
        "primary_entity_reference_name": "Rughak War",
        "secondary_entity_name": "North Lantern Trade",
        "secondary_entity_reference_name": "North Lantern",
        "reference_name": "Rughak",
        "region": "North Lantern",
    }


def trade_conference_citadel_context() -> dict[str, str]:
    return {
        "producer": "trade_conferences_raise_citadels",
        "producer_kind": "generator",
        "primary_entity_name": "North Lantern To Corpse Current Conclave",
        "primary_entity_reference_name": "North Lantern Conclave",
        "secondary_entity_name": "Corpse Current Harbor",
        "secondary_entity_reference_name": "Corpse Current",
        "reference_name": "North Lantern",
        "region": "Corpse Current",
    }


def monster_stole_artifact_context() -> dict[str, str]:
    return {
        "producer": "stolen_artifacts_draw_hunters",
        "parent_producer": "monster_steals_artifact",
        "producer_kind": "generator",
        "trigger_relationship_source_entity_name": "The Murk Leviathan",
        "trigger_relationship_source_entity_reference_name": "Murk Leviathan",
        "trigger_relationship_target_entity_name": "The Dreaming Tooth",
        "trigger_relationship_target_entity_reference_name": "Dreaming Tooth",
        "primary_entity_name": "The Murk Leviathan",
        "primary_entity_reference_name": "Murk Leviathan",
        "secondary_entity_name": "The Dreaming Tooth",
        "secondary_entity_reference_name": "Dreaming Tooth",
        "reference_name": "Kaladin",
        "region": "Murk Vent",
    }


def monster_slaying_festival_context() -> dict[str, str]:
    return {
        "producer": "monster_slayings_become_festivals",
        "parent_producer": "penguin_slays_monster",
        "producer_kind": "generator",
        "trigger_relationship_source_entity_name": "Kaladin The Thaw Witness",
        "trigger_relationship_source_entity_reference_name": "Kaladin",
        "trigger_relationship_target_entity_name": "The Murk Leviathan",
        "trigger_relationship_target_entity_reference_name": "Murk Leviathan",
        "primary_entity_name": "Kaladin The Thaw Witness",
        "primary_entity_reference_name": "Kaladin",
        "secondary_entity_name": "The Murk Leviathan",
        "secondary_entity_reference_name": "Murk Leviathan",
        "reference_name": "Kaladin",
        "region": "Murk Vent",
    }


def generator_monster_context() -> dict[str, str]:
    return {
        "producer_kind": "generator",
        "primary_entity_name": "Murk Vent",
        "primary_entity_reference_name": "Murk Vent",
        "secondary_entity_name": "The Dreaming Tooth",
        "secondary_entity_reference_name": "Dreaming Tooth",
        "reference_name": "Murk",
        "region": "Murk Vent",
    }


def regional_context() -> dict[str, str]:
    return {
        "producer_kind": "generator",
        "primary_entity_name": "Corpse Current Shelf",
        "primary_entity_reference_name": "Corpse Current",
        "secondary_entity_name": "North Lantern Market",
        "secondary_entity_reference_name": "North Lantern",
        "reference_name": "Corpse Current",
        "region": "Corpse Current",
    }


def dedupe_requests(requests: list[SampleRequest]) -> list[SampleRequest]:
    seen: set[SampleRequest] = set()
    unique: list[SampleRequest] = []
    for request in requests:
        if request in seen:
            continue
        seen.add(request)
        unique.append(request)
    return unique


def resolve_nameforge_cli(repo_root: Path, no_build: bool) -> list[str]:
    if not no_build:
        subprocess.run(
            ["dotnet", "build", "tools/NameForge/NameForge.csproj", "-c", "Release", "--nologo"],
            cwd=repo_root,
            stdout=sys.stderr,
            stderr=sys.stderr,
            check=True,
        )

    dll = find_nameforge_dll(repo_root)
    if dll is not None:
        return ["dotnet", str(dll)]

    command = ["dotnet", "run"]
    if no_build:
        command.append("--no-build")
    command.extend(["--project", "tools/NameForge", "--"])
    return command


def find_nameforge_dll(repo_root: Path) -> Path | None:
    candidates = [
        repo_root / ".artifacts/bin/NameForge/Release/net8.0/NameForge.dll",
        repo_root / "tools/NameForge/bin/Release/net8.0/NameForge.dll",
    ]
    for candidate in candidates:
        if candidate.exists():
            return candidate
    matches = sorted(repo_root.glob("**/NameForge.dll"))
    return matches[0] if matches else None


def write_samples(
    repo_root: Path,
    project_path: Path,
    output_path: Path,
    cli: list[str],
    requests: list[SampleRequest],
    args: argparse.Namespace,
) -> int:
    fieldnames = [
        "culture",
        "kind",
        "subkind",
        "scenario",
        "sample_index",
        "name",
        "profile",
        "group",
        "prominence",
        "tags",
        "context",
        "error",
    ]

    if str(output_path) == "-":
        handle = sys.stdout
        close_handle = False
    else:
        handle = output_path.open("w", encoding="utf-8", newline="")
        close_handle = True

    rows_written = 0
    try:
        writer = csv.DictWriter(handle, fieldnames=fieldnames)
        writer.writeheader()

        for request in requests:
            count = args.context_count if request.is_contextual and args.context_count is not None else args.count
            names, error = generate_names(repo_root, project_path, cli, request, count, args.seed_prefix)
            if error:
                if args.fail_fast:
                    raise RuntimeError(error)
                writer.writerow(row_for_error(request, error))
                rows_written += 1
                continue

            for index, name in enumerate(names, start=1):
                writer.writerow(row_for_name(request, index, name))
                rows_written += 1
    finally:
        if close_handle:
            handle.close()

    return rows_written


def generate_names(
    repo_root: Path,
    project_path: Path,
    cli: list[str],
    request: SampleRequest,
    count: int,
    seed_prefix: str,
) -> tuple[list[str], str]:
    command = [
        *cli,
        "--project",
        str(project_path),
        "--culture",
        request.culture,
        "--kind",
        request.kind,
        "--count",
        str(max(1, count)),
        "--seed",
        seed_for(seed_prefix, request),
    ]

    if request.subkind:
        command.extend(["--subtype", request.subkind])
    if request.profile:
        command.extend(["--profile", request.profile])
    if request.prominence:
        command.extend(["--prominence", request.prominence])
    for tag in request.tags:
        command.extend(["--tag", tag])
    for key, value in request.context:
        command.extend(["--context", f"{key}={value}"])

    completed = subprocess.run(command, cwd=repo_root, capture_output=True, text=True)
    if completed.returncode != 0:
        return [], completed.stderr.strip() or completed.stdout.strip() or f"exit {completed.returncode}"

    names = [line.strip() for line in completed.stdout.splitlines() if line.strip()]
    return names, ""


def seed_for(seed_prefix: str, request: SampleRequest) -> str:
    subkind = request.subkind or "default"
    tags = "-".join(request.tags) or "none"
    context = "-".join(key for key, _ in request.context) or "none"
    return (
        f"{seed_prefix}:{request.scenario}:{request.culture}:{request.kind}:"
        f"{subkind}:{request.profile or 'auto'}:{request.prominence}:{tags}:{context}"
    )


def row_for_name(request: SampleRequest, index: int, name: str) -> dict[str, str | int]:
    return {
        "culture": request.culture,
        "kind": request.kind,
        "subkind": request.subkind or "(default)",
        "scenario": request.scenario,
        "sample_index": index,
        "name": name,
        "profile": request.profile,
        "group": request.group,
        "prominence": request.prominence,
        "tags": "|".join(request.tags),
        "context": json.dumps(dict(request.context), sort_keys=True, ensure_ascii=False),
        "error": "",
    }


def row_for_error(request: SampleRequest, error: str) -> dict[str, str | int]:
    return {
        "culture": request.culture,
        "kind": request.kind,
        "subkind": request.subkind or "(default)",
        "scenario": request.scenario,
        "sample_index": "",
        "name": "",
        "profile": request.profile,
        "group": request.group,
        "prominence": request.prominence,
        "tags": "|".join(request.tags),
        "context": json.dumps(dict(request.context), sort_keys=True, ensure_ascii=False),
        "error": error,
    }


if __name__ == "__main__":
    raise SystemExit(main())
