# VS Code Codex Launcher

A small Windows launcher for opening VS Code projects with isolated Codex histories.

Each saved project gets its own `CODEX_HOME` and VS Code `--user-data-dir`, allowing multiple projects to be open simultaneously without mixing Codex conversation history.

## Usage

1. Open **Codex Project Launcher**.
2. Click **Add Project** and select a project folder.
3. Select the project and click **Open in VS Code**.

Project list: `%LOCALAPPDATA%\\CodexProjectLauncher\\projects.json`

Default isolated state:

- `%LOCALAPPDATA%\\CodexHomes\\<project-id>`
- `%LOCALAPPDATA%\\VSCodeInstances\\<project-id>`

The locations can be changed from **Settings**.

## First project launch

A new project Codex home can be initialized from the existing Codex state in `CODEX_HOME` or `%USERPROFILE%\\.codex`. Only `auth.json`, `config.toml`, and `environments.toml` are copied when present. Sessions are never copied.

A new isolated VS Code profile is seeded once with the main `settings.json`, `keybindings.json`, and snippets. The launcher forces `chat.editor.codex.preferAgentHost` to `false` so the official OpenAI Codex extension is used.

## Build

The application targets **.NET 8 / WPF / Windows x64**. GitHub Actions builds a self-contained Windows executable on every push to `master` and on pull requests. The artifact is named `CodexProjectLauncher-win-x64`.

## Release process

The release workflow is automated by `.github/workflows/release.yml`.

For every new release:

1. Update the version metadata in `src/CodexProjectLauncher/CodexProjectLauncher.csproj`:
   - `<Version>` → semantic version such as `1.1.0`
   - `<AssemblyVersion>` → matching four-part version such as `1.1.0.0`
   - `<FileVersion>` → matching four-part version such as `1.1.0.0`
2. Update `RELEASE_NOTES.md` for that version.
3. Commit and push those changes to `master`.

The release workflow then:

- reads `<Version>` from the project file;
- builds a self-contained Windows x64 executable;
- creates the corresponding Git tag, for example `v1.1.0`;
- creates the GitHub Release;
- uploads `CodexProjectLauncher.exe`, the Windows x64 ZIP, and `SHA256SUMS.txt`;
- marks the new release as the latest release.

Do **not** manually create the tag or GitHub Release during the normal release flow. If a release with that tag already exists, the workflow deliberately skips release creation; bump the version for the next release instead of overwriting an existing release.

### Code signing

Releases are intentionally **unsigned** for now. Windows may therefore show an **Unknown Publisher** / SmartScreen warning. Do not add a signing step or signing secrets unless the maintainer explicitly decides to enable code signing later.
