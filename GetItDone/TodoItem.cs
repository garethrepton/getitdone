public class TodoItem
{
    public int Id { get; set; }
    public string Text { get; set; } = "";
    public bool Done { get; set; }
    public DateOnly? DueDate { get; set; }
    public bool IsAsap { get; set; }
    public bool IsBackground { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int LoggedMinutes { get; set; }
    public string? Notes { get; set; }
}
