# Official clash batch

Run after Phase 0 selection QA and federated preflight.

## Steps

1. `nwd_run_federated_preflight` — record units, model inventory, default tolerance (`0.100 m` unless specified).
2. Prefer **search sets** for durable sides: `nwd_create_search_set` (conditionsJson or default Item/Name contains). Fall back to `nwd_create_selection_set` for one-off explicit picks.
3. `nwd_create_clash_test` with project tolerance.
   - Use **Hard** for intersection; **Clearance** for proximity.
4. `nwd_run_clash_test` then `nwd_get_clash_results` (paged).
5. `nwd_cluster_clash_results` — group by rounded clash point / model pair / distance bucket (triage noise).
6. `nwd_export_clash_report` under `reports/`.
7. For priority clusters: select items → `nwd_capture_viewport(framing="selection", restoreView=true)` → `nwd_create_evidence_pack` (or `nwd_create_issue_pack`).
8. Optional: `nwd_export_bcf` from the evidence JSON for CDE/BIMcollab handoff.

## Pair matrix tips

- Prefer search-set / selection-set ids (`ss:*`) over huge ad-hoc `mi:*` lists.
- Keep composite object clashing on unless the model requires otherwise.
- Re-run only after selection hygiene changes; do not thrash tests.
- Exclude fastener/list-only families from official clash sides when project RULE says so.
