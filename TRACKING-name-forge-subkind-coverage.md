# Name Forge Subkind Coverage Tracking

Current status: every current Ice Remembers kind/subkind has a Name Forge naming route. Wake-Singer places, transports, and monsters now have first-pass subtype-specific grammars; Aurora transports and monsters have first-pass subtype-specific grammars. Several actor/social kinds still share broader routes.

## Completed First Pass

- Wake-Singer `transport`: `trade`, `battleship`, `scout`, and `citadel` route to distinct grammars and focused noun lists.
- Wake-Singer `monster`: `leviathan`, `wraith`, `beast`, and `revenant` route to distinct grammars and focused noun lists.
- Wake-Singer `point_of_interest`: `vent`, `peak`, `bloom`, and `shoal` route to distinct grammars.
- Wake-Singer `settlement`: `rookery`, `colony`, `holdfast`, `sanctuary`, `harbor`, and `reefhold` route to distinct grammars.
- Wake-Singer `region`: `face`, `shelf`, `depths`, `reach`, `sea`, and `scar` route to distinct grammars.
- Aurora `transport`: `trade`, `battleship`, `scout`, and `citadel` route to distinct grammars with compact ice-memory echoes.
- Aurora `monster`: `leviathan`, `wraith`/`revenant`, and `beast` route to distinct grammars with compact ice-memory echoes.

## Highest-Priority Gaps

- Nightshelf `settlement`, `region`, `point_of_interest`, `transport`, and `monster` still mostly share the location/monster route families.
- Aurora `settlement`, `region`, and `point_of_interest` still mostly share broad route families, though labels now carry compact echoes where needed.
- World/default culture has not received a subtype-distinction pass.

## Medium-Priority Gaps

- `artifact`: five subtypes collapse to roughly three routes.
- `occurrence`: four current subtypes collapse to roughly three routes.
- `penguin`: four current subtypes collapse to roughly three routes.
- `orca`: four current subtypes use stricter deed-record grammars, but role-level distinction is still broad.
- `faction`: `order`, `compact`, `cell`, `choir` share one route.

## Recommended Next Pass

Continue subtype-specific grammars and lexeme lists for the remaining cultures and then tighten actor/social kinds.

Suggested order:

1. Split Nightshelf places, transports, and monsters with incomplete-name markers tuned per subtype.
2. Split Aurora settlement, region, and point-of-interest naming by function/terrain.
3. Give `artifact`, `occurrence`, `penguin`, `orca`, and `faction` subtype families focused route sets.
4. Re-run 100-name samples per implemented kind/subkind after each culture pass.
