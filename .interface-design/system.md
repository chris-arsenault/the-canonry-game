# Design System — The Canonry Game

## Direction

**Name:** The Glacial Archive
**Personality:** Editorial archival · observational · disciplined
**Foundation:** Cold near-black with warm-cream text, one ember accent
**Depth:** Flat with hairline borders, minimal soft shadows on cards only
**Density:** Medium — ledger-like, readable at a glance, never cluttered

**One-line differentiation:** a serif display + mono numeric pairing on cold surfaces, with ember used so rarely that when it appears the eye follows it.

## Principles

1. **The archivist, not the adventurer.** The player reads a frozen record of an unfolding civilization. Copy and type choices behave like museum labels, field journals, and physics-lab readouts — not fantasy chrome.
2. **Warm ink on cold surfaces.** Vellum-cream text (not pure white) against basalt/slate/frost surfaces. The warmth of the text carries the humanity; the coldness of the surface carries the world's indifference.
3. **Ember is sacred.** The hot accent is used for: selected entity, last-fired generator, critical thresholds. Nothing else. Overuse breaks the spell.
4. **Section headers are small-caps, spaced, in Parchment.** Every body section reads like a specimen card.
5. **Numerical values are mono.** Ticks, coordinates, attribute values, counts. The eye needs tabular alignment.

## Tokens

### Surface (cold, layered)
- `Ink`:      `#0B0F14` — darkest, rare (app root, behind everything)
- `Basalt`:   `#121722` — surface 0 (main background)
- `Slate`:    `#19202C` — surface 1 (panel backgrounds)
- `Frost`:    `#222B3A` — surface 2 (raised cards, buttons)
- `Ice`:      `#2E3A4F` — surface 3 (hover, pressed, selected row)

### Text (warm, layered)
- `Vellum`:    `#E7E3D5` — primary text, titles
- `Parchment`: `#B8B5A6` — secondary text, section headers (small-caps)
- `Whisper`:   `#6F7684` — muted / tertiary / placeholder
- `Charcoal`:  `#3E4654` — disabled text

### Rules & borders
- `Hairline`:  `rgba(231, 227, 213, 0.08)` — default border
- `HairlineStrong`: `rgba(231, 227, 213, 0.20)` — emphasized border, active card

### Accents (used sparingly)
- `Ember`:     `#D97742` — selected, last-fired, urgent (RARE)
- `Aurora`:    `#78C9B2` — active, confirm, healthy trend
- `Blood`:     `#B04646` — critical, dead, failure
- `Gilt`:      `#C9A96B` — highlighted info, decorative flourish

### Kind accents (hex map + card borders)
These are muted cold-palette variants of the kind colors, replacing the current hot saturated palette:
- `Penguin`:    `#8AB4C9` — ice-blue
- `Settlement`: `#C9A96B` — gilt / parchment
- `Faction`:    `#B4707A` — dusty rose (faction flags stand out against cold)
- `Artifact`:   `#A6C8B8` — sea-glass
- `Region`:     `#8896C2` — cold periwinkle
- `Orca`:       `#556A7F` — ocean shadow

### Terrain (hex map)
Muted realistic palette, not oversaturated.
- `Wastes`:     `#8C8A80` — bone gray
- `Crags`:      `#54555A` — basalt
- `Tundra`:     `#A5B1B8` — pale frost
- `Ice`:        `#C7D2DA` — glare
- `HotSprings`: `#7BA488` — moss-warm
- `Shelf`:      `#6889A1` — shallow ocean
- `Reef`:       `#3E6078` — deep shelf
- `Ocean`:      `#1C2B3F` — abyss

## Spacing

Base: `4px`. Scale: `4, 8, 12, 16, 24, 32, 48`. All paddings/margins/gaps pick from this scale. No ad-hoc values.

## Radius

- `RadiusSharp`:  `2` — hairline shapes, rules
- `RadiusSmall`:  `6` — buttons, badges, inputs
- `RadiusMedium`: `10` — entity cards, generator cards
- `RadiusLarge`:  `14` — root panels, floating windows

## Typography

Uses Godot `SystemFont` with fallback chains — no binary assets, graceful degradation.

### Display (serif, editorial)
Chain: `EB Garamond` → `Cormorant Garamond` → `Iowan Old Style` → `Georgia` → `DejaVu Serif` → `Liberation Serif` → default serif.
Used for: panel titles, entity names, generator card names, section banners.

### Body (sans, disciplined)
Chain: `IBM Plex Sans` → `Inter Tight` → `Source Sans Pro` → `DejaVu Sans` → `Liberation Sans` → default sans.
Used for: description text, button labels, dropdowns, tooltips.

### Mono (numerical)
Chain: `IBM Plex Mono` → `JetBrains Mono` → `Fira Code` → `DejaVu Sans Mono` → `Liberation Mono` → default mono.
Used for: ticks, coordinates, attribute values, hashes, counts.

### Sizes
- `TitleLarge`: 22 (display) — floating window / panel chrome titles
- `TitleMedium`: 18 (display) — section titles, entity names
- `TitleSmall`: 15 (display) — card names, subsection titles
- `Body`: 13 (body) — default body text
- `BodySmall`: 11 (body) — descriptions, dropdown items
- `Caption`: 10 (body, uppercase, letter-spaced) — section headers, badges
- `Micro`: 9 (body, uppercase, letter-spaced) — type lines, status chips
- `Numeric`: 13 (mono) — inline numerical values

## Patterns

### Root window
- Background: `Basalt`
- Top bar (if added): hairline divider below, `Slate`

### Panel (browser, event log, tableau root)
- Background: `Slate`
- Border: `Hairline` 1px
- Radius: `RadiusLarge` (14)
- Content padding: `16`
- Title: `TitleMedium` in `Vellum`, center-aligned, with 8px gap then a `HairlineStrong` rule, then body

### Card (entity card, generator card)
- Background: `Frost`
- Border: `HairlineStrong` 1px (or kind-accent @ 60% α for generator cards)
- Radius: `RadiusMedium` (10)
- Soft shadow: `rgba(0, 0, 0, 0.28)` 8px blur
- Selected: border upgraded to `Ember`
- Content padding: `12`

### Button
- Background: `Frost`
- Hover: `Ice`
- Pressed: `Ice` with `Ember` 1px border
- Text: `Vellum`, body font, 13
- Padding: `8 12`
- Radius: `RadiusSmall` (6)
- Flat buttons (entity list rows): transparent bg, hover = `Frost`, padding `4 8`

### Section header (inside cards/panels)
- Text: `Caption` (10), uppercase, letter-spaced (~2px)
- Color: `Parchment`
- Optional hairline rule below, 1px `Hairline`
- Margin: `12` top, `6` bottom

### Badges / kind tags
- Background: kind accent @ 20% α
- Border: kind accent @ 50% α, 1px
- Text: kind accent lightened, `Micro` (9), uppercase
- Padding: `2 6`
- Radius: `RadiusSmall`

### Status chip
- Background: semantic color (Aurora/Ember/Blood/Slate) @ 80% α
- Border: same, lightened, @ 60% α
- Text: `Vellum`, `Micro`
- Padding: `3 6`
- Radius: `RadiusSmall`

### Tick / coordinate readouts
- Mono font, `Numeric` (13) or `Caption` (10)
- Color: `Parchment` for secondary, `Vellum` for primary

### Rules and separators
- 1px, color `Hairline` — never heavier
- Where emphasis is needed, double-rule: `HairlineStrong` over 3px of `Basalt`

### Hex map
- Background canvas: `Basalt`
- Hex fill: terrain token
- Hex outline: `Hairline` 1px; 1.5px `Whisper` on axis hexes (semantic plane)
- Region borders: `Ink` 2.5px
- Entity markers: kind accent, not oversaturated
- Selected entity: additional `Ember` halo
- Names: body font, 10px, `Vellum` @ 85% α

### RichTextLabel / BBCode (event log, entity cards)
BBCode color map (for `[color=X]` usage in existing code):
- `red` → `Blood`
- `yellow` → `Gilt`
- `green` → `Aurora`
- `cyan` → `Gilt` (creamy — not true cyan)
- `white` → `Vellum`
- `gray` → `Whisper`

## Anti-patterns (do not do)

- Pure white (`#FFFFFF`) text — use `Vellum`
- Drop shadows on text
- Gradients (except the subtle vertical fade on hex map background if implemented)
- Rounded-corner values outside the radius scale
- Spacing values outside the spacing scale
- Any saturation ramp that pushes magenta / electric cyan / neon green
- Multiple Ember elements on screen at once (unless a multi-select is explicit and modal)
- Sans-serif for headers, serif for body (reverse what everyone else does — keep it)
