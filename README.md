# QEE – Auto-Repeat Vats

Adds an **"Auto-repeat growing"** toggle to the Questionable Ethics Enhanced vats.
When enabled, the vat automatically restarts the last thing it grew as soon as it
finishes and goes idle — provided the required ingredients are available (and, for
the clone vat, a matching genome is available on the map).

## How it works
- Adds a `Comp_AutoRepeat` (via XML patch) to the QEE vats, giving a toggle gizmo.
- A Harmony patch captures the last recipe / genome you started manually.
- When the vat returns to **Idle** with the toggle on, it re-invokes that start.

Covered:
- **Organ vat** (`Building_VatGrower`, the "Iniciar / select organ" gizmo) — recipe is
  not consumed, so it repeats indefinitely while ingredients last.
- **Clone vat** (`Building_PawnVatGrower`) — repeats while a matching **genome** item is
  available; stops when you run out of genomes.

Note: the newer bill-based organ vat (`Building_OrganVat`) gets the toggle but its
auto-restart path may differ — report if it doesn't repeat and it'll be handled.

## Requirements
- Questionable Ethics Enhanced (Continued)
- Harmony

No files from QEE are included or redistributed — this mod only patches at runtime
and requires QEE to be installed.

## Credits
Questionable Ethics Enhanced — KongMD, continued by Mlie.

## License
MIT (this patch only). See [LICENSE](LICENSE).
