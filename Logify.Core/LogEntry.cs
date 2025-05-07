namespace Logify.Core
{
    public class LogEntry
    {
	public DateTime Timestamp { get; set; } = DateTime.UtcNow;
	public string Content { get; set; } = string.Empty;
	public List<string> Tags { get; set; } = new List<string>();
    }
}
