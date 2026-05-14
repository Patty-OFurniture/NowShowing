using System.Text.Json;

namespace NowShowing
{
    internal class ExclusionList
    {
        // Tuple replacement
        public class Stuple
        {
            public string Classification {  get; set; }
            public List<string> Names { get; set; }

            // for deserialization
            public Stuple() {
                Classification = "";
                Names = new List<string>();
            }

            public Stuple(string classification, List<string> names)
            {
                this.Classification = classification;
                this.Names = names;
            }
        }

        public List<string> Genres { get; set; }
        public List<string> Movies { get; set; }
        public List<Stuple> Shows { get; set; }
        public List<string> Cast { get; set; }

        public ExclusionList() 
        {
            Genres = new();
            Movies = new();
            Shows = new();
            Cast = new();
        }

        public bool Create(string[] excludeList)
        {
            // excludeList is not needed, but code reuse is
            Program.GenerateLists(excludeList, out var excludeTitles, out var excludeGenres, out var excludeMovies, out var highlightCast);
            Genres = excludeGenres.ToList();
            Movies = excludeMovies.ToList();
            Cast = highlightCast.ToList();

            Stuple lastTuple = new Stuple();

            foreach (var line in excludeList)
            {
                if (string.IsNullOrWhiteSpace(line)) 
                    continue;
                else if (line.StartsWith("# === "))
                {
                    string genre = line.Substring(6);
                    var tuple = new Stuple(genre, new List<string>());
                    Shows.Add(tuple);
                    lastTuple = tuple;
                }
                else if(line.StartsWith("#"))
                        continue;
                else { 
                    lastTuple.Names.Add(line);
                }
            }

            bool success = false;

            try
            {
                var json = JsonSerializer.Serialize(this);
                if (json != null && json != "{}")
                {  
                    success = true;
                    Console.WriteLine(json);
                }
            }
            catch (Exception)
            {
            }

            return success;
        }
    }
}
