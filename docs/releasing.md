# Releasing

## Overview

- Release binaries are **not committed** to the repo.
- End users download the Windows executable from **GitHub Releases** (asset: `Pomodoro.exe`).
- Publishing a GitHub Release triggers a GitHub Actions workflow that builds `dist/Pomodoro.exe` and uploads it to that Release.
- Build outputs like `dist/` are **gitignored** (see `.gitignore`).

Workflow location: `.github/workflows/release-build.yml`.

Build script used by the workflow: `scripts/build-dist.ps1`.

## Maintainer release steps

1) Ensure `main` is clean and green (CI passing).

2) Decide the release version/tag (example: `v0.1.0`).

   If you also maintain a version number anywhere else (changelog, docs, etc.), bump that first.

3) Create the tag + Release (pick one approach):

   **A) GitHub UI (recommended)**

   - GitHub → Releases → **Draft a new release**
   - Create a new tag like `v0.1.0` targeting `main`
   - Fill in release notes
   - Click **Publish release**

   **B) Git CLI + GitHub UI**

   - Create a tag locally:

     ```powershell
     git tag -a v0.1.0 -m "v0.1.0"
     ```

   - Push the tag:

     ```powershell
     git push --tags
     ```

   - Then create/publish the Release in GitHub using that tag.

4) Wait for the workflow to complete

   - When the Release is **published**, GitHub Actions runs the workflow in `.github/workflows/release-build.yml`.
   - The workflow runs `scripts/build-dist.ps1`, which produces `dist/Pomodoro.exe`.
   - The workflow uploads `dist/Pomodoro.exe` to the published Release as a Release asset.

5) Verify the Release assets

   - Open the Release page on GitHub
   - Under **Assets**, confirm `Pomodoro.exe` exists
   - Download and smoke-test the EXE on Windows

## Notes

- `dist/` is intended as a local/CI output folder only. Do not commit it.
- If you need a local build for testing before you publish a Release, run the same build script locally (but keep outputs uncommitted):

  ```powershell
  pwsh -NoProfile -File scripts/build-dist.ps1
  ```
