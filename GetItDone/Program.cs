using System.Diagnostics;
using Spectre.Console;

var store = new TodoStore();
store.Load();
var viewMode = "schedule"; // "schedule", "week", or "list"

while (true)
{
    AnsiConsole.Clear();

    // Summary bar
    var total = store.Items.Count;
    var done = store.Items.Count(i => i.Done);
    var open = total - done;
    var overdue = store.Items.Count(i => !i.Done && i.DueDate.HasValue
        && i.DueDate.Value < DateOnly.FromDateTime(DateTime.Now));
    var asap = store.Items.Count(i => !i.Done && i.IsAsap);

    var parts = new List<string> { $"[blue]{open}[/] open", $"[green]{done}[/] done" };
    if (overdue > 0) parts.Add($"[red]{overdue}[/] overdue");
    if (asap > 0) parts.Add($"[magenta]{asap}[/] asap");

    AnsiConsole.MarkupLine($"  {string.Join("[dim] | [/]", parts)}");
    AnsiConsole.WriteLine();

    RenderTable(store, viewMode);

    AnsiConsole.MarkupLine("[dim]  [[a]] Add  [[d]] Done  [[n]] Notes  [[v]] View  [[q]] Quit  [[Enter]] Menu[/]");
    AnsiConsole.WriteLine();

    // Check for keyboard shortcut
    var key = Console.ReadKey(true);

    if (key.Key == ConsoleKey.A)
    {
        HandleAdd(store);
        continue;
    }
    if (key.Key == ConsoleKey.D)
    {
        HandleComplete(store);
        continue;
    }
    if (key.Key == ConsoleKey.N)
    {
        HandleEditNotes(store);
        continue;
    }
    if (key.Key == ConsoleKey.V)
    {
        viewMode = viewMode switch
        {
            "schedule" => "week",
            "week" => "week-notes",
            "week-notes" => "list",
            _ => "schedule"
        };
        continue;
    }
    if (key.Key == ConsoleKey.Q)
    {
        return;
    }

    // Any other key (including Enter) → show the full menu
    var nextView = viewMode switch
    {
        "schedule" => "View: Week",
        "week" => "View: Week + Notes",
        "week-notes" => "View: List",
        _ => "View: Schedule"
    };

    var choice = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("[bold]What would you like to do?[/]")
            .HighlightStyle(new Style(Color.Cyan1))
            .AddChoiceGroup("Tasks", "Add todo", "Complete", "Uncomplete", "Set due date", "Edit notes", "Log time", "Remove")
            .AddChoiceGroup("View", nextView)
            .AddChoiceGroup("Storage", "Show file path", "Open storage file")
            .AddChoiceGroup("", "Quit"));

    switch (choice)
    {
        case "Add todo":
            HandleAdd(store);
            break;
        case "Complete":
            HandleComplete(store);
            break;
        case "Uncomplete":
            HandleUncomplete(store);
            break;
        case "Set due date":
            HandleSetDueDate(store);
            break;
        case "Edit notes":
            HandleEditNotes(store);
            break;
        case "Log time":
            HandleLogTime(store);
            break;
        case "Remove":
            HandleRemove(store);
            break;
        case "View: Week":
            viewMode = "week";
            break;
        case "View: Week + Notes":
            viewMode = "week-notes";
            break;
        case "View: List":
            viewMode = "list";
            break;
        case "View: Schedule":
            viewMode = "schedule";
            break;
        case "Show file path":
            AnsiConsole.MarkupLine($"[bold]Storage:[/] [link]{Markup.Escape(TodoStore.FilePath)}[/]");
            Console.ReadKey(true);
            break;
        case "Open storage file":
            try
            {
                Process.Start(new ProcessStartInfo(TodoStore.FilePath) { UseShellExecute = true });
            }
            catch
            {
                AnsiConsole.MarkupLine($"[red]Could not open file.[/] Path: {Markup.Escape(TodoStore.FilePath)}");
                Console.ReadKey(true);
            }
            break;
        case "Quit":
            return;
    }
}

static void RenderTable(TodoStore store, string viewMode)
{
    var table = viewMode switch
    {
        "week" => BuildWeekTable(store),
        "week-notes" => BuildWeekNotesTable(store),
        "list" => BuildListTable(store),
        _ => BuildScheduleTable(store)
    };

    var viewTag = viewMode switch
    {
        "week" => " [dim]— This Week[/]",
        "week-notes" => " [dim]— This Week + Notes[/]",
        "list" => " [dim]— List[/]",
        _ => ""
    };

    var panel = new Panel(table)
        .Header($"[bold blue]GetItDone[/]{viewTag}")
        .Border(BoxBorder.Double)
        .Expand();

    AnsiConsole.Write(panel);
    AnsiConsole.WriteLine();
}

// --- Schedule View ---

static Table BuildScheduleTable(TodoStore store)
{
    var table = new Table()
        .Border(TableBorder.Rounded)
        .Expand();

    table.AddColumn(new TableColumn("[bold]ID[/]").RightAligned());
    table.AddColumn(new TableColumn("[bold]Status[/]").Centered());
    table.AddColumn("[bold]Todo[/]");
    table.AddColumn(new TableColumn("[bold]Logged[/]").RightAligned());

    if (store.Items.Count == 0)
    {
        table.AddRow("", "", "[dim]No todos yet — add one![/]", "");
        return table;
    }

    var today = DateOnly.FromDateTime(DateTime.Now);
    var tomorrow = today.AddDays(1);

    // Build sections
    var asapItems = store.Items.Where(i => !i.Done && i.IsAsap).ToList();
    var overdueItems = store.Items.Where(i => !i.Done && !i.IsAsap && i.DueDate.HasValue && i.DueDate.Value < today)
        .OrderBy(i => i.DueDate!.Value).ToList();
    var todayItems = store.Items.Where(i => !i.Done && !i.IsAsap && i.DueDate == today).ToList();
    var tomorrowItems = store.Items.Where(i => !i.Done && !i.IsAsap && i.DueDate == tomorrow).ToList();
    var futureItems = store.Items.Where(i => !i.Done && !i.IsAsap && i.DueDate.HasValue && i.DueDate.Value > tomorrow)
        .OrderBy(i => i.DueDate!.Value).ToList();
    var backgroundItems = store.Items.Where(i => !i.Done && i.IsBackground).ToList();
    var noDateItems = store.Items.Where(i => !i.Done && !i.IsAsap && !i.IsBackground && !i.DueDate.HasValue).ToList();
    var doneItems = store.Items.Where(i => i.Done)
        .OrderByDescending(i => i.CompletedAt).ToList();

    void AddSection(string header, List<TodoItem> items, Func<TodoItem, string>? textSuffix = null)
    {
        if (items.Count == 0) return;
        var rule = new Rule(header).LeftJustified().RuleStyle("dim");
        table.AddRow(new Markup(""), new Markup(""), rule, new Markup(""));
        foreach (var item in items)
        {
            var text = Markup.Escape(item.Text);
            if (textSuffix != null)
                text += textSuffix(item);
            var logged = FormatLoggedTime(item.LoggedMinutes);
            var status = item.Done ? "[green][[x]][/]" : "[grey][[  ]][/]";
            var id = item.Done ? $"[dim]{item.Id}[/]" : item.Id.ToString();
            if (item.Done)
            {
                text = $"[dim strikethrough]{text}[/]";
                logged = $"[dim]{Markup.Escape(logged)}[/]";
            }
            table.AddRow(new Markup(id), new Markup(status), new Markup(text), new Markup(logged));
        }
    }

    AddSection("[bold magenta]ASAP[/]", asapItems);
    AddSection("[bold red]OVERDUE[/]", overdueItems, i => $" [red]({i.DueDate!.Value:MMM dd})[/]");
    AddSection("[bold yellow]TODAY[/]", todayItems);
    AddSection("[bold]TOMORROW[/]", tomorrowItems);

    // Future: group by date
    var futureByDate = futureItems.GroupBy(i => i.DueDate!.Value).OrderBy(g => g.Key);
    foreach (var group in futureByDate)
    {
        var label = group.Key.ToString("ddd, MMM dd");
        AddSection($"[bold]{Markup.Escape(label)}[/]", group.ToList());
    }

    AddSection("[dim]NO DATE[/]", noDateItems);
    AddSection("[dim cyan]BACKGROUND[/]", backgroundItems);
    AddSection("[dim]COMPLETED[/]", doneItems);

    return table;
}

// --- Week View ---

static Table BuildWeekTable(TodoStore store)
{
    var table = new Table()
        .Border(TableBorder.Rounded)
        .Expand();

    table.AddColumn(new TableColumn("[bold]ID[/]").RightAligned());
    table.AddColumn(new TableColumn("[bold]Status[/]").Centered());
    table.AddColumn("[bold]Todo[/]");
    table.AddColumn(new TableColumn("[bold]Logged[/]").RightAligned());

    var today = DateOnly.FromDateTime(DateTime.Now);
    var weekEnd = today.AddDays(7);

    // Items due within the next 7 days (+ overdue + ASAP)
    var asapItems = store.Items.Where(i => !i.Done && i.IsAsap).ToList();
    var overdueItems = store.Items.Where(i => !i.Done && !i.IsAsap && i.DueDate.HasValue && i.DueDate.Value < today)
        .OrderBy(i => i.DueDate!.Value).ToList();

    var weekItems = store.Items
        .Where(i => !i.Done && !i.IsAsap && i.DueDate.HasValue && i.DueDate.Value >= today && i.DueDate.Value <= weekEnd)
        .OrderBy(i => i.DueDate!.Value).ToList();

    if (asapItems.Count == 0 && overdueItems.Count == 0 && weekItems.Count == 0)
    {
        table.AddRow("", "", "[dim]Nothing due this week![/]", "");
        return table;
    }

    void AddSectionHeader(string label)
    {
        var rule = new Rule(label).LeftJustified().RuleStyle("dim");
        table.AddRow(new Markup(""), new Markup(""), rule, new Markup(""));
    }

    void AddRow(TodoItem item)
    {
        var text = Markup.Escape(item.Text);
        var logged = FormatLoggedTime(item.LoggedMinutes);
        table.AddRow(new Markup(item.Id.ToString()), new Markup("[grey][[  ]][/]"), new Markup(text), new Markup(logged));
    }

    if (asapItems.Count > 0)
    {
        AddSectionHeader("[bold magenta]ASAP[/]");
        foreach (var item in asapItems) AddRow(item);
    }

    if (overdueItems.Count > 0)
    {
        AddSectionHeader("[bold red]OVERDUE[/]");
        foreach (var item in overdueItems)
        {
            var text = $"{Markup.Escape(item.Text)} [red]({item.DueDate!.Value:MMM dd})[/]";
            table.AddRow(new Markup(item.Id.ToString()), new Markup("[grey][[  ]][/]"), new Markup(text), new Markup(FormatLoggedTime(item.LoggedMinutes)));
        }
    }

    // Group week items by day
    foreach (var group in weekItems.GroupBy(i => i.DueDate!.Value))
    {
        var diff = group.Key.DayNumber - today.DayNumber;
        var label = diff switch
        {
            0 => "[bold yellow]TODAY[/]",
            1 => "[bold]TOMORROW[/]",
            _ => $"[bold]{Markup.Escape(group.Key.ToString("ddd, MMM dd"))}[/]"
        };
        AddSectionHeader(label);
        foreach (var item in group) AddRow(item);
    }

    return table;
}

// --- Week + Notes View ---

static Table BuildWeekNotesTable(TodoStore store)
{
    var table = new Table()
        .Border(TableBorder.Rounded)
        .Expand();

    table.AddColumn(new TableColumn("[bold]ID[/]").RightAligned());
    table.AddColumn(new TableColumn("[bold]Status[/]").Centered());
    table.AddColumn("[bold]Todo[/]");
    table.AddColumn(new TableColumn("[bold]Logged[/]").RightAligned());

    var today = DateOnly.FromDateTime(DateTime.Now);
    var weekEnd = today.AddDays(7);
    const int notePreviewLen = 60;

    var asapItems = store.Items.Where(i => !i.Done && i.IsAsap).ToList();
    var overdueItems = store.Items.Where(i => !i.Done && !i.IsAsap && i.DueDate.HasValue && i.DueDate.Value < today)
        .OrderBy(i => i.DueDate!.Value).ToList();
    var weekItems = store.Items
        .Where(i => !i.Done && !i.IsAsap && i.DueDate.HasValue && i.DueDate.Value >= today && i.DueDate.Value <= weekEnd)
        .OrderBy(i => i.DueDate!.Value).ToList();

    if (asapItems.Count == 0 && overdueItems.Count == 0 && weekItems.Count == 0)
    {
        table.AddRow("", "", "[dim]Nothing due this week![/]", "");
        return table;
    }

    string TodoWithNotes(string todoMarkup, TodoItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Notes))
            return todoMarkup;
        return $"{todoMarkup}\n{FormatNotesPreview(item.Notes, notePreviewLen)}";
    }

    void AddSectionHeader(string label)
    {
        var rule = new Rule(label).LeftJustified().RuleStyle("dim");
        table.AddRow(new Markup(""), new Markup(""), rule, new Markup(""));
    }

    void AddRow(TodoItem item)
    {
        var text = TodoWithNotes(Markup.Escape(item.Text), item);
        table.AddRow(
            new Markup(item.Id.ToString()),
            new Markup("[grey][[  ]][/]"),
            new Markup(text),
            new Markup(FormatLoggedTime(item.LoggedMinutes)));
    }

    if (asapItems.Count > 0)
    {
        AddSectionHeader("[bold magenta]ASAP[/]");
        foreach (var item in asapItems) AddRow(item);
    }

    if (overdueItems.Count > 0)
    {
        AddSectionHeader("[bold red]OVERDUE[/]");
        foreach (var item in overdueItems)
        {
            var text = $"{Markup.Escape(item.Text)} [red]({item.DueDate!.Value:MMM dd})[/]";
            text = TodoWithNotes(text, item);
            table.AddRow(
                new Markup(item.Id.ToString()),
                new Markup("[grey][[  ]][/]"),
                new Markup(text),
                new Markup(FormatLoggedTime(item.LoggedMinutes)));
        }
    }

    foreach (var group in weekItems.GroupBy(i => i.DueDate!.Value))
    {
        var diff = group.Key.DayNumber - today.DayNumber;
        var label = diff switch
        {
            0 => "[bold yellow]TODAY[/]",
            1 => "[bold]TOMORROW[/]",
            _ => $"[bold]{Markup.Escape(group.Key.ToString("ddd, MMM dd"))}[/]"
        };
        AddSectionHeader(label);
        foreach (var item in group) AddRow(item);
    }

    return table;
}

// --- List View ---

static Table BuildListTable(TodoStore store)
{
    var table = new Table()
        .Border(TableBorder.Rounded)
        .Expand();

    table.AddColumn(new TableColumn("[bold]ID[/]").RightAligned());
    table.AddColumn(new TableColumn("[bold]Status[/]").Centered());
    table.AddColumn("[bold]Todo[/]");
    table.AddColumn("[bold]Due[/]");
    table.AddColumn(new TableColumn("[bold]Logged[/]").RightAligned());

    if (store.Items.Count == 0)
    {
        table.AddRow("", "", "[dim]No todos yet — add one![/]", "", "");
        return table;
    }

    var today = DateOnly.FromDateTime(DateTime.Now);

    var sorted = store.Items
        .OrderBy(i => i.Done)
        .ThenByDescending(i => i.Done ? i.CompletedAt : null)
        .ToList();

    foreach (var item in sorted)
    {
        var id = item.Id.ToString();
        string status;
        string text = Markup.Escape(item.Text);
        string due;
        string logged = FormatLoggedTime(item.LoggedMinutes);

        if (item.Done)
        {
            status = "[green][[x]][/]";
            text = $"[dim strikethrough]{text}[/]";
            due = item.DueDate.HasValue
                ? $"[dim]{item.DueDate.Value:yyyy-MM-dd}[/]"
                : "[dim]-[/]";
            id = $"[dim]{id}[/]";
            logged = $"[dim]{Markup.Escape(logged)}[/]";
        }
        else if (item.IsAsap)
        {
            status = "[grey][[  ]][/]";
            due = "[bold magenta]ASAP[/]";
        }
        else if (item.IsBackground)
        {
            status = "[grey][[  ]][/]";
            due = "[dim cyan]BG[/]";
        }
        else
        {
            status = "[grey][[  ]][/]";
            due = FormatDueDate(item.DueDate, today);
        }

        table.AddRow(id, status, text, due, logged);
    }

    return table;
}

static string FormatDueDate(DateOnly? dueDate, DateOnly today)
{
    if (!dueDate.HasValue)
        return "[dim]-[/]";

    var date = dueDate.Value;
    var diff = date.DayNumber - today.DayNumber;

    if (diff < 0)
        return $"[bold red]{date:yyyy-MM-dd}\nOVERDUE[/]";
    if (diff == 0)
        return $"[bold yellow]{date:yyyy-MM-dd}\nTODAY[/]";
    if (diff <= 3)
        return $"[yellow]{date:yyyy-MM-dd}[/]";

    return date.ToString("yyyy-MM-dd");
}

static string FormatNotesPreview(string notes, int maxLen)
{
    // Split inline numbered items (e.g. "1. foo 2. bar") onto separate lines
    notes = System.Text.RegularExpressions.Regex.Replace(notes, @"(?<=\S)\s+(?=\d+\.)", "\n");
    var lines = notes.Split('\n', StringSplitOptions.RemoveEmptyEntries);

    // Check if notes contain numbered lines (e.g. "1. thing", "2) thing", "1 thing")
    var numbered = lines.Where(l =>
    {
        var trimmed = l.TrimStart();
        // Match: digit(s) followed by "." or ")" or " " then text
        if (trimmed.Length < 2) return false;
        var i = 0;
        while (i < trimmed.Length && char.IsDigit(trimmed[i])) i++;
        if (i == 0) return false;
        return i < trimmed.Length && (trimmed[i] is '.' or ')' or ' ');
    }).ToList();

    if (numbered.Count >= 2)
    {
        // Render as bullet points
        var bullets = new List<string>();
        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();
            // Extract the number prefix
            var i = 0;
            while (i < trimmed.Length && char.IsDigit(trimmed[i])) i++;
            var number = trimmed[..i];
            if (i > 0 && i < trimmed.Length && (trimmed[i] is '.' or ')'))
                trimmed = trimmed[(i + 1)..].TrimStart();
            else if (i > 0 && i < trimmed.Length && trimmed[i] == ' ')
                trimmed = trimmed[(i + 1)..].TrimStart();

            if (trimmed.Length > 0)
                bullets.Add($"[dim italic]  {number}. {Markup.Escape(trimmed)}[/]");
        }
        return string.Join("\n", bullets);
    }

    // Plain text preview — single line
    var preview = notes.ReplaceLineEndings(" ");
    if (preview.Length > maxLen)
        preview = preview[..maxLen] + "...";
    return $"[dim italic]{Markup.Escape(preview)}[/]";
}

static string FormatLoggedTime(int minutes)
{
    if (minutes <= 0) return "-";
    var h = minutes / 60;
    var m = minutes % 60;
    if (h > 0 && m > 0) return $"{h}h {m}m";
    if (h > 0) return $"{h}h";
    return $"{m}m";
}

// --- PickTodo with schedule ordering and date context ---

static List<TodoItem> GetScheduleOrder(TodoStore store)
{
    var today = DateOnly.FromDateTime(DateTime.Now);
    var tomorrow = today.AddDays(1);

    var asap = store.Items.Where(i => !i.Done && i.IsAsap);
    var overdue = store.Items.Where(i => !i.Done && !i.IsAsap && i.DueDate.HasValue && i.DueDate.Value < today)
        .OrderBy(i => i.DueDate!.Value);
    var todayItems = store.Items.Where(i => !i.Done && !i.IsAsap && i.DueDate == today);
    var tomorrowItems = store.Items.Where(i => !i.Done && !i.IsAsap && i.DueDate == tomorrow);
    var future = store.Items.Where(i => !i.Done && !i.IsAsap && i.DueDate.HasValue && i.DueDate.Value > tomorrow)
        .OrderBy(i => i.DueDate!.Value);
    var noDate = store.Items.Where(i => !i.Done && !i.IsAsap && !i.IsBackground && !i.DueDate.HasValue);
    var background = store.Items.Where(i => !i.Done && i.IsBackground);
    var done = store.Items.Where(i => i.Done)
        .OrderByDescending(i => i.CompletedAt);

    return asap.Concat(overdue).Concat(todayItems).Concat(tomorrowItems)
        .Concat(future).Concat(noDate).Concat(background).Concat(done).ToList();
}

static string GetDateContext(TodoItem item)
{
    if (item.Done) return "(DONE)";
    if (item.IsAsap) return "(ASAP)";
    if (item.IsBackground) return "(BG)";
    if (!item.DueDate.HasValue) return "";

    var today = DateOnly.FromDateTime(DateTime.Now);
    var diff = item.DueDate.Value.DayNumber - today.DayNumber;

    if (diff < 0) return $"(OVERDUE: {item.DueDate.Value:MMM dd})";
    if (diff == 0) return "(TODAY)";
    if (diff == 1) return "(TOMORROW)";
    return $"({item.DueDate.Value:ddd, MMM dd})";
}

static TodoItem? PickTodo(TodoStore store, string title)
{
    if (store.Items.Count == 0)
    {
        AnsiConsole.MarkupLine("[yellow]No todos to choose from.[/]");
        Console.ReadKey(true);
        return null;
    }

    var ordered = GetScheduleOrder(store);
    var choices = ordered
        .Select(i =>
        {
            var mark = i.Done ? "[[x]]" : "[[  ]]";
            var context = GetDateContext(i);
            var suffix = string.IsNullOrEmpty(context) ? "" : $" {context}";
            var noteTag = !string.IsNullOrWhiteSpace(i.Notes) ? " [[note]]" : "";
            return $"{i.Id}. {mark} {Markup.Escape(i.Text)}{suffix}{noteTag}";
        })
        .Append("(cancel)")
        .ToList();

    var picked = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title(title)
            .PageSize(15)
            .AddChoices(choices));

    if (picked == "(cancel)") return null;

    var idStr = picked.Split('.')[0];
    if (int.TryParse(idStr, out var id))
        return store.Items.FirstOrDefault(i => i.Id == id);

    return null;
}

// --- Date extraction from add text ---

static (string text, DateOnly? date, bool isAsap, bool isBackground) ExtractDateFromText(string input)
{
    var words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    if (words.Length < 2)
        return (input, null, false, false);

    var today = DateOnly.FromDateTime(DateTime.Now);

    // Try 2-word patterns first (need ≥3 words total to leave ≥1 for text)
    if (words.Length >= 3)
    {
        var last2 = $"{words[^2]} {words[^1]}".ToLowerInvariant();

        // "on <day>" or "on <date>"
        if (words[^2].Equals("on", StringComparison.OrdinalIgnoreCase))
        {
            var dayPart = words[^1].ToLowerInvariant();
            var parsed = ParseDayName(dayPart, today);
            if (parsed.HasValue)
            {
                var text = string.Join(' ', words[..^2]);
                return (text, parsed, false, false);
            }
            // Try explicit date
            if (DateOnly.TryParse(words[^1], out var explDate))
            {
                var text = string.Join(' ', words[..^2]);
                return (text, explDate, false, false);
            }
        }

        // "next <day>"
        if (words[^2].Equals("next", StringComparison.OrdinalIgnoreCase))
        {
            var dayPart = words[^1].ToLowerInvariant();
            var parsed = ParseDayNameNextWeek(dayPart, today);
            if (parsed.HasValue)
            {
                var text = string.Join(' ', words[..^2]);
                return (text, parsed, false, false);
            }
        }
    }

    // Try 1-word patterns (need ≥2 words total)
    var lastWord = words[^1].ToLowerInvariant();
    var remaining = string.Join(' ', words[..^1]);

    if (lastWord is "today")
        return (remaining, today, false, false);

    if (lastWord is "tomorrow" or "tmw")
        return (remaining, today.AddDays(1), false, false);

    if (lastWord is "asap")
        return (remaining, null, true, false);

    if (lastWord is "background" or "bg")
        return (remaining, null, false, true);

    // Day names
    var dayDate = ParseDayName(lastWord, today);
    if (dayDate.HasValue)
        return (remaining, dayDate, false, false);

    // Relative: +3d, +1w
    if (lastWord.StartsWith('+') && lastWord.Length >= 3)
    {
        var unit = lastWord[^1];
        if (int.TryParse(lastWord.AsSpan(1, lastWord.Length - 2), out var num))
        {
            var rel = unit switch
            {
                'd' => today.AddDays(num),
                'w' => today.AddDays(num * 7),
                _ => (DateOnly?)null
            };
            if (rel.HasValue)
                return (remaining, rel, false, false);
        }
    }

    return (input, null, false, false);
}

static DateOnly? ParseDayName(string name, DateOnly today)
{
    var dayMap = new Dictionary<string, DayOfWeek>
    {
        ["mon"] = DayOfWeek.Monday, ["monday"] = DayOfWeek.Monday,
        ["tue"] = DayOfWeek.Tuesday, ["tuesday"] = DayOfWeek.Tuesday,
        ["wed"] = DayOfWeek.Wednesday, ["wednesday"] = DayOfWeek.Wednesday,
        ["thu"] = DayOfWeek.Thursday, ["thursday"] = DayOfWeek.Thursday,
        ["fri"] = DayOfWeek.Friday, ["friday"] = DayOfWeek.Friday,
        ["sat"] = DayOfWeek.Saturday, ["saturday"] = DayOfWeek.Saturday,
        ["sun"] = DayOfWeek.Sunday, ["sunday"] = DayOfWeek.Sunday
    };

    if (!dayMap.TryGetValue(name, out var dow))
        return null;

    var daysAhead = ((int)dow - (int)today.DayOfWeek + 7) % 7;
    if (daysAhead == 0) daysAhead = 7;
    return today.AddDays(daysAhead);
}

static DateOnly? ParseDayNameNextWeek(string name, DateOnly today)
{
    var dayMap = new Dictionary<string, DayOfWeek>
    {
        ["mon"] = DayOfWeek.Monday, ["monday"] = DayOfWeek.Monday,
        ["tue"] = DayOfWeek.Tuesday, ["tuesday"] = DayOfWeek.Tuesday,
        ["wed"] = DayOfWeek.Wednesday, ["wednesday"] = DayOfWeek.Wednesday,
        ["thu"] = DayOfWeek.Thursday, ["thursday"] = DayOfWeek.Thursday,
        ["fri"] = DayOfWeek.Friday, ["friday"] = DayOfWeek.Friday,
        ["sat"] = DayOfWeek.Saturday, ["saturday"] = DayOfWeek.Saturday,
        ["sun"] = DayOfWeek.Sunday, ["sunday"] = DayOfWeek.Sunday
    };

    if (!dayMap.TryGetValue(name, out var dow))
        return null;

    // "next <day>" = 8-14 days ahead (always the following week)
    var daysAhead = ((int)dow - (int)today.DayOfWeek + 7) % 7;
    if (daysAhead == 0) daysAhead = 7;
    daysAhead += 7; // push to next week
    // But cap: if already >7, we just need the next-week occurrence
    // Actually: simple approach — find this-week occurrence, add 7
    return today.AddDays(daysAhead);
}

static DateParseResult ParseDateInput(string input)
{
    var lower = input.ToLowerInvariant().Trim();

    if (lower is "none" or "clear")
        return new DateParseResult(DateParseKind.Clear, null);

    if (lower is "asap")
        return new DateParseResult(DateParseKind.Asap, null);

    if (lower is "background" or "bg")
        return new DateParseResult(DateParseKind.Background, null);

    var date = ParseDate(lower);
    if (date.HasValue)
        return new DateParseResult(DateParseKind.Date, date);

    return new DateParseResult(DateParseKind.None, null);
}

static DateOnly? ParseDate(string input)
{
    var lower = input.ToLowerInvariant().Trim();
    var today = DateOnly.FromDateTime(DateTime.Now);

    if (lower is "today")
        return today;

    if (lower is "tomorrow" or "tmw")
        return today.AddDays(1);

    if (lower is "yesterday")
        return today.AddDays(-1);

    // Relative: +3d, +1w
    if (lower.StartsWith('+') && lower.Length >= 3)
    {
        var unit = lower[^1];
        if (int.TryParse(lower.AsSpan(1, lower.Length - 2), out var num))
        {
            return unit switch
            {
                'd' => today.AddDays(num),
                'w' => today.AddDays(num * 7),
                _ => null
            };
        }
    }

    // Day names (short and full)
    var dayDate = ParseDayName(lower, today);
    if (dayDate.HasValue)
        return dayDate;

    // "next <day>"
    if (lower.StartsWith("next "))
    {
        var nextDay = ParseDayNameNextWeek(lower[5..].Trim(), today);
        if (nextDay.HasValue)
            return nextDay;
    }

    // Explicit date
    if (DateOnly.TryParse(input, out var explicitDate))
        return explicitDate;

    return null;
}

// --- Handlers ---

static void HandleAdd(TodoStore store)
{
    var input = AnsiConsole.Ask<string>("[bold]Todo text:[/]");
    if (string.IsNullOrWhiteSpace(input)) return;

    var (text, date, isAsap, isBackground) = ExtractDateFromText(input.Trim());

    if (string.IsNullOrWhiteSpace(text)) return;

    // If no date/asap/background was extracted, ask for one
    if (!date.HasValue && !isAsap && !isBackground)
    {
        var when = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold]When is this due?[/]")
                .AddChoices("No date", "Today", "Tomorrow", "ASAP", "Background task", "Pick a date"));

        switch (when)
        {
            case "Today":
                date = DateOnly.FromDateTime(DateTime.Now);
                break;
            case "Tomorrow":
                date = DateOnly.FromDateTime(DateTime.Now).AddDays(1);
                break;
            case "ASAP":
                isAsap = true;
                break;
            case "Background task":
                isBackground = true;
                break;
            case "Pick a date":
                var dateInput = AnsiConsole.Ask<string>(
                    "[bold]Due date[/] [dim](+3d, mon-sun, next tue, yyyy-mm-dd):[/]");
                date = ParseDate(dateInput.Trim());
                if (date is null)
                {
                    AnsiConsole.MarkupLine("[red]Could not parse date — adding without due date.[/]");
                    Thread.Sleep(800);
                }
                break;
        }
    }

    var item = store.Add(text, date, isAsap, isBackground);

    if (date.HasValue)
        AnsiConsole.MarkupLine($"[dim]Due date set to {date.Value:yyyy-MM-dd}[/]");
    else if (isAsap)
        AnsiConsole.MarkupLine("[dim]Marked as ASAP[/]");
    else if (isBackground)
        AnsiConsole.MarkupLine("[dim]Marked as background task[/]");

    // Brief pause so user can see feedback
    if (date.HasValue || isAsap || isBackground)
        Thread.Sleep(800);
}

static void HandleComplete(TodoStore store)
{
    var incomplete = GetScheduleOrder(store).Where(i => !i.Done).ToList();
    if (incomplete.Count == 0)
    {
        AnsiConsole.MarkupLine("[yellow]No incomplete todos.[/]");
        Console.ReadKey(true);
        return;
    }

    var choices = incomplete.Select(i =>
    {
        var context = GetDateContext(i);
        var suffix = string.IsNullOrEmpty(context) ? "" : $" {context}";
        return $"{i.Id}. {Markup.Escape(i.Text)}{suffix}";
    }).ToList();

    var selected = AnsiConsole.Prompt(
        new MultiSelectionPrompt<string>()
            .Title("[bold]Mark which todos as done?[/]")
            .PageSize(15)
            .InstructionsText("[grey](Space to toggle, Enter to confirm)[/]")
            .AddChoices(choices));

    foreach (var pick in selected)
    {
        var idStr = pick.Split('.')[0];
        if (int.TryParse(idStr, out var id))
            store.Toggle(id);
    }
}

static void HandleUncomplete(TodoStore store)
{
    var doneItems = store.Items.Where(i => i.Done)
        .OrderByDescending(i => i.CompletedAt).ToList();
    if (doneItems.Count == 0)
    {
        AnsiConsole.MarkupLine("[yellow]No completed todos.[/]");
        Console.ReadKey(true);
        return;
    }

    var choices = doneItems.Select(i =>
    {
        var when = i.CompletedAt.HasValue
            ? $" (done {i.CompletedAt.Value:MMM dd})"
            : "";
        return $"{i.Id}. {Markup.Escape(i.Text)}{when}";
    }).Append("(cancel)").ToList();

    var picked = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("[bold]Uncomplete which todo?[/]")
            .PageSize(15)
            .AddChoices(choices));

    if (picked == "(cancel)") return;

    var idStr = picked.Split('.')[0];
    if (int.TryParse(idStr, out var id))
        store.Toggle(id);
}

static void HandleSetDueDate(TodoStore store)
{
    var item = PickTodo(store, "[bold]Set due date for which todo?[/]");
    if (item is null) return;

    var currentInfo = "";
    if (item.IsAsap)
        currentInfo = " [dim](currently: ASAP)[/]";
    else if (item.IsBackground)
        currentInfo = " [dim](currently: Background)[/]";
    else if (item.DueDate.HasValue)
        currentInfo = $" [dim](currently: {item.DueDate.Value:yyyy-MM-dd})[/]";

    var input = AnsiConsole.Ask<string>(
        $"[bold]Due date[/]{currentInfo} [dim](today, tomorrow, +3d, mon-sun, yyyy-mm-dd, asap, bg, or none/clear):[/]");

    if (string.IsNullOrWhiteSpace(input)) return;

    var result = ParseDateInput(input.Trim());

    switch (result.Kind)
    {
        case DateParseKind.Clear:
            store.SetDueDate(item.Id, null);
            store.SetAsap(item.Id, false);
            store.SetBackground(item.Id, false);
            break;
        case DateParseKind.Asap:
            store.SetAsap(item.Id, true);
            break;
        case DateParseKind.Background:
            store.SetBackground(item.Id, true);
            break;
        case DateParseKind.Date:
            store.SetDueDate(item.Id, result.Date);
            break;
        default:
            AnsiConsole.MarkupLine("[red]Could not parse date.[/]");
            Console.ReadKey(true);
            break;
    }
}

static void HandleEditNotes(TodoStore store)
{
    var item = PickTodo(store, "[bold]Edit notes for which todo?[/]");
    if (item is null) return;

    if (!string.IsNullOrWhiteSpace(item.Notes))
    {
        AnsiConsole.MarkupLine("[bold]Current notes:[/]");
        var notesPanel = new Panel(Markup.Escape(item.Notes))
            .Border(BoxBorder.Rounded)
            .BorderStyle(new Style(Color.Grey))
            .Expand();
        AnsiConsole.Write(notesPanel);
        AnsiConsole.WriteLine();

        var action = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold]What would you like to do?[/]")
                .AddChoices("Edit notes", "Append to notes", "Replace notes", "Clear notes", "Cancel"));

        switch (action)
        {
            case "Edit notes":
                EditNotesLineByLine(store, item);
                break;
            case "Replace notes":
                var replace = AnsiConsole.Ask<string>("[bold]New notes:[/]");
                store.SetNotes(item.Id, replace);
                break;
            case "Append to notes":
                var append = AnsiConsole.Ask<string>("[bold]Additional notes:[/]");
                store.SetNotes(item.Id, item.Notes + "\n" + append);
                break;
            case "Clear notes":
                store.SetNotes(item.Id, null);
                break;
        }
    }
    else
    {
        var notes = AnsiConsole.Ask<string>("[bold]Notes:[/]");
        if (!string.IsNullOrWhiteSpace(notes))
            store.SetNotes(item.Id, notes);
    }
}

static void EditNotesLineByLine(TodoStore store, TodoItem item)
{
    var lines = item.Notes!.Split('\n').ToList();
    var edited = new List<string>();

    AnsiConsole.MarkupLine("[dim]For each line: edit the text, press Enter to keep as-is, or type[/] [bold]//d[/] [dim]to delete the line.[/]");
    AnsiConsole.WriteLine();

    for (var i = 0; i < lines.Count; i++)
    {
        var line = lines[i];
        var result = AnsiConsole.Prompt(
            new TextPrompt<string>($"[dim]{i + 1}.[/]")
                .DefaultValue(line)
                .AllowEmpty());

        if (result.Trim() == "//d")
            continue; // skip = delete

        if (!string.IsNullOrEmpty(result))
            edited.Add(result);
    }

    // Offer to add more lines
    while (true)
    {
        var extra = AnsiConsole.Prompt(
            new TextPrompt<string>($"[dim]{edited.Count + 1}.[/] [dim italic]new line (empty to finish):[/]")
                .AllowEmpty());

        if (string.IsNullOrWhiteSpace(extra))
            break;

        edited.Add(extra);
    }

    var final = string.Join("\n", edited);
    store.SetNotes(item.Id, string.IsNullOrWhiteSpace(final) ? null : final);
}

static void HandleLogTime(TodoStore store)
{
    var item = PickTodo(store, "[bold]Log time for which todo?[/]");
    if (item is null) return;

    var input = AnsiConsole.Ask<string>(
        "[bold]Time spent[/] [dim](e.g. 30m, 1h, 1h30m, or plain number = minutes):[/]");

    var minutes = ParseTime(input.Trim());
    if (minutes <= 0)
    {
        AnsiConsole.MarkupLine("[red]Could not parse time.[/]");
        Console.ReadKey(true);
        return;
    }

    store.LogTime(item.Id, minutes);
}

static void HandleRemove(TodoStore store)
{
    var item = PickTodo(store, "[bold]Remove which todo?[/]");
    if (item is null) return;

    if (!AnsiConsole.Confirm($"Remove [bold]{Markup.Escape(item.Text)}[/]?", defaultValue: false))
        return;

    store.Remove(item.Id);
}

static int ParseTime(string input)
{
    var lower = input.ToLowerInvariant().Trim();

    // Plain number = minutes
    if (int.TryParse(lower, out var plainMinutes))
        return plainMinutes;

    var total = 0;

    // Match hours: e.g. "1h" or "2h"
    var hIdx = lower.IndexOf('h');
    if (hIdx > 0 && int.TryParse(lower.AsSpan(0, hIdx), out var hours))
    {
        total += hours * 60;
        lower = lower[(hIdx + 1)..];
    }

    // Match minutes: e.g. "30m" or remaining after hours
    var mIdx = lower.IndexOf('m');
    if (mIdx > 0 && int.TryParse(lower.AsSpan(0, mIdx), out var mins))
    {
        total += mins;
    }
    else if (mIdx < 0 && lower.Length > 0 && int.TryParse(lower, out var trailingMins))
    {
        total += trailingMins;
    }

    return total;
}

// --- Type declarations ---

record DateParseResult(DateParseKind Kind, DateOnly? Date);

enum DateParseKind { None, Date, Asap, Background, Clear }
