using System;
namespace XIVSplits.Models
{
    // Segment = time in combat (or otherwise if combat unknown, time since last split)
    // Split = time since last split
    // Total = time since run started
    public class Split : ICloneable
    {
        public string Name { get; set; } = "";
        public string Objective { get; set; } = "";

        // Segment is the time since combat started to the split
        public TimeSpan Segment { get; set; } = TimeSpan.Zero;
        public TimeSpan SegmentParsed { get; set; } = TimeSpan.Zero;

        // Split is the time since the last split
        public TimeSpan SplitTime { get; set; } = TimeSpan.Zero;

        // Total is the time since the run started, could also be represented as the sum of all splits including the current split
        public TimeSpan Total { get; set; } = TimeSpan.Zero;

        public TimeSpan BestSegment { get; set; } = TimeSpan.Zero;
        public TimeSpan BestSegmentParsed { get; set; } = TimeSpan.Zero;
        public TimeSpan BestSplit { get; set; } = TimeSpan.Zero;

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
