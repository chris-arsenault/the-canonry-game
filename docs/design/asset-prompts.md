# Asset Generation Prompts — The Canonry Game

Flux prompts for a full static-asset pass. Design direction is **"The Glacial Archive"** — editorial archival illustration, cold palette, warm ink on cold surfaces. Think 19th-century field naturalist's sketchbook crossed with a cartographer's atlas, rendered in muted watercolor and fine ink.

## Ground rules (apply to every prompt)

**Always include in the prompt or as a style tag:**
- `painterly editorial illustration, ink and watercolor, muted cold palette, hand-drawn feel`
- `antique cartographic style, museum plate aesthetic, aged parchment substrate`

**Always explicitly exclude** (Flux responds to direct negative language; repeat where needed):
- `not pixel art, not 8-bit, not 16-bit, not chibi, not anime, not cel-shaded, not flat vector, not low-poly, not neon, no harsh outlines`

**Palette anchors** — reference these hex values directly in prompts when it helps:
- Ink `#0B0F14` · Basalt `#121722` · Slate `#19202C` · Frost `#222B3A`
- Vellum (warm cream) `#E7E3D5` · Parchment `#B8B5A6` · Whisper `#6F7684`
- Ember (rare hot accent) `#D97742` · Aurora `#78C9B2` · Gilt `#C9A96B` · Blood `#B04646`

**Output conventions** (state in each prompt):
- Sprites / icons: centered subject, neutral cream `#E7E3D5` background, clean edges for easy cutout, square 1:1
- Terrain tiles: orthographic top-down, edges softly fading to cream for hex masking, square 1:1
- Chrome / ornaments: transparent or cream background, centered, symmetric where applicable
- All renders at **1024×1024 minimum** — Python will downsample

---

## 1 · Entity sprites

Used on the hex map and in the Registry sidebar. Each drawn from a **slight 3/4 overhead angle**, as though you're looking at a specimen pinned to a page. Clean silhouette with a soft drop shadow below to anchor it on cream.

### 1.1 Penguin (`penguin.png`)

```
A single emperor penguin rendered as a detailed field-journal illustration
in ink and watercolor, from a slight 3/4 overhead angle, standing upright
on cream parchment. Dove-grey back, cream belly with a faint amber wash
along the breast, slate-blue shadow where the plumage catches cold light.
The eye is dark and attentive. Fine ink outlines, loose watercolor fills,
subtle grain from aged paper beneath. Centered, soft drop shadow below,
neutral warm-cream background #E7E3D5. Muted, contemplative, archival.
Not pixel art, not 8-bit, not chibi, not anime, not cel-shaded.
```

### 1.2 Settlement (`settlement.png`)

```
A small snow-bound penguin settlement rendered as a 19th-century
cartographer's marker: a cluster of low domed shelters of packed snow
and dark stone, a wisp of smoke rising from a central vent, seen from
a slight 3/4 overhead angle. Subdued gilt warmth from a fire-lit door,
bone-grey walls, deep ink shadows. Hand-painted watercolor with fine
nib-pen linework. Centered on warm-cream parchment #E7E3D5, soft drop
shadow. Painterly editorial illustration, museum atlas aesthetic.
Not pixel art, not 8-bit, not low-poly, not cartoon.
```

### 1.3 Faction banner (`faction.png`)

```
A weathered heraldic banner on a black pole, dusty rose fabric with a
swallowtail fly and a simple black emblem, fluttering slightly as though
caught in polar wind. Rendered in ink and watercolor, centered on a
warm-cream parchment background #E7E3D5 with a soft drop shadow. Hand
painted, aged, with small tears and wear along the edges. No glow.
Editorial illustration in the style of an antique military atlas.
Not pixel art, not 8-bit, not cel-shaded, not flat vector design.
```

### 1.4 Artifact (`artifact.png`)

```
An ancient carved bone or obsidian talisman rendered as a museum plate
illustration — a diamond-shaped pendant with concentric inlaid sea-glass
patterns, faint rune-like etching, resting on a cream parchment surface
from a slight overhead angle. Cool sea-glass green and bone-ivory with
deep ink shadow. Fine technical ink linework with restrained watercolor
fill. Centered, soft drop shadow, warm-cream background #E7E3D5.
Not pixel art, not 8-bit, not neon-glowing, not anime.
```

### 1.5 Orca (`orca.png`)

```
A single orca (killer whale) seen from directly above, dorsal fin and
back just breaking the dark water surface, rendered as a naturalist's
ink-and-watercolor plate. Jet-black body with a crisp white eye-patch
and chest, surrounded by a suggested wash of deep blue-black water that
fades to cream at the edges. Fine scientific linework. Centered,
warm-cream background #E7E3D5. Reserved, ominous, documentary.
Not pixel art, not 8-bit, not cartoon, not cute, not chibi.
```

### 1.6 Region cairn (`region.png`)

A small marker to stand in for "region" when represented as an entity — a cartographic waypoint.

```
A small weather-worn stone cairn on a featureless snowfield, three
stacked slate-grey stones with faint lichen, casting a long cold-blue
shadow to one side. Rendered as a 19th-century field sketch in ink
and muted watercolor. Centered on warm-cream parchment #E7E3D5, soft
drop shadow, a faint sketched ground line beneath. Editorial
illustration, no color saturation boost.
Not pixel art, not 8-bit, not low-poly, not flat vector.
```

### Optional variants

Run these with small tweaks if you want per-subtype visual distinction later:

```
[Above penguin prompt], but rendered as an ELDER penguin — longer
plumage, one eye scarred, a slight stoop, posture of quiet authority.
```

```
[Above settlement prompt], but rendered as a LARGE TOWN — multiple
clusters of domes, a small carved stone totem at the center, several
faint smoke trails. Same style and palette.
```

---

## 2 · Terrain hex tiles

These tile edge-to-edge in a pointy-top hex grid. Generate as **square images with the terrain filling the full frame**; the Python pass will apply a hex clip mask and cut a six-sided tile from the center. No hard edges in the image itself — let terrain fade naturally so tiles can overlap slightly without seams.

Integration note: the renderer currently fills hex polygons with a solid color. Swapping to textured tiles requires a small change in `HexMapRenderer.DrawPhysicalHexCell` to use `DrawTextureRectRegion` with a clipping polygon. I'll do that code change after we have the tiles.

All terrain prompts should include: `orthographic top-down view, seamless texture suitable for tiling, no horizon line, no sky, 1:1 square, painterly not photographic`.

### 2.1 Wastes (`terrain-wastes.png`)

```
Orthographic top-down view of a bone-grey empty snow waste — faint wind
scour patterns, a few dark protruding pebbles, pale cracked crust. Muted
palette of #8C8A80 bone-grey with warm-ochre dust and faint slate-blue
shadow in the wind grooves. Painterly, soft, almost abstract. No horizon,
no sky, fills the full square. Seamless texture feel at the edges.
Antique atlas aesthetic, ink and watercolor, hand-painted.
Not pixel art, not 8-bit, not photorealistic, not satellite photo.
```

### 2.2 Crags (`terrain-crags.png`)

```
Orthographic top-down view of dark basalt crags breaking through thin
snow — jagged rock ridges, deep shadow in the crevasses, pale dusting
of snow caught in pockets. Deep slate palette with #54555A base and
near-black #121722 in the deepest shadow, warm bone highlights on ridge
tops. Ink and watercolor, painterly. Full-frame, no horizon or sky.
Seamless at edges, suitable for tiling.
Not pixel art, not 8-bit, not photorealistic, not low-poly.
```

### 2.3 Tundra (`terrain-tundra.png`)

```
Orthographic top-down view of pale tundra — wind-packed snow broken by
sparse patches of rust-colored lichen and low frozen scrub, faint game
trails winding through. Pale frost palette of #A5B1B8 with muted rust
and ochre dots of vegetation. Painterly watercolor wash with fine ink
detail. Full-frame, no horizon, no sky. Seamless. Antique atlas plate.
Not pixel art, not 8-bit, not photorealistic, not anime.
```

### 2.4 Ice sheet (`terrain-ice.png`)

```
Orthographic top-down view of a pale glare-ice sheet — faint crackle
pattern in the surface, pale blue-white palette with #C7D2DA highlights
and deep slate-blue #2E3A4F in the fracture lines, no shadows because
the light is diffuse and everywhere. Looks cold, slightly translucent.
Painterly watercolor. Full-frame, no horizon, no sky. Seamless.
Not pixel art, not 8-bit, not photorealistic, not glossy digital.
```

### 2.5 Hot springs (`terrain-hotsprings.png`)

```
Orthographic top-down view of a cluster of hot spring pools set in
snow-rimmed rock — emerald-to-sea-green water #7BA488 with faint steam
wisps rising, warm mineral deposits in sulfur-yellow and rust ringing
each pool, dark wet rock between. Painterly watercolor. The warmest
terrain on the map — reads as a rare refuge. Full-frame, no horizon,
no sky. Seamless.
Not pixel art, not 8-bit, not photorealistic, not anime.
```

### 2.6 Shelf (`terrain-shelf.png`)

```
Orthographic top-down view of a shallow continental shelf — translucent
glacier-blue water #6889A1 over a visible bed of pale stone and sand,
faint ripple pattern, occasional kelp-dark patches. Painterly watercolor
with soft gradients from pale shallow to deeper blue. Full-frame, no
horizon, no sky. Seamless.
Not pixel art, not 8-bit, not photorealistic, not glossy digital.
```

### 2.7 Reef (`terrain-reef.png`)

```
Orthographic top-down view of a cold-water reef — dark kelp-black and
rust tones over deep blue #3E6078 water, glimpses of skeletal coral
structures and pale stone, the occasional flash of silver fish. Dense,
textured, slightly forbidding. Painterly watercolor with ink detail.
Full-frame, no horizon, no sky. Seamless.
Not pixel art, not 8-bit, not photorealistic, not tropical-reef colors.
```

### 2.8 Ocean (`terrain-ocean.png`)

```
Orthographic top-down view of deep polar ocean — abyssal blue-black
#1C2B3F with faint swell patterns catching pale slate highlights, a
hint of movement suggested by fine ink ripples. No foam, no whitecaps,
no sky. Restrained, cold, vast. Painterly watercolor. Full-frame.
Seamless at edges.
Not pixel art, not 8-bit, not photorealistic, not stormy-sea drama.
```

---

## 3 · Tableau card backgrounds

The Tableau shows "cards that define the world's nature." Each generator card currently has a colored border. Giving them an illustrated background per kind turns them into specimen cards. Generate **portrait-orientation card faces** with a **central illustrated scene** and space reserved top and bottom for text overlays.

Card format: `portrait orientation, 3:4 aspect, illustrated top half, muted textured blank space in the bottom half for overlaid text, warm-cream parchment frame around the edge, fine ink border`

### 3.1 Penguin card face (`card-penguin.png`)

```
Portrait-orientation illustrated card face. Upper half: an ink-and-
watercolor vignette of a lone emperor penguin standing on a ridge at
dusk, backlit by a pale aurora-green #78C9B2 band in the sky, fine
falling snow. Lower half: a plain textured #222B3A surface for overlaid
text, with a faint ghosted penguin silhouette watermark. Warm-cream
#E7E3D5 parchment frame around the whole card with a fine ink border.
3:4 portrait. Editorial illustration, museum specimen card aesthetic.
Not pixel art, not 8-bit, not anime, not cel-shaded.
```

### 3.2 Settlement card face (`card-settlement.png`)

```
Portrait-orientation illustrated card face. Upper half: an ink-and-
watercolor vignette of a small settlement at night, warm gilt #C9A96B
light spilling from a domed shelter's doorway onto the snow, faint
smoke rising, stars suggested. Lower half: textured dark slate
#222B3A for text, with a ghosted settlement silhouette watermark.
Warm-cream parchment frame, fine ink border. 3:4 portrait.
Not pixel art, not 8-bit, not low-poly, not cartoon.
```

### 3.3 Faction card face (`card-faction.png`)

```
Portrait-orientation illustrated card face. Upper half: a weathered
dusty-rose #B4707A faction banner planted on a ridge of dark stone,
silhouettes of three distant penguins gathered beneath it, cold
overcast sky. Lower half: textured dark slate for text, ghosted
banner watermark. Warm-cream parchment frame, fine ink border.
3:4 portrait. Editorial illustration, heraldic and spare.
Not pixel art, not 8-bit, not flat vector, not anime.
```

### 3.4 Artifact card face (`card-artifact.png`)

```
Portrait-orientation illustrated card face. Upper half: an ancient
sea-glass and bone talisman resting on a pedestal of dark ice, lit
from above by a single pale shaft of light, the talisman's carved
patterns faintly visible. Lower half: textured dark slate for text,
ghosted artifact watermark. Warm-cream parchment frame, fine ink
border. 3:4 portrait. Reverent, archival, still-life.
Not pixel art, not 8-bit, not neon-glowing, not anime.
```

### 3.5 Orca card face (`card-orca.png`)

```
Portrait-orientation illustrated card face. Upper half: the shadow of
a single orca cruising just beneath a thin sheet of translucent ice,
seen from directly above, a dorsal fin cutting the surface, cold
abyssal blue. Lower half: textured dark slate for text, ghosted orca
silhouette watermark. Warm-cream parchment frame, fine ink border.
3:4 portrait. Ominous, quiet, inevitable.
Not pixel art, not 8-bit, not photorealistic, not cartoon.
```

### 3.6 Generic card back (`card-back.png`)

```
Portrait-orientation illustrated card back — a single centered
compass rose in dark ink on a warm-cream parchment field, faint
concentric circle patterns radiating outward, aged paper texture
with subtle foxing at the corners. Slate-blue accent in the compass
needle. Fine ink linework, no color saturation. Symmetric. 3:4
portrait. Museum collection cataloging aesthetic.
Not pixel art, not 8-bit, not digital-flat, not anime.
```

---

## 4 · UI chrome & decorations

Small reusable ornaments. Generate on transparent background where possible (Flux handles this with explicit `transparent background` prompting, but a cream background + Python alpha-key works as a fallback).

### 4.1 Section divider flourish (`divider-flourish.png`)

```
A long horizontal ink flourish — central small compass-rose motif
flanked by thin hairline rules that fade to nothing at either end,
like a chapter break in a 19th-century atlas. Dark slate #19202C ink
on a transparent background. Narrow strip, roughly 8:1 aspect ratio.
Purely decorative linework, no color fills, no watercolor, just fine
ink. Symmetric.
Not pixel art, not 8-bit, not digital-flat, not decorative-vector.
```

### 4.2 Corner ornament (`corner-ornament.png`)

```
A single corner ornament in dark ink — an elegant cartographic
flourish suitable for the top-left corner of a museum plate, with
small geometric motifs and a delicate vine-like curl that extends
along both edges of the corner. Dark slate #19202C ink on transparent
background. Square, clean, symmetric along the diagonal. Fine linework
only.
Not pixel art, not 8-bit, not flat-vector, not digital-decorative.
```

### 4.3 Compass rose (`compass-rose.png`)

```
A centered compass rose in the style of an antique nautical chart —
eight-pointed star with elongated N-S axis, fleur-de-lis at north,
finely inked concentric degree rings, subtle Gilt #C9A96B wash at
the points only. Dark ink on warm-cream #E7E3D5 parchment with a
faint aged texture. Square, centered, symmetric. Detailed technical
linework.
Not pixel art, not 8-bit, not digital-flat, not neon.
```

### 4.4 Scale bar (`scale-bar.png`)

```
A horizontal cartographic scale bar in dark ink — alternating filled
and empty segments with small tick marks beneath, numeric labels in
small-caps serif font spelling "0  ·  1  ·  2  ·  3  hexes". Dark
slate #19202C ink on transparent background. Narrow strip aspect.
Clean technical drafting style.
Not pixel art, not 8-bit, not flat-vector, not digital decorative.
```

### 4.5 Panel parchment texture (`texture-parchment.png`)

```
A seamless aged parchment texture — warm cream #E7E3D5 base with
subtle variations, faint foxing spots, occasional fiber and a hint
of horizontal grain. No patterns, no illustrations, just the surface.
Muted and restrained — this is a background, it must not compete with
foreground content. Seamless at edges, 1:1 square, 2048x2048 if
possible.
Not pixel art, not 8-bit, not photorealistic-leather, not digital noise.
```

### 4.6 Panel slate texture (`texture-slate.png`)

```
A seamless dark slate stone texture — base color #19202C, subtle
variations from slightly bluer to slightly greyer, very faint mineral
veining in Whisper #6F7684, overall quiet and uniform. Intended as a
UI panel background, so no strong features, no chisel marks, no
obvious pattern. Seamless at edges, 1:1 square, 2048x2048.
Not pixel art, not 8-bit, not photorealistic-rock, not digital noise.
```

---

## 5 · Event-type icons

Used in the Chronicle feed. The chronicle is now produced by
`src/Engine.Projection/UiChronicleProjection.cs` (consuming primitive
kernel events) and rendered by `src/TheCanonryGame.UI/ChroniclePanel.cs`
in the Godot scene started from `src/TheCanonryGame.App/Main.cs`. Each
icon is tiny, distinct at glance, filled line-art style.

Common prompt suffix for all five: `simple glyph in dark ink on transparent background, 1:1 square, centered, roughly 128×128 intended output, clean line-art, legible at 16px, not pixel art, not 8-bit, not flat vector`

### 5.1 Lifecycle — birth/death (`icon-lifecycle.png`)

```
A small ink glyph depicting a circle inscribed with a simple
half-filled triangle — symbolic of transition between states, like
an hourglass but simpler. Dark Blood #B04646 ink on transparent
background. 1:1 square, centered, legible at small size.
Not pixel art, not 8-bit, not flat vector, not neon.
```

### 5.2 Threshold — crossing a line (`icon-threshold.png`)

```
A small ink glyph of a single horizontal line broken in the middle
by an upward-arrow notch — symbolic of crossing a threshold. Ember
#D97742 ink on transparent background. Simple, urgent, legible at
16px. 1:1 square, centered.
Not pixel art, not 8-bit, not flat vector, not cartoon.
```

### 5.3 Creation — new entity (`icon-creation.png`)

```
A small ink glyph of a seed-like form — a teardrop shape with two
tiny outward-radiating marks suggesting growth. Aurora #78C9B2 ink
on transparent background. Simple, legible at 16px, symmetric.
1:1 square, centered.
Not pixel art, not 8-bit, not flat vector, not cute.
```

### 5.4 Card — card-driven event (`icon-card.png`)

```
A small ink glyph of a stylized rectangular card tilted slightly,
with a corner fold suggested by a single diagonal line. Gilt
#C9A96B ink on transparent background. Clean, legible at 16px.
1:1 square, centered.
Not pixel art, not 8-bit, not flat vector, not playing-card.
```

### 5.5 Generic notice (`icon-generic.png`)

```
A small ink glyph of a simple open circle with a single centered dot —
the universal "mark, notice" sigil. Parchment #B8B5A6 ink on
transparent background. 1:1 square, centered, legible at 16px.
Not pixel art, not 8-bit, not flat vector.
```

---

## 6 · Attribute icons (Ice Remembers domain)

Used wherever an attribute value is displayed (EntityCard, hex tooltip, legend). These are domain-specific to Ice Remembers. Generate at 256×256, centered, simple enough to read at 14×14 rendered.

Common prompt suffix: `centered sigil-style ink drawing on warm-cream #E7E3D5 background, simple enough to read at small size, not pixel art, not 8-bit, not flat vector, not cartoon`

### 6.1 Food (`attr-food.png`)

```
A small ink sigil of a simple fish silhouette arched slightly,
single line, no scales, no detail — pure iconography. Dark ink
on warm-cream #E7E3D5 background. 1:1 square, centered.
Not pixel art, not 8-bit, not flat vector.
```

### 6.2 Wealth (`attr-wealth.png`)

```
A small ink sigil of three stacked coins viewed from the edge,
suggested by three narrow horizontal ellipses. Faint Gilt #C9A96B
wash on the topmost coin only. Dark ink on warm-cream #E7E3D5.
1:1 square, centered.
Not pixel art, not 8-bit, not flat vector.
```

### 6.3 Population (`attr-population.png`)

```
A small ink sigil of three overlapping penguin silhouettes — a
huddle, reduced to essential curves, no faces, no detail. Dark ink
on warm-cream #E7E3D5 background. 1:1 square, centered.
Not pixel art, not 8-bit, not flat vector, not chibi.
```

### 6.4 Warmth (`attr-warmth.png`)

```
A small ink sigil of a simple flame — three tongues, symmetric, no
detail beyond silhouette. Ember #D97742 wash inside the flame, dark
ink outline. Warm-cream #E7E3D5 background. 1:1 square, centered.
Not pixel art, not 8-bit, not flat vector.
```

### 6.5 Echo (`attr-echo.png`)

```
A small ink sigil of concentric expanding wave-rings, four rings
radiating outward from a center dot — symbolic of social resonance
and influence spreading. Dark ink on warm-cream #E7E3D5 background.
1:1 square, centered, symmetric.
Not pixel art, not 8-bit, not flat vector.
```

### 6.6 Dominion (`attr-dominion.png`)

```
A small ink sigil of a simple crown-like form — three rounded points
over a horizontal band, reduced to essential silhouette. Dark ink
on warm-cream #E7E3D5 background. 1:1 square, centered, symmetric.
Not pixel art, not 8-bit, not flat vector, not cartoon.
```

### 6.7 Power (`attr-power.png`)

```
A small ink sigil of an angular lightning-like mark — a single bold
zig-zag stroke, no gradient, no glow, just weight. Dark ink on
warm-cream #E7E3D5 background. 1:1 square, centered.
Not pixel art, not 8-bit, not flat vector, not neon.
```

### 6.8 Holding (`attr-holding.png`)

```
A small ink sigil of a hand grasping a small stone, seen in profile
silhouette, reduced to essential curves. Dark ink on warm-cream
#E7E3D5 background. 1:1 square, centered.
Not pixel art, not 8-bit, not flat vector, not cartoon.
```

### 6.9 Provisions (`attr-provisions.png`)

```
A small ink sigil of a simple sack or bundle tied at the top with
a single diagonal line for the tie. Dark ink on warm-cream #E7E3D5
background. 1:1 square, centered.
Not pixel art, not 8-bit, not flat vector.
```

---

## 7 · Atmospheric / background art

Larger pieces for title screens, the area behind the Tableau panel, loading screens.

### 7.1 Title art — "The Ice Remembers" (`title-ice-remembers.png`)

```
A wide cinematic landscape illustration in ink and watercolor: a
vast polar ridgeline at dusk, a single lone penguin silhouette
standing on the foreground crest, a pale aurora-green #78C9B2 band
stretching across a cold slate sky, tiny settlements suggested by
faint warm gilt lights in the far distance. Subdued, reverent,
painterly. Muted palette — ice, slate, bone, ember only at the
settlement lights. Landscape orientation 16:9, high detail, aged
paper texture. Editorial book-plate aesthetic, not game-cinematic.
Not pixel art, not 8-bit, not photorealistic, not neon-painted, not anime.
```

### 7.2 Tableau hero backdrop (`tableau-backdrop.png`)

```
A subtle textured dark backdrop suitable for placing behind the
Tableau panel — a ghostly illustrated map of the simulation world
rendered in near-monochrome dark slate and very faint Gilt, with
hex grid lines barely visible, compass rose in one corner, the rest
of the image quiet and non-distracting. Landscape 16:9, muted enough
that white text overlaid on it remains fully legible. Antique atlas
aesthetic.
Not pixel art, not 8-bit, not photorealistic, not flat vector.
```

### 7.3 Reading-room vignette (`vignette-reading-room.png`)

```
A dark vignette overlay — pure transparent center fading to
near-black at the edges, simulating the effect of reading a page
by a single lamp. Smooth radial gradient from transparent to
#0B0F14 at the corners, slight grain. 16:9 landscape, intended to
be multiplied over the entire game view for atmosphere.
Not pixel art, not 8-bit, not cartoon-vignette.
```

### 7.4 Aurora band (`atmosphere-aurora.png`)

```
A wide horizontal band of pale aurora-green #78C9B2 with faint
purple undertones, soft-edged and ribbon-like, painterly with
suggested wisps and fade-outs. Pure aurora on a transparent
background. Intended to be composited as a subtle overhead accent
over the map. 16:3 wide aspect. Hand-painted, not computer-generated.
Not pixel art, not 8-bit, not neon, not digital-glow.
```

### 7.5 Aged paper frame (`frame-aged-paper.png`)

```
An ornate rectangular frame rendered as a 19th-century cartographer's
map border — fine ink linework with small repeating diamond motifs,
corner flourishes, subtle Gilt #C9A96B accents, aged cream parchment
visible in the border itself. Transparent rectangular interior so the
frame can be overlaid on any content. 4:3 outer aspect. Precise,
symmetric, restrained.
Not pixel art, not 8-bit, not flat vector, not digital decorative.
```

---

## 8 · Tick / speed control icons

Small chrome icons for the top-bar playback row, if we want to replace the current unicode `⏸ ▸ ↺` glyphs with proper art.

Common prompt suffix: `simple ink glyph on transparent background, 1:1 square, centered, legible at 16px, not pixel art, not 8-bit, not flat vector, not gamepad-UI`

### 8.1 Pause (`ctrl-pause.png`)

```
Two parallel vertical bars with soft rounded ends, drawn in dark
ink with the faintest brush-thickness variation. Centered on
transparent background. Nothing fancy.
Not pixel art, not 8-bit, not flat vector.
```

### 8.2 Step (`ctrl-step.png`)

```
A single right-pointing arrowhead followed by a thin vertical bar
on the right, rendered in dark ink. Suggests "advance one frame."
Centered on transparent background.
Not pixel art, not 8-bit, not flat vector.
```

### 8.3 Reset (`ctrl-reset.png`)

```
A counterclockwise circular arrow with an arrowhead pointing to
the upper-left, rendered in dark ink with a faint brush-variation.
Simple, instantly recognizable. Centered on transparent background.
Not pixel art, not 8-bit, not flat vector.
```

---

## 9 · Python post-processing notes

After generation, each category needs a slightly different treatment. High-level plan for the Python pass:

### Sprites (entities, event icons, attribute icons, control icons)
- Background removal via `rembg` (good enough for our aesthetic)
- Auto-crop to content bounding box with a small padding
- Resize to target sizes: 64/128/256 as PNG with alpha
- Optional: a faint drop shadow baked into the alpha so they anchor on hex terrain

### Terrain hex tiles
- Apply a hex-shaped clip mask with soft feathering at the edges so tiles overlap cleanly
- Resize to the in-engine hex size (current `HexSize = 30` → render target ~70×80 for pointy-top hexes at 2× retina)
- Save as flat PNG (no alpha needed; the hex mask handles it)
- **Code change required**: `HexMapRenderer.DrawPhysicalHexCell` currently does `DrawColoredPolygon(points, color)`. To use textures, switch to `DrawTexturePolygon(points, texture)` or draw the hex via `DrawTextureRectRegion` with a clipping polygon. I'll wire this up once tiles are generated.

### Card faces
- Light crop to 3:4 aspect
- Resize to the card size used in the Tableau (look up `CustomMinimumSize = new Vector2(308, 260)` — aspect is actually closer to 6:5, may need to regenerate or crop)
- Save as PNG, no alpha
- Apply via `UiTheme.CardStyle(...)` — would require extending the style helper to accept a background texture

### Chrome / decorations
- Background removal where needed
- Crop to symmetric bounding box
- Save at native resolution; UI will scale as needed

### Textures (parchment, slate)
- Verify seamlessness with a tiling test (offset 50% and check for seams)
- Reduce to 8-bit per channel, save PNG
- Feed to `StyleBoxTexture` for tiling panel backgrounds

### Atmosphere / backgrounds
- Minimal processing — mostly just resize and compress
- Vignette gets saved as PNG with alpha

---

## Suggested generation order

If you only want to do a first pass, prioritize:

1. **Entity sprites** (1.1–1.5) — biggest perceptual win, isolated work
2. **Terrain hexes** (2.1–2.8) — second-biggest win, but requires code wiring
3. **Title art** (7.1) — useful even if nothing else lands
4. **Card faces** (3.1–3.6) — nice to have but works without
5. Everything else is polish

Once a batch is generated, drop the PNGs in `assets/raw/` and I'll build the Python processing pipeline + integrate them into the Godot renderer.
