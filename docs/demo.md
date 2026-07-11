# Demo script (60 seconds)

Record a short GIF/video for the README hero using this path on **Autodesk Samples** (or another public sample model):

1. Open a federated NWF / sample model in Navisworks Manage 2026.
2. Agent: `nwd_run_federated_preflight`
3. Agent: `nwd_create_search_set` for a discipline term from `nwd_list_domain_ontology` (e.g. `en.structure` / railing)
4. Agent: `nwd_capture_viewport` with `framing=selection`
5. Agent: create clash test from two search/selection sets → run → `nwd_export_clash_report`
6. Agent: `nwd_create_evidence_pack` for one priority clash
7. Optional: `nwd_set_section_box` with `mode=fit` around the clash items, then `nwd_update_clash_result`

Save the recording as `docs/assets/demo.gif` (git LFS optional) and link it from README when available. See [assets/README.md](assets/README.md).
