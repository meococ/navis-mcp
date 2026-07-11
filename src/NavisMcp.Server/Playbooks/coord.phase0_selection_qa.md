# Phase 0 selection QA

Use before official clash batches to prove selection hygiene.

## Steps

1. `nwd_list_targets` — prefer a target with `documentTitle`.
2. `nwd_health_check` and `nwd_get_document_info`.
3. `nwd_get_federation_map` — confirm linked models and units.
4. `nwd_list_domain_ontology` — pick EN core packs (`en.structure`, `en.mep`, …) and optional locale overlays (`vi.*`).
5. Scoped `nwd_find_items` / `nwd_select_by_search` with accent-insensitive queries.
6. `nwd_run_phase0_selection_qa` — write Must/Should/QA/List/Exclude placeholders under `reports/`.
7. Spot-check with `nwd_capture_viewport(framing="selection", restoreView=true)`.

## Exit criteria

- Selection counts are explainable per discipline pack.
- False-positive notes from ontology packs are acknowledged.
- No official clash run until Phase 0 pack is written.
