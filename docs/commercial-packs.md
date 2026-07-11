# Commercial packs (optional layer)

The OSS core ships:

- Bridge + Clash Detective + evidence/BCF packs
- Global EN ontology packs (`en.structure`, `en.mep`, `en.architecture`, `en.civil`)
- Locale overlays (e.g. `vi.*`) as samples
- Coordination playbooks

## What a commercial layer may add (not required for OSS)

| Pack type | Examples |
|-----------|----------|
| Firm ontology | Naming standards, discipline aliases, false-positive notes |
| Clash policy | Pair matrix, tolerances, hard vs clearance defaults |
| Meeting templates | Branded evidence/meeting pack layouts |
| Support | Signed installers, SLA, NW version matrix CI |

Commercial packs must **not** hardcode a single client project’s NWF names or private clash SOPs into the public product tree. Job-specific scripts stay in the BIM job folder (`private-scripts/`).

## Loading packs

Today packs are compiled into `DomainOntology` / playbook markdown. Future releases may load JSON/YAML overlays from `--project-root/packs/` without requiring a rebuild.
