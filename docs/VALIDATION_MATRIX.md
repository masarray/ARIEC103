# ArIEC103 Validation Matrix

Use this file to record relay, simulator, and bench validation results before publishing a stable release.

## Release validation status

| Item | Status | Notes |
|---|---|---|
| Build on Windows | Pending | Run GitHub Actions and local build. |
| Protocol smoke tests | Pending | Run `tests/ArIEC103.Protocol.Tests`. |
| Portable package created | Pending | Run `scripts/publish-windows-portable.ps1`. |
| Portable package opened | Pending | Start desktop from ZIP extract. |
| CLI help opened | Pending | Run `Open-CLI-Help.bat`. |
| Simulator master run | Pending | Use deterministic simulator mode. |
| Real relay validation | Pending | Add sanitized relay results below. |

## Device validation table

Avoid posting customer-sensitive device identifiers if the repository is public. Use sanitized names when needed.

| Test ID | Device / Simulator | Connection | Baud / Parity | Link / CA | Reset Link | GI | Class 2 | Class 1 / ACD | Time Sync | Mapping | Evidence Export | Result | Notes |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| VAL-001 | Built-in simulator | Local/simulated | N/A | 1 / 1 | N/A | Pass | Pass | Pass | N/A | Example | Pass | Pending | Run before release. |
| VAL-002 | IEC-103 relay A | RS-485 | 9600 / Even | 1 / 1 | Pending | Pending | Pending | Pending | Pending | Pending | Pending | Pending | Sanitized field test. |
| VAL-003 | IEC-103 relay B | RS-485 | 19200 / Even | 1 / 1 | Pending | Pending | Pending | Pending | Pending | Pending | Pending | Pending | Sanitized field test. |

## Minimum pass criteria for Public Beta

- Source build passes.
- Protocol smoke tests pass.
- Portable package can be created.
- Desktop app opens from portable package.
- Simulator mode can produce Markdown evidence.
- README, landing page, and release notes do not overclaim stable field validation.

## Minimum pass criteria for Stable Release

- At least two independent relay/simulator validations pass.
- GI behavior is observed and documented.
- Class 2 polling is stable for a defined duration.
- Class 1 drain behavior is verified with at least one event source.
- Timeout/checksum/malformed recovery is reviewed.
- Exported evidence is reviewed for privacy and clarity.
