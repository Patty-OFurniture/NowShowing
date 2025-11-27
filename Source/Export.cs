using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using System.Data;

namespace NowShowing
{
    internal class Exporter
    {
        private static string folder = @"D:\Downloads\0 0\data - npvr";

        public static void Export(string[] args)
        {
            if (args.Length > 1 && File.Exists(args[0]))
            {
                ExportInternal(args[0]);
            }
            else
            {
                foreach (var filename in Directory.GetFiles(folder, "npvr_*.db3"))
                {
                    ExportInternal(filename);
                }
            }
        }

        public static void ExportInternal(string sqlFile)
        {
            List<string> tables = new List<string>();

            var connectionString = new SqliteConnectionStringBuilder()
            {
                Mode = SqliteOpenMode.ReadOnly,
                DataSource = sqlFile
            }.ToString();

            Console.WriteLine(sqlFile);

            string query = "select name\r\nfrom sqlite_schema\r\nwhere Type = 'table'and name not in ('sqlite_sequence')";
            using (var connection = new SqliteConnection(connectionString))
            {
                var command = connection.CreateCommand();
                command.CommandText = query;

                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        tables.Add(reader.GetString(0));
                    }
                }
            }

            Dictionary<string, List<string>> inserts = new();

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                foreach (var tableName in tables)
                {
                    List<string> columns = new();

                    string prefix, columnNames;

                    columnNames = GetColumnNames(connection, tableName, columns, out prefix);

                    inserts.Add(tableName, GetInserts(connection, tableName, columns, prefix, columnNames));
                }

            }

            foreach(var key in inserts.Keys)
            {
                var insert = inserts[key];

                // exec
                using var sqlConnection = new SqlConnection
                {
                    ConnectionString = @"data source=MSI\SQL2019;Integrated Security=SSPI;Initial Catalog=NPVR_TEMP;TrustServerCertificate=true"
                };

                sqlConnection.Open();
                var cmd = sqlConnection.CreateCommand();

                Console.WriteLine("----------" + key);
                int inserted = 0;
                foreach (var i in insert)
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = i;
                    try
                    {
                        cmd.ExecuteNonQuery();
                        inserted++;
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine(e);
                        Console.WriteLine(i);
                    }
                }
                Console.WriteLine($"{inserted} / {insert.Count}");
                Console.WriteLine("----------");
                Console.WriteLine();
            }
            return;
        }

        private static List<string> GetInserts(SqliteConnection connection, string tableName, List<string> columns, string prefix, string columnNames)
        {
            List<string> inserts = new();

            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = $"select * from {tableName}";
                using (var reader = command.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            List<string> values = new();

                            foreach (var column in columns)
                            {
                                //if (column == "first_run")
                                //    System.Diagnostics.Debugger.Break();

                                var tmp = reader[column]?.ToString() ?? "null";

                                if (tmp.StartsWith("202") && 
                                    (tmp.Length <= 27 && tmp.Length >= 25)
                                    && tmp.Contains("."))
                                    tmp = tmp.Substring(0, 19);

                                if ("0001-01-01 00:00:00" == tmp)
                                    values.Add("null");
                                else
                                    values.Add($"'{tmp.Replace("'", "''")}'");
                            }

                            string insert =
                                prefix +
                                columnNames +
                                " ) values ( " +
                                string.Join(",", values) +
                                " );";

                            // Console.WriteLine(insert);
                            inserts.Add(insert);
                        }
                    }
                }
            }

            return inserts;
        }

        private static string GetColumnNames(SqliteConnection connection, string tableName, List<string> columns, out string prefix)
        {
            string columnNames = "";

            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = $"select * from {tableName}";
                prefix = $"insert into {tableName} ( ";
                using (var reader = command.ExecuteReader(CommandBehavior.SchemaOnly))
                {
                    var schema = reader.GetSchemaTable();
                    foreach (DataRow col in schema.Rows)
                    {
                        columns.Add(col.Field<String>("ColumnName") ?? "");
                    }

                    columnNames = "[" + String.Join("], [", columns) + "]";
                }
            }
            return columnNames;
        }
    }
}
