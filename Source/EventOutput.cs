using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Linq;
using System.Text;

namespace NowShowing
{
    internal class EventOutput
    {
        const char genreSeparator = ';';

        public static string[] GenreList = Array.Empty<string>();

        public static string FixGenres(string? v)
        {
            if (v == null)
                return "";

            if (v.Contains(genreSeparator))
            {
                GenreList = v
                    .Split(genreSeparator)
                    .Order()
                    .ToArray();

                string temp = String.Join(genreSeparator, GenreList);
                if (v != temp)
                    v = temp;
            }
            return v;
        }

        public static void WriteEvents(List<Event> events, HashSet<string> excludeTitles, HashSet<string> excludeGenres, HashSet<string> excludeMovies, HashSet<string> highlightCast)
        {
            // consider the same show on different channels as different
            bool exclusive = true;
            Dictionary<string, int> Counter = new();

            foreach (var @event in events)
            {
                // differentiate between movie titles and series titles
                bool isMovie = false;
                // mark this as evaluated, so displayed only once
                string identifier = $"{@event.ChannelName} - {@event.Title}";
                if (exclusive && excludeTitles.Contains(identifier))
                    continue;

                if (Counter.ContainsKey(identifier))
                    Counter[identifier]++;
                else
                    Counter.Add(identifier, 1);

                // show only the first instance
                excludeTitles.Add(identifier);

                DateTime.TryParse(@event.OriginalAirDate, out DateTime originalAirDate);
                if (originalAirDate != DateTime.MinValue)
                    @event.OriginalAirDate = originalAirDate.Year.ToString();

                // avoid a title search if posible
                if (!string.IsNullOrEmpty(@event.Genres))
                {
                    if (excludeGenres.Contains(FixGenres(@event.Genres)))
                        continue;

                    if (!string.IsNullOrEmpty(@event.Title))
                    {
                        var c = ';';
                        var g = @event.Genres.Split(c);

                        if (g.Contains("Movie"))
                        {
                            // optionally make release years important
                            // all movies should be marked with a year
                            if (excludeMovies.Contains(($"{@event.Title} ({@event.OriginalAirDate})")))
                                continue;
                            else if (excludeMovies.Contains(@event.Title))
                                continue;
                            else
                                isMovie = true;
                        }
                    }
                }

                if (!isMovie)
                {
                    if (string.IsNullOrEmpty(@event.Title))
                        continue;
                    if (excludeTitles.Contains(@event.Title))
                        continue;
                }

                WriteEventLine(@event, identifier, highlightCast);
            }

            foreach(var c in Counter)
            {
                if (c.Value >= 5)
                    Console.WriteLine($"Marathon: {c.Key} {c.Value}");
            }
        }

        private static void WriteEventLine(Event @event, string identifier, HashSet<string> highlightCast)
        {
            string startTime = "";
            if (DateTime.TryParse(@event.StartTime, out DateTime start))
                startTime = $"{start:ddd} {start:HH:mm}";

            ConsoleColor oldBackgroundColor = Console.BackgroundColor;
            ConsoleColor oldForegroundColor = Console.ForegroundColor;

            if (@event.Genres?.Contains("Animated") ?? false)
                Console.ForegroundColor = ConsoleColor.Yellow;

            bool isMovie = @event.Genres?.Contains("Movie") ?? false;
            bool isHighlightCast = HighlightCast(@event.Cast, highlightCast);

            if (isMovie)
                OutputMovie(@event, identifier, startTime, oldBackgroundColor, isHighlightCast);
            else
                OutputEpisode(@event, identifier, startTime, oldBackgroundColor, isHighlightCast);

            Console.ForegroundColor = oldForegroundColor;
        }

        private static bool HighlightCast(string? cast, HashSet<string> highlightCast)
        {
            if (!string.IsNullOrEmpty(cast))
            {
                var tempCast = cast.Split(';');

                if (tempCast.Any(c => highlightCast.Contains(c)))
                    return true;
            }
            return false;
        }

        private static void OutputEpisode(Event @event, string identifier, string startTime, ConsoleColor oldBackgroundColor, bool isHighlightCast)
        {
            string episode = "";
            if (@event.Season > 0 && @event.Episode > 0)
                episode = $"S{@event.Season:00}E{@event.Episode:00}";

            if (isHighlightCast)
                Console.ForegroundColor = ConsoleColor.Green;

            Console.Write($"{startTime} {@event.Duration:0.0} {identifier} {episode} ({@event.Genres})");
            Console.BackgroundColor = oldBackgroundColor;
            Console.WriteLine();
            if (!string.IsNullOrEmpty(@event.Subtitle))
                Console.WriteLine($"\t{@event.Subtitle}");
            if (!string.IsNullOrEmpty(@event.Description))
                Console.WriteLine($"\t{@event.Description}");
            FixCast(@event);
            if (!string.IsNullOrEmpty(@event.Cast))
                Console.WriteLine($"\t{@event.Cast}");
        }

        private static void OutputMovie(Event @event, string identifier, string startTime, ConsoleColor oldBackgroundColor, bool isHighlightCast)
        {
            Console.BackgroundColor = ConsoleColor.Blue;
            if (isHighlightCast)
                Console.ForegroundColor = ConsoleColor.Green;

            FixCast(@event);

            if (!string.IsNullOrEmpty(@event.Rating))
            {
                if (decimal.TryParse(@event.Rating, out decimal rating))
                {
                    var sb = new StringBuilder();
                    int ratingInt = (int)decimal.Floor(rating);
                    sb.Append("[");
                    sb.Append(new string('*', ratingInt)); // ★
                    if (ratingInt != rating)
                    {
                        sb.Append("½");
                        rating++;
                    }

                    sb.Append(new string(' ', 6 - sb.Length)); // ☆ +1 because of leading '['
                    sb.Append("]");
                    @event.Rating = sb.ToString();
                }
            }

            Console.Write($"{startTime} {@event.Duration:0.0} {identifier} ({@event.OriginalAirDate}) {@event.Rating} ({@event.Genres})");
            Console.BackgroundColor = oldBackgroundColor;
            Console.WriteLine();
            if (!string.IsNullOrEmpty(@event.Subtitle))
                Console.WriteLine($"\t{@event.Subtitle}");
            if (!string.IsNullOrEmpty(@event.Cast))
                Console.WriteLine($"\t{@event.Cast}");
        }

        private static void FixCast(Event @event)
        {
            if (string.IsNullOrEmpty(@event.Cast))
                return;

            Dictionary<string, List<string>> Types = new();

            var tempCast = @event.Cast
                .Split(';')
                .Select(c =>
                    c.StartsWith("Actor:") ? c.Substring(6) :
                    c.StartsWith("Voice:") ? "V:" + c.Substring(6) :
                    c);

            foreach(var member in tempCast)
            {
                var t = member.Split(':');
                if (t.Length != 2
                    || string.IsNullOrEmpty(t[0])
                    || string.IsNullOrEmpty(t[1])
                    )
                    t = new string[] { "", member };

                if (Types.ContainsKey(t[0]))
                    Types[t[0]].Add(t[1]);
                else
                    Types.Add(t[0], new List<string> { t[1] });
            }

            List<string> finalCast = new List<string>();
            foreach (var member in Types.Keys)
            {
                StringBuilder stringBuilder = new();
                if (!string.IsNullOrEmpty(member))
                    stringBuilder.Append(member + ": ");

                stringBuilder.Append(string.Join(", ", Types[member]));
                finalCast.Add(stringBuilder.ToString());
            }

            @event.Cast = String.Join(", ", finalCast);
        }
    }
}
