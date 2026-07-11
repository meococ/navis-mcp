# ADR 0001: Single product SemVer

## Status
Accepted

## Decision
Server, plug-in, and contracts share one SemVer stamped from `Directory.Build.props` and Git tags (`vX.Y.Z`).

## Consequences
Ship one GitHub Release zip. `nwd_health_check` warns on version mismatch.
