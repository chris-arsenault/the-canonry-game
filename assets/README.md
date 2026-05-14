# assets

Source pack for the game's illustrated assets. Each PNG used by the Godot
client is generated from a prompt file here, routed through the
[`illuminator`](../../illuminator) CLI: BFL Flux 2 for rendering, Claude
for prompt synthesis against the shared Glacial Archive style anchor.

## Layout

```
assets/
├── pack.toml           Manifest: sections, asset defs, style/palette overrides
├── prompts/            One Markdown file per asset; short raw descriptions
│   ├── 1-entity-sprites/
│   ├── 2-terrain-hex-tiles/
│   ├── 3-tableau-card-faces/
│   ├── 4-ui-chrome-decoration/
│   ├── 5-event-type-icons/
│   ├── 6-attribute-icons/
│   ├── 7-atmospheric-art/
│   └── 8-playback-control-icons/
└── output/             Generated PNGs (gitignored — see below)
    ├── raw/            Raw Flux outputs + sidecar .json metadata
    └── processed/      rembg cutouts, resized icon variants, atlas sheets
```

## Running it

Requires the illuminator CLI at `../illuminator`. See its README for install
details.

Outputs land in `assets/output/` by default — `output/raw/` for the raw Flux
PNGs + sidecar JSONs, `output/processed/` for post-processed variants and
atlases. Generated files are tracked in git so the game builds without
needing anyone to regenerate; the raw/ PNGs average ~2 MB each and the
processed/ atlases are smaller, so a full pack is large enough to review
before committing.

A `.gdignore` file at `assets/` keeps Godot from auto-importing the prompt
files or generated PNGs. If you later want Godot to consume specific
rendered assets, copy or symlink them into a Godot-facing directory (e.g.
`art/` under the project root).

```bash
# validate the pack — no API calls, no cost
with-cred -- node ../illuminator/dist/cli.js generate assets --dry-run

# smoke test: 3 assets across sections, generate + post-process in one shot
with-cred -- node ../illuminator/dist/cli.js generate assets \
  --smoke-test --and-process --concurrency 3

# full batch with post-processing
with-cred -- node ../illuminator/dist/cli.js generate assets \
  --and-process --concurrency 3
```

API keys are injected automatically by `with-cred` in this environment. Running
outside the managed PTY? Export `BFL_API_KEY` and `ANTHROPIC_API_KEY` directly.

## Editing the pack

- **New asset:** add a `[[section.asset]]` entry to `pack.toml` referencing a
  new `.md` file, write the prompt file, run `--dry-run` to validate.
- **New section:** add a `[[section]]` with its own `style` / `palette`
  overrides if the group needs to diverge visually.
- **Tweaking style:** edit `[style.archive]` or `[style.cinematic]` in
  `pack.toml`. Changes apply to every asset in the batch.
- **Tweaking palette:** same — `[palette.polar]` / `[palette.signal]`.

Prompt files themselves should stay short. The style anchor and palette do
the heavy visual direction; prompts describe the subject matter only.

## Pack structure

- `preset = "glacial-archive"` — the base style + palette living in the
  illuminator tool.
- `default_style = "archive"` + `default_palette = "polar"` — the canonical
  look for most assets.
- Sections 5 and 6 (event + attribute icons) override `palette = "signal"`
  for higher-contrast readability at 16px.
- Section 7 (atmospheric art) overrides `style = "cinematic"` for broader
  framing on title art and backdrops.
- `[type_prompt_fragment]` adds production guidance per asset type (e.g.
  icons get "keep silhouettes crisp at 16px").
