# ADR 0002: UI-thread only Navisworks API

## Status
Accepted

## Decision
All Autodesk Navisworks API calls run on the existing UI-thread dispatcher inside the plug-in. The named-pipe listener never touches the API directly.

## Consequences
Slight latency under load; avoids crash classes from off-thread API use.
