# Region

A named physical partition of the map. Regions are literal named coverage over
terrain, not an abstract layer separate from geography.

## Subtypes

- `face`
- `shelf`
- `depths`
- `reach`
- `sea`
- `scar`

## Attributes

- `stability`: health; low values mark places near collapse or breach.
- `holding`: the land's capacity to hold boundaries and protections.
- `pristiness`: unspoiled quality; the inverse of corruption.

## Low-Altitude Behavior

Regions participate mostly through semantic movement, semantic influence, map
signals, lifecycle, and relationship maintenance. They are special cases and
should not be forced into every actor-like system.

## High-Altitude Role

Generators can create true new regions, split map coverage, scar terrain, or
name a previously background place. Region placement must respect terrain and
coverage rules.
