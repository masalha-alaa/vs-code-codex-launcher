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
