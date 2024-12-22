using MapDataReader;

namespace NowShowing
{
    [GenerateDataReaderMapper] // package MapDataReader
    public class Event
    {
        public String? Title { get; set; }
        public String? Genres { get; set; }
        public String? ChannelName { get; set; }
        public Int64?  ChannelMajor { get; set; }
        public Int64?  ChannelMinor { get; set; }
        public String? StartTime { get; set; }
        public Double? Duration { get; set; }
    }
}
