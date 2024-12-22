//#define GenerateClass
using Microsoft.Data.Sqlite;
using System.Data;
using MapDataReader;

namespace NowShowing
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var sqlFile = @"C:\Users\Public\NPVR-data\npvr.db3";
            if (args.Length > 1)
                if (File.Exists(args[0]))
                    sqlFile = args[0];

            string query = File.ReadAllText("Query.sql");

            // if?
            var excludeList = File.ReadAllLines("Exclude.txt");
            var exclude = excludeList
                .Where(s => !String.IsNullOrEmpty(s))
                .Where(s => !s.StartsWith("#"))
                .Distinct()
                .ToHashSet();

#if GenerateClass
            CreateClass("Event", sqlFile, query);
            return;
#endif
            CheckSqliteFile(sqlFile, query, exclude);

            return;
        }

        private static void CheckSqliteFile(string sqlFile, string query, HashSet<string> exclude)
        {
            bool exclusive = true;
            List<Event> results;

            var connectionString = new SqliteConnectionStringBuilder()
            {
                Mode = SqliteOpenMode.ReadOnly,
                DataSource = sqlFile
            }.ToString();

            using (var connection = new SqliteConnection(connectionString))
            {
                var command = connection.CreateCommand();
                command.CommandText = query;

                // command.Parameters.AddWithValue("$id", id);

                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    results = reader.ToEvent(); // "ToMyClass" method is generated at compile time
                }
            }

            foreach (var @event in results)
            {
                if (!string.IsNullOrEmpty(@event.Title))
                {
                    if (exclude.Contains(@event.Title))
                        continue;

                    string identifier = $"{@event.ChannelName} - {@event.Title}";
                    if (exclusive && exclude.Contains(identifier))
                        continue;

                    exclude.Add(identifier);

                    WriteEventLine(@event, identifier);
                }
            }
        }

        private static void WriteEventLine(Event @event, string identifier)
        {
            string startTime = "";
            if (DateTime.TryParse(@event.StartTime, out DateTime start))
                startTime = $"{start.DayOfWeek} {start.TimeOfDay}";

            ConsoleColor oldBackgroundColor = Console.BackgroundColor;
            ConsoleColor oldForegroundColor = Console.ForegroundColor;

            bool isMovie = @event.Genres?.Contains("Movie") ?? false;
            if (isMovie)
            {
                Console.BackgroundColor = ConsoleColor.Blue;
            }

            bool isAnimated = @event.Genres?.Contains("Animated") ?? false;
            if (isAnimated)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
            }

            Console.WriteLine($"{startTime} {identifier} ({@event.Genres})");

            Console.BackgroundColor = oldBackgroundColor;
            Console.ForegroundColor = oldForegroundColor;
        }

#if GenerateClass
        private static void CreateClass(string className, string sqlFile, string query)
        {
            using (var connection = new SqliteConnection($"Data Source={sqlFile}"))
            {
                var command = connection.CreateCommand();
                command.CommandText = query;

                // command.Parameters.AddWithValue("$id", id);

                connection.Open();
                using (var reader = command.ExecuteReader(CommandBehavior.SchemaOnly))
                {
                    if (reader.Read())
                    {
                        string reserved = "System.";

                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            var name = reader.GetName(i);
                            var type = reader.GetFieldType(i).FullName;

                            if (type.StartsWith(reserved, StringComparison.OrdinalIgnoreCase))
                                type = type.Substring(reserved.Length);

                            Console.WriteLine($"public {type}? {name} {{ get; set; }}");
                        }
                    }
                }
            }
        }
#endif
    }
}
