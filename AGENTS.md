# Agent notes

## Release process

Releases are automated by `.github/workflows/release.yml`.

For a new release, update all three version fields in `src/CodexProjectLauncher/CodexProjectLauncher.csproj`:

- `<Version>` — semantic version, e.g. `1.1.0`
- `<AssemblyVersion>` — matching four-part version, e.g. `1.1.0.0`
- `<FileVersion>` — matching four-part version, e.g. `1.1.0.0`

Then update `RELEASE_NOTES.md` and push the changes to `master`.

The release workflow reads `<Version>`, publishes the self-contained Windows x64 app, creates tag `vX.Y.Z`, creates the GitHub Release, and uploads:

- `CodexProjectLauncher.exe`
- `CodexProjectLauncher-vX.Y.Z-win-x64.zip`
- `SHA256SUMS.txt`

Do not manually create the normal release tag or GitHub Release. If that tag already exists, increment the version instead of overwriting the existing release.

## Signing policy

The application is intentionally unsigned at present. Do not add code-signing steps, certificates, or signing secrets unless the maintainer explicitly requests it.
