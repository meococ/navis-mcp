# Navisworks version matrix

## Current (v0.1.x)

| Product | Series | Status |
|---------|--------|--------|
| Navisworks Manage 2026 | `Nw23` | **Supported** |
| Manage 2025 / earlier | — | Not supported |
| Freedom / Simulate | — | Not supported |

Supporting a single Manage year is intentional for alpha: one API surface, one CI story, honest docs.

## Multi-year (Phase C — only with CI)

Do **not** advertise multi-year support until:

1. A self-hosted Windows runner (or maintainer artifact pipeline) builds each target year.
2. `PackageContents.xml` `SeriesMin` / `SeriesMax` and plugin projects are split or multi-targeted.
3. Smoke checklist passes on each year for: health, find, snapshot, clash create/run/export, section box, clash status update.

Until then, forks that retarget HintPaths do so at their own risk.
