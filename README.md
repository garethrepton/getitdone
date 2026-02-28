# GetItDone

A fast, minimal console todo list app built with .NET 9 and [Spectre.Console](https://spectreconsole.net/).

JSON file storage, keyboard shortcuts, multiple views, time logging, and notes — all from your terminal.

## Vibe Coding Disclaimer

This project was **vibe coded** — built collaboratively with AI (Claude) through natural language conversation. The code was generated, reviewed, and iterated on through conversational prompts rather than traditional manual development. It works, it's been scanned for security issues, but it carries the spirit of rapid AI-assisted creation. Use at your own discretion.

## Install

```bash
dotnet build
```

The compiled binary will be in `GetItDone/bin/Debug/net9.0/`.

## Usage

Run the app and use keyboard shortcuts or the interactive menu:

```
[a] Add   [d] Done   [n] Notes   [v] View   [q] Quit   [Enter] Menu
```

### Schedule View (default)

```
  3 open | 1 done

╔══ GetItDone ════════════════════════════════════════════════════╗
║ ╭──────┬────────┬──────────────────────────────────────┬───────╮ ║
║ │   ID │ Status │ Todo                                 │ Logged│ ║
║ ├──────┼────────┼──────────────────────────────────────┼───────┤ ║
║ │      │        │ ── ASAP ──────────────────────────── │       │ ║
║ │    3 │ [  ]   │ Fix login bug                        │     - │ ║
║ │      │        │ ── TODAY ─────────────────────────── │       │ ║
║ │    1 │ [  ]   │ Write project README                 │  45m  │ ║
║ │      │        │ ── TOMORROW ──────────────────────── │       │ ║
║ │    2 │ [  ]   │ Review pull requests                 │     - │ ║
║ │      │        │ ── COMPLETED ─────────────────────── │       │ ║
║ │    4 │ [x]    │ ~~Set up CI pipeline~~               │ 1h 30m│ ║
║ ╰──────┴────────┴──────────────────────────────────────┴───────╯ ║
╚══════════════════════════════════════════════════════════════════╝
```

### Week + Notes View

```
╔══ GetItDone — This Week + Notes ════════════════════════════════╗
║ ╭──────┬────────┬──────────────────────────────────────┬───────╮ ║
║ │   ID │ Status │ Todo                                 │ Logged│ ║
║ ├──────┼────────┼──────────────────────────────────────┼───────┤ ║
║ │      │        │ ── TODAY ─────────────────────────── │       │ ║
║ │    1 │ [  ]   │ Deploy v2 to staging                 │  20m  │ ║
║ │      │        │   1. Run migrations                  │       │ ║
║ │      │        │   2. Smoke test auth flow             │       │ ║
║ │      │        │   3. Check logs for errors           │       │ ║
║ │      │        │ ── Wed, Mar 04 ────────────────────  │       │ ║
║ │    5 │ [  ]   │ Prepare demo for stakeholders        │     - │ ║
║ │      │        │   slides are in Google Drive          │       │ ║
║ ╰──────┴────────┴──────────────────────────────────────┴───────╯ ║
╚══════════════════════════════════════════════════════════════════╝
```

### Interactive Menu

```
What would you like to do?

  Tasks
  > Add todo
    Complete
    Uncomplete
    Set due date
    Edit notes
    Log time
    Remove
  View
    View: Week
  Storage
    Show file path
    Open storage file

    Quit
```

## Views

| Shortcut | View | Description |
|----------|------|-------------|
| `v` x1 | **Week** | Only items due within 7 days |
| `v` x2 | **Week + Notes** | Week view with inline notes |
| `v` x3 | **List** | All items in a flat list |
| `v` x4 | **Schedule** | Grouped by ASAP, today, tomorrow, future dates |

## Data Storage

Todos are stored as JSON in your user profile directory:

```
~/GetItDone/todos.json
```

```json
[
  {
    "id": 1,
    "text": "Buy milk",
    "done": false,
    "dueDate": "2026-03-01",
    "isAsap": false,
    "isBackground": false,
    "loggedMinutes": 0,
    "notes": null
  }
]
```

## Tech Stack

- **.NET 9** (console app)
- **Spectre.Console** (terminal UI rendering)
- **System.Text.Json** (persistence)

## License

[MIT](LICENSE)
