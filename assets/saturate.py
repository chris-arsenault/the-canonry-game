#!/usr/bin/env python3
"""One-off saturation/contrast boost for Flux-generated watercolor assets.

Flux 2 consistently renders painterly watercolor at low saturation — the
output looks ashed-out compared to the prompt's actual palette. This
script is the immediate fix against the currently-generated canonry pack.
The proper fix goes into illuminator as a post-processor saturation step
(or per-asset LUT).

Saturation boosts RGB in place; alpha is preserved intact. Acts on every
.png under assets/output/processed/ including atlas sheets.

Usage:
    python assets/saturate.py
    python assets/saturate.py --saturation 1.5 --contrast 1.10
    python assets/saturate.py --dry-run
"""
from __future__ import annotations

from pathlib import Path

import click
from PIL import Image, ImageEnhance

ROOT = Path(__file__).parent / "output" / "processed"


@click.command()
@click.option("--saturation", type=float, default=1.4,
              help="Saturation multiplier (1.0 = unchanged; 1.4 is the Flux-ash default).")
@click.option("--contrast", type=float, default=1.10,
              help="Contrast multiplier (1.0 = unchanged; mild default to restore definition).")
@click.option("--dry-run", is_flag=True,
              help="List what would be changed without writing.")
def main(saturation: float, contrast: float, dry_run: bool) -> None:
    if not ROOT.exists():
        raise click.UsageError(f"Processed assets dir not found: {ROOT}")

    pngs = sorted(ROOT.rglob("*.png"))
    click.echo(
        f"Applying saturation={saturation:.2f}x contrast={contrast:.2f}x "
        f"to {len(pngs)} PNGs under {ROOT}"
    )

    for path in pngs:
        img = Image.open(path)
        alpha = None
        if img.mode == "RGBA":
            alpha = img.split()[3]
            rgb = img.convert("RGB")
        elif img.mode == "RGB":
            rgb = img
        else:
            rgb = img.convert("RGB")

        rgb = ImageEnhance.Color(rgb).enhance(saturation)
        rgb = ImageEnhance.Contrast(rgb).enhance(contrast)

        out = Image.merge("RGBA", (*rgb.split(), alpha)) if alpha is not None else rgb

        rel = path.relative_to(ROOT)
        if dry_run:
            click.echo(f"  [dry] {rel}")
        else:
            out.save(path)
            click.echo(f"  {rel}")

    click.echo(
        f"\nDone. {len(pngs)} files {'would be ' if dry_run else ''}"
        f"saturated {saturation:.2f}x / contrast {contrast:.2f}x."
    )


if __name__ == "__main__":
    main()
