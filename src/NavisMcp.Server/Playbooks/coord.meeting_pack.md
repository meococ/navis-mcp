# Meeting pack

Build a coordination meeting agenda from clustered clash evidence.

## Steps

1. Start from clustered clash results (`nwd_cluster_clash_results`).
2. For each top cluster: capture selection snapshot and create an issue pack.
3. Optionally `nwd_export_bcf` from issue pack JSON for BIM 360 / Solibri / other BCF tools.
4. Assemble agenda under `reports/`:
   - Cluster key, count, model pair, distance bucket
   - Snapshot path
   - Proposed status / owner
5. Summarize what is visually visible, property-supported, and uncertain.

## Deliverables

- Issue packs (JSON + markdown) under `reports/`
- Optional BCF zip stubs
- Short verbal summary for the meeting (not a dump of all clash rows)
