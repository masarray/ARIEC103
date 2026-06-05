# ArIEC103 Roadmap

ArIEC103 is currently positioned as a public beta / release candidate for IEC 60870-5-103 master testing and relay communication evidence. The next work should prioritize trust, validation, and repeatable field workflow instead of adding random features.

## Phase 1 — Release hardening

Goal: make the public repository look credible, easy to verify, and safe to download.

- Keep GitHub Actions CI green on every push.
- Keep GitHub Pages deploy status visible.
- Publish a Windows x64 portable package from a clean release build.
- Add SHA256 checksums for release artifacts.
- Keep README badges and release notes current.
- Keep public reports free from local workstation paths and customer project folders.

## Phase 2 — Field validation

Goal: prove the master behavior against real relay behavior, not only source-code inspection.

- Maintain a relay/simulator validation matrix.
- Add sanitized IEC-103 frame test vectors under `samples/test-vectors/`.
- Expand ASDU decoder tests using known-good frames.
- Test FCB retry behavior under timeout, malformed frame, checksum error, and relay busy conditions.
- Record supported baudrate, parity, link address, GI, Class 1, Class 2, and measurand behavior.

## Phase 3 — Operator workflow

Goal: make the app easier for protection, SCADA, FAT, and commissioning engineers who need fast answers.

- Add save/load connection profiles.
- Add recent profiles for frequently used relay settings.
- Add serial health diagnostics: RX/TX activity, timeout rate, checksum error rate, malformed frame rate, and likely wrong-address symptoms.
- Add a guided test checklist: connect, reset link, GI, Class 2 polling, Class 1 event observation, evidence export.
- Improve troubleshooting messages so the user knows what to check next.

## Phase 4 — FAT evidence

Goal: produce a reviewable evidence package that can be attached to FAT/SAT records.

- Add one-click formatted PDF report.
- Include session metadata, app version, COM settings, relay address, duration, counters, warnings, and raw evidence appendix.
- Add pass/fail style assessment summary without overclaiming formal compliance.
- Add export package ZIP containing Markdown, JSON, PDF, and sanitized raw trace.

## Phase 5 — Analyzer maturity

Goal: make ArIEC103 useful beyond quick bench testing.

- Add capture replay mode.
- Add compare-two-sessions workflow.
- Improve long-duration test handling and evidence ring buffers.
- Add stronger event/value filtering.
- Refactor desktop code-behind into maintainable services and view models before adding more UI-heavy features.
