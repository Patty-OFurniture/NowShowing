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
            // args = new[] { "-export" };

            if (args.Length == 1 && args[0] == "-export")
                Exporter.Export(args);
			else
				Main_(args);
        }

        static void Main_(string[] args)
        {
            bool createJson = false;
            var sqlFile = @"C:\Users\Public\NPVR-data\npvr.db3";
            if (args.Length == 1)
            {
                if (File.Exists(args[0]))
                    sqlFile = args[0];
            }
            else if (args.Length == 2)
            {
                createJson = true;
            }

            string query = File.ReadAllText("Query.sql");

            var excludeList = File.ReadAllLines("Exclude.txt");

            if (createJson)
            {
                new ExclusionList().Create(excludeList);
                return;
            }

            // HashSet<string> excludeTitles, excludeGenres, excludeMovies;
            
            GenerateLists(excludeList, out var excludeTitles, out var excludeGenres, out var excludeMovies, out var highlightCast);

#if GenerateClass
            CreateClass("Event", sqlFile, query);
            return;
#endif
            var events = GetEvents(sqlFile, query);

            EventOutput.WriteEvents(events, excludeTitles, excludeGenres, excludeMovies, highlightCast);

            return;
        }

        public static void GenerateLists(string[] excludeList, out HashSet<string> excludeTitles, out HashSet<string> excludeGenres, out HashSet<string> excludeMovies, out HashSet<string> highlightCast)
        {
            excludeList = [.. excludeList.Distinct()];

            excludeTitles = excludeList
                .Where(s => !String.IsNullOrEmpty(s))
                .Where(s => !s.StartsWith("#"))
                .Distinct()
                .ToHashSet();
            excludeGenres = excludeList
                .Where(s => s.StartsWith("#G:"))
                .Select(s => EventOutput.FixGenres(s.Substring(3)))
                .Distinct()
                .ToHashSet();
            excludeMovies = excludeList
                .Where(s => s.StartsWith("#M:"))
                .Distinct()
                .Select(s => s.Substring(3))
                .ToHashSet();
            highlightCast = excludeList
                .Where(s => s.StartsWith("#C:"))
                .Distinct()
                .Select(s => s.Substring(3))
                .ToHashSet();
        }

        private static List<Event> GetEvents(string sqlFile, string query)
        {
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
                    // "ToMyClass" method is generated at compile time
                    results = reader.ToEvent();
                }

                command.Dispose();

                connection.Close();
                connection.Dispose();

                /*
                This is by design - connection pooling was implemented in version 
                6.0 of the Sqlite provider, which keeps connections open so they 
                can be recycled for better performance (see docs). You can use the 
                SqliteConnection.ClearAllPools() API to force all idle pooled 
                connections to be closed at a specific moment, or disable pooling 
                altogether by adding Pooling=false to your connection string (and 
                forgo the perf improvements).
                */
                SqliteConnection.ClearAllPools();
            }

            return results;
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
