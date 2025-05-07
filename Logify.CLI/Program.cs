using Logify.Core;
using Spectre.Console;
using System.Text.RegularExpressions;

var service = new LogService();

if (args.Length == 0)
{
    AnsiConsole.MarkupLine("[yellow]Usage:[/] logify [add|filter|list|search|stats] [options]");
    return;
}

var command = args[0].ToLowerInvariant();

if (command == "add")
{
    if (args.Length < 2)
    {
        AnsiConsole.MarkupLine("[red]Error:[/] Missing content for log entry.");
        return;
    }

    var messageParts = args.Skip(1).ToList();
    var tags = new List<string>();

    if (messageParts.Contains("--tags"))
    {
        var tagsIndex = messageParts.IndexOf("--tags");
        tags = [.. messageParts.Skip(tagsIndex + 1)];
        messageParts = [.. messageParts.Take(tagsIndex)];
    }

    var message = string.Join(" ", messageParts);
    service.Add(message, tags);

    AnsiConsole.MarkupLine("[green]Log entry added successfully.[/]");
}
else if (command == "list")
{
    var logs = service.Load();

    if (logs.Count == 0)
    {
        AnsiConsole.MarkupLine("[yellow]No log entries found.[/]");
        return;
    }

    var table = new Table { Border = TableBorder.Rounded };
    table.AddColumn("[bold]Timestamp[/]");
    table.AddColumn("[bold]Content[/]");
    table.AddColumn("[bold]Tags[/]");

    foreach (var log in logs)
    {
        var localTime = log.Timestamp.ToLocalTime();
        var content = RenderMarkdown(log.Content);
        var tags = log.Tags.Count > 0 ? string.Join(", ", log.Tags) : "-";

        table.AddRow($"[grey]{localTime}[/]", content, $"[cyan]{tags}[/]");
    }

    AnsiConsole.Write(table);
}
else if (command == "filter")
{
    var period = args.Length > 1 ? args[1].ToLowerInvariant() : "all";
    var logs = service.Filter(period);

    if (logs.Count == 0)
    {
        AnsiConsole.MarkupLine("[yellow]No log entries found for the specified period.[/]");
        return;
    }

    var table = new Table { Border = TableBorder.Rounded };
    table.AddColumn("[bold]Timestamp[/]");
    table.AddColumn("[bold]Content[/]");
    table.AddColumn("[bold]Tags[/]");

    foreach (var log in logs)
    {
        var localTime = log.Timestamp.ToLocalTime();
        var content = RenderMarkdown(log.Content);
        var tags = log.Tags.Count > 0 ? string.Join(", ", log.Tags) : "-";

        table.AddRow($"[grey]{localTime}[/]", content, $"[cyan]{tags}[/]");
    }

    AnsiConsole.Write(table);
}
else if (command == "search")
{
    if (args.Length < 2)
    {
        AnsiConsole.MarkupLine("[red]Please provide search query.[/]");
        return;
    }

    var query = string.Join(' ', args.Skip(1));
    var results = service.Search(query);

    if (results.Count == 0)
    {
        AnsiConsole.MarkupLine("[yellow]No matching logs found.[/]");
        return;
    }

    var table = new Table { Border = TableBorder.Rounded };
    table.AddColumn("[bold]Timestamp[/]");
    table.AddColumn("[bold]Content[/]");
    table.AddColumn("[bold]Tags[/]");

    foreach (var log in results)
    {
        var localTime = log.Timestamp.ToLocalTime();
        var content = RenderMarkdown(log.Content);
        var tags = log.Tags.Count > 0 ? string.Join(", ", log.Tags) : "-";

        table.AddRow($"[grey]{localTime}[/]", content, $"[cyan]{tags}[/]");
    }

    AnsiConsole.Write(table);
}
else if (command == "stats")
{
    var stats = service.StatsByTag();

    if (stats.Count == 0)
    {
        AnsiConsole.MarkupLine("[yellow]No tags found.[/]");
        return;
    }

    var table = new Table { Border = TableBorder.Rounded };
    table.AddColumn("[bold]Tag[/]");
    table.AddColumn("[bold]Count[/]");

    foreach (var stat in stats.OrderByDescending(static s => s.Value))
    {
        table.AddRow($"[cyan]{stat.Key}[/]", $"[green]{stat.Value}[/]");
    }

    AnsiConsole.Write(table);
}
else if (command == "delete")
{
    if (args.Length < 2)
    {
        AnsiConsole.MarkupLine("[red]Please provide timestamp to delete.[/]");
        return;
    }

    var timestampStr = string.Join(' ', args.Skip(1));

    if (!DateTime.TryParse(timestampStr, out var timestamp))
    {
        AnsiConsole.MarkupLine("[red]Invalid timestamp format.[/]");
        return;
    }

    var result = service.Delete(timestamp);

    if (result)
        AnsiConsole.MarkupLine("[green]Log entry deleted.[/]");
    else
        AnsiConsole.MarkupLine("[yellow]No log entry found for that timestamp.[/]");
}
else if (command == "export")
{
    var logs = service.Load();

    if (logs.Count == 0)
    {
        AnsiConsole.MarkupLine("[yellow]No log entries to export.[/]");
        return;
    }

    var exportPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".logify", "export.md");
    using var writer = new StreamWriter(exportPath);

    foreach (var log in logs.OrderBy(l => l.Timestamp))
    {
        writer.WriteLine($"## {log.Timestamp.ToLocalTime()}");
        writer.WriteLine();
        writer.WriteLine(log.Content);
        writer.WriteLine();

        if (log.Tags.Count > 0)
            writer.WriteLine($"**Tags:** {string.Join(", ", log.Tags)}");

        writer.WriteLine("\n---\n");
    }

    AnsiConsole.MarkupLine($"[green]Export completed:[/] {exportPath}");
}
else
{
    AnsiConsole.MarkupLine("[red]Error:[/] Unknown command.");
}

string RenderMarkdown(string text)
{
    // Bold **text**
    text = Regex.Replace(text, @"\*\*(.*?)\*\*", "[bold]$1[/]");

    // Italic *text*
    text = Regex.Replace(text, @"\*(.*?)\*", "[italic]$1[/]");

    return text;
}

