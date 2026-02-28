# GetItDone — Console Todo App

A fast, minimal console todo list app. JSON file storage, one-line commands.

## Technology Stack

| Concern | Choice |
|---|---|
| Runtime | .NET 9 (net9.0) |
| Persistence | `System.Text.Json` → `todos.json` |

## Project Structure

```
GetItDone/
├── GetItDone.sln
└── GetItDone/
    ├── GetItDone.csproj
    ├── Program.cs         # Console UI loop
    ├── TodoItem.cs        # POCO model (Id, Text, Done)
    └── TodoStore.cs       # JSON load/save + CRUD
```

## Commands (at runtime)

| Input | Action |
|---|---|
| `<text>` + Enter | Add a new todo |
| `<number>` + Enter | Toggle done/undone |
| `rm <number>` | Remove a todo |
| `q` | Quit |

## Data

Todos are stored in `~/GetItDone/todos.json` (user profile directory). Format:

```json
[{"id":1,"text":"Buy milk","done":false}]
```

---

## Available Agents

Use these agents in parallel when working on independent tasks. Invoke multiple agents simultaneously where possible. These agents are available in the global agent store.

### `dotnet-tool`
Use for: scaffolding projects, adding NuGet packages, running builds and tests, project configuration, `dotnet` CLI tasks.

### `planning-agent`
Use for: researching requirements and designing implementation plans before writing code. Give it a feature description and it returns a step-by-step implementation plan.
Use this before starting any non-trivial feature.

### `security-reviewer`
Use proactively after implementing any feature that touches: file paths, network calls, API keys, PII, or user-provided input.
FLAGS dangerous patterns — treat its output as a blocking review.

### `network-audit`
Use proactively after any code changes that could introduce outbound network calls. Run in parallel with `security-reviewer` after completing a feature.

### `dangerous-code`
Use before committing. Scans for patterns inappropriate for production: hardcoded credentials, debug backdoors, unsafe operations.

### `package-audit`
Use when adding or updating NuGet packages. Checks for security issues, popularity, and known vulnerabilities.

### `git-agent`
Use for: all git workflow operations — branching, committing (Conventional Commits format), pull requests, merging, tagging releases.

### `obvious-bug`
Use to find obvious bugs by understanding the application's intent and identifying things that are not connected up, wired in, or reachable.

### `app-summariser`
Use when you need a high-level summary of what the app or a module does — useful for onboarding context or documentation.

---

## Development Workflow

1. Before committing, run `dangerous-code` + `security-reviewer` + `network-audit` in parallel
2. Use `git-agent` to commit (Conventional Commits)
3. Use `package-audit` when adding any new NuGet dependency
