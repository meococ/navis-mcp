# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2026-07-11

### Added

- Clean OSS product layout (`src/`, `tests/`, `docs/`, `samples/`, `packaging/`, `.github/`).
- Apache-2.0 license and Autodesk NOTICE; CODE_OF_CONDUCT, MAINTAINERS, Dependabot, EditorConfig.
- Session token required on every bridge method; named-pipe and token-file ACL hardening; constant-time token compare.
- Honest `not_supported_by_api` error code for stub capabilities; tool failures expose `ok=false` + `errorCode`.
- MCP prompts/resources for coordination playbooks (Phase 0, clash batch, meeting pack, safety preflight).
- Default toolsets (`core`/`search`/`clash`/`report`/`write`); stubs only via `--toolsets advanced`.
- Domain ontology: EN core packs + `vi.*` locale overlays.
- Clash clustering, issue/evidence packs + open BCF export, federation inventory, visual QA batch helpers.
- Live Clash Detective create/run/export plus `nwd_update_clash_result` (status/comment/assignment).
- `nwd_set_section_box` (ClipPlanes box fit/clear).
- Real search-set create/export via Navisworks `Search` + `SelectionSet` API.
- `nwd_run_federated_preflight` and `nwd_create_evidence_pack`.
- Release gate: zip fails without plugin DLL; `scripts/generate-mcp-config.ps1`; FakeBridge CI smoke.
- Capability matrix, marketplace, commercial-packs, and version-matrix docs.

### Security

- Close auth gap on read bridge methods.
- Document threat model in `SECURITY.md` and `docs/security.md`.
