using System.Text.Json;

public class TodoStore
{
    public static readonly string FilePath = Path.Combine(
        AppContext.BaseDirectory, "todos.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public List<TodoItem> Items { get; private set; } = [];

    public void Load()
    {
        if (!File.Exists(FilePath))
            return;

        var json = File.ReadAllText(FilePath);
        Items = JsonSerializer.Deserialize<List<TodoItem>>(json, JsonOptions) ?? [];
    }

    public void Save()
    {
        var json = JsonSerializer.Serialize(Items, JsonOptions);
        File.WriteAllText(FilePath, json);
    }

    public TodoItem Add(string text, DateOnly? dueDate = null, bool isAsap = false, bool isBackground = false)
    {
        var nextId = Items.Count > 0 ? Items.Max(t => t.Id) + 1 : 1;
        var item = new TodoItem
        {
            Id = nextId,
            Text = text,
            CreatedAt = DateTime.Now,
            DueDate = (isAsap || isBackground) ? null : dueDate,
            IsAsap = isAsap && !isBackground,
            IsBackground = isBackground
        };
        Items.Add(item);
        Save();
        return item;
    }

    public bool Toggle(int id)
    {
        var item = Items.FirstOrDefault(t => t.Id == id);
        if (item is null) return false;
        item.Done = !item.Done;
        item.CompletedAt = item.Done ? DateTime.Now : null;
        Save();
        return true;
    }

    public bool Remove(int id)
    {
        var item = Items.FirstOrDefault(t => t.Id == id);
        if (item is null) return false;
        Items.Remove(item);
        Save();
        return true;
    }

    public bool SetDueDate(int id, DateOnly? date)
    {
        var item = Items.FirstOrDefault(t => t.Id == id);
        if (item is null) return false;
        item.DueDate = date;
        item.IsAsap = false;
        item.IsBackground = false;
        Save();
        return true;
    }

    public bool SetAsap(int id, bool asap)
    {
        var item = Items.FirstOrDefault(t => t.Id == id);
        if (item is null) return false;
        item.IsAsap = asap;
        if (asap) { item.DueDate = null; item.IsBackground = false; }
        Save();
        return true;
    }

    public bool SetBackground(int id, bool background)
    {
        var item = Items.FirstOrDefault(t => t.Id == id);
        if (item is null) return false;
        item.IsBackground = background;
        if (background) { item.DueDate = null; item.IsAsap = false; }
        Save();
        return true;
    }

    public bool SetNotes(int id, string? notes)
    {
        var item = Items.FirstOrDefault(t => t.Id == id);
        if (item is null) return false;
        item.Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        Save();
        return true;
    }

    public bool LogTime(int id, int minutes)
    {
        var item = Items.FirstOrDefault(t => t.Id == id);
        if (item is null) return false;
        item.LoggedMinutes += minutes;
        Save();
        return true;
    }
}
