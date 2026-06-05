# ArIEC103 Landing Page

Static user-facing landing page for ArIEC103.

The page should explain, in practical language:

- what the software does
- who it is for
- the main workflow screens
- polling behavior in field-test terms
- evidence/export benefits
- how to download and start the desktop app

Keep the page focused on features, workflow, and usage. Avoid internal implementation debates or technical details that ordinary users do not need.

The public page uses optimized screenshots from `assets/screenshots/`. Source screenshots may be kept in `screenshot/` only when they are current, correct, and safe to publish.

## GitHub Pages deployment notes

This repository includes compatibility files for three common GitHub Pages setups:

1. **GitHub Actions**: `.github/workflows/pages.yml` publishes the `landing/` folder.
2. **Deploy from branch / root**: root `index.html` redirects visitors to `landing/`.
3. **Deploy from branch / docs**: `docs/index.html` contains a deployable copy of the landing page.

If the public URL returns 404, check repository **Settings → Pages**:

- Recommended source: **GitHub Actions**.
- If using branch deployment, select either `/root` or `/docs` and wait until the Pages build finishes.
- Make sure the workflow is allowed to run on the actual default branch, either `main` or `master`.
