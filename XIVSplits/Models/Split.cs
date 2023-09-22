using System;
namespace XIVSplits.Models
{
    public class Split : ICloneable
    {
        public string Name { get; set; } = "";
        public string Objective { get; set; } = "";

        // Segment is the time since combat started to the split
        public TimeSpan Segment { get; set; } = TimeSpan.Zero;
        public TimeSpan SegmentParsed { get; set; } = TimeSpan.Zero;

        // these will only be populated on the template
        public TimeSpan BestSegment { get; set; } = TimeSpan.Zero;
        public TimeSpan BestSegmentParsed { get; set; } = TimeSpan.Zero;

        // Split is the time since the last split
        public TimeSpan SplitTime { get; set; } = TimeSpan.Zero;

        // Total is the time since the run started, could also be represented as the sum of all splits including the current split
        public TimeSpan Total { get; set; } = TimeSpan.Zero;

        public object Clone()
        {
            // memberwise clone
            return this.MemberwiseClone();
        }

        public Split CloneSplit()
        {
            return (Split)this.Clone();
        }
    }
}
