# Contributing to ArIEC103

ArIEC103 is intended to remain legally clean, auditable, and safe for corporate engineering review.

## Clean-room rules

- Do not copy source code from commercial, GPL, or unclear-license IEC 60870 protocol stacks.
- Do not port internal class structures, parser logic, state machines, or test suites from third-party protocol libraries.
- Use independently written code, public protocol behavior, legally shareable traces, and self-generated test vectors.
- Do not upload real customer, vendor, utility, or project captures unless they are fully sanitized and legally shareable.
- Keep all dependencies documented in `THIRD_PARTY_NOTICES.md`.

## What belongs in GitHub

Commit only:

- source code under `src/` and `tests/`
- static landing page files under `landing/`
- documentation under `docs/`
- sanitized samples under `samples/`
- legal/config files such as `LICENSE`, `NOTICE`, `.gitignore`, `.gitattributes`, and `.editorconfig`

Do not commit:

- `bin/`, `obj/`, `dist/`, `out/`, `node_modules/`
- generated reports
- real field logs or confidential traces
- PDFs, MSG files, PCAP files, spreadsheets, installers, ZIP packages, or exported binaries
- secrets, tokens, environment files, or production appsettings

## License

By contributing, you agree that your contribution is provided under the Apache License, Version 2.0.
