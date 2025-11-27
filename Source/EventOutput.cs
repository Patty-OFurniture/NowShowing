using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NowShowing
{
    internal class EventOutput
    {
        public static string FixGenres(string? v)
        {
            const char c = ';';

            if (v == null)
                return "";

            if (v.Contains(c))
            {
                string temp = String.Join(c, v.Split(c).Order());
                if (v != temp)
                    v = temp;
            }
            return v;
        }

        public static void WriteEvents(List<Event> events, HashSet<string> excludeTitles, HashSet<string> excludeGenres, HashSet<string> excludeMovies)
        {
            // consider the same show on different channels as different
            bool exclusive = true;

            foreach (var @event in events)
            {
                // differentiate between movie titles and series titles
                bool isMovie = false;
                // mark this as evaluated, so displayed only once
                string identifier = $"{@event.ChannelName} - {@event.Title}";
                if (exclusive && excludeTitles.Contains(identifier))
                    continue;

                excludeTitles.Add(identifier);

                if (!string.IsNullOrEmpty(@event.Genres))
                {
                    // avoid a title search if posible
                    if (excludeGenres.Contains(FixGenres(@event.Genres)))
                        continue;

                    if (!string.IsNullOrEmpty(@event.Title))
                    {
                        var c = ';';
                        var g = @event.Genres.Split(c);

                        if (g.Contains("Movie"))
                        {
                            if (excludeMovies.Contains(@event.Title))
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

                WriteEventLine(@event, identifier);
            }
        }

        private static void WriteEventLine(Event @event, string identifier)
        {
            string startTime = "";
            if (DateTime.TryParse(@event.StartTime, out DateTime start))
                startTime = $"{start.DayOfWeek} {start.TimeOfDay}";

            ConsoleColor oldBackgroundColor = Console.BackgroundColor;
            ConsoleColor oldForegroundColor = Console.ForegroundColor;

            bool isAnimated = @event.Genres?.Contains("Animated") ?? false;
            if (isAnimated)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
            }

            bool isMovie = @event.Genres?.Contains("Movie") ?? false;
            if (isMovie)
            {
                Console.BackgroundColor = ConsoleColor.Blue;

                if (!string.IsNullOrEmpty(@event.Cast))
                {
                    var tempCast = @event.Cast
                        .Split(';')
                        .Select(c =>
                            c.StartsWith("Actor:") ? c.Substring(6) :
                            c.StartsWith("Voice:") ? "V:" + c.Substring(6) :
                            c);
                    @event.Cast = String.Join(", ", tempCast);
                }

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

                DateTime.TryParse(@event.OriginalAirDate, out DateTime originalAirDate);
                if (originalAirDate != DateTime.MinValue)
                {
                    @event.OriginalAirDate = originalAirDate.Year.ToString();
                }

                Console.WriteLine($"{startTime} {identifier} ({@event.OriginalAirDate}) {@event.Rating} ({@event.Genres})");
                Console.BackgroundColor = oldBackgroundColor;
                Console.WriteLine($"\t{@event.Cast}");
            }
            else
            {
                string episode = "";
                if (@event.Season > 0 &&  @event.Episode > 0)
                    episode = $"S{@event.Season:00} E{@event.Episode:00}";

                Console.WriteLine($"{startTime} {identifier} {episode} ({@event.Genres})");
                Console.BackgroundColor = oldBackgroundColor;
                Console.WriteLine($"\t{@event.Description}");
            }

            Console.ForegroundColor = oldForegroundColor;
        }
    }
}
