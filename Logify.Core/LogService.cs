using System.Text.Json;

namespace Logify.Core
{
    public class LogService
    {
        private readonly string _filePath;

        public LogService()
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".logify"
            );

            if (!Directory.Exists(folder))
            {
                _ = Directory.CreateDirectory(folder);
            }

            _filePath = Path.Combine(folder, "logify.json");
        }

        public List<LogEntry> Load()
        {
            if (!File.Exists(_filePath))
            {
                return [];
            }
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<LogEntry>>(json) ?? [];
        }

        public void Add(string content, List<string> tags)
        {
            var logs = Load();
            logs.Add(new LogEntry { Content = content, Tags = tags });
            Save(logs);
        }

        public List<LogEntry> Filter(string period)
        {
            var logs = Load();
            return period switch
            {
                "today" => [.. logs.Where(static log => log.Timestamp.Date == DateTime.Today)],
                "week" =>
                [
                    .. logs.Where(static log => log.Timestamp.Date >= DateTime.Today.AddDays(-7)),
                ],
                "month" =>
                [
                    .. logs.Where(static log => log.Timestamp.Date >= DateTime.Today.AddMonths(-1)),
                ],
                _ => logs,
            };
        }

        public void Save(List<LogEntry> logs)
        {
            if (!File.Exists(_filePath))
            {
                File.Create(_filePath).Close();
            }
            {
                var json = JsonSerializer.Serialize(
                    logs,
                    new JsonSerializerOptions { WriteIndented = true }
                );
                File.WriteAllText(_filePath, json);
            }
        }

        public List<LogEntry> Search(string query)
        {
            var logs = Load();
            return
            [
                .. logs.Where(log =>
                    log.Content.Contains(query, StringComparison.InvariantCultureIgnoreCase)
                    || log.Tags.Any(tag =>
                        tag.Contains(query, StringComparison.InvariantCultureIgnoreCase)
                    )
                ),
            ];
        }

        public Dictionary<string, int> StatsByTag()
        {
            var logs = Load();
            Dictionary<string, int> tagCounts = [];

            foreach (LogEntry log in logs)
            {
                foreach (string tag in log.Tags)
                {
                    tagCounts[tag] = tagCounts.TryGetValue(tag, out int value) ? ++value : 1;
                }
            }

            return tagCounts;
        }

        public bool Delete(DateTime timestamp)
        {
            var logs = Load();
            var toRemove = logs.FirstOrDefault(l => l.Timestamp == timestamp);

            if (toRemove == null)
                return false;

            logs.Remove(toRemove);
            Save(logs);
            return true;
        }
    }
}
