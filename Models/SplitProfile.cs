using System;
using System.Collections.Generic;

namespace XIVSplits.Models
{
    public class SplitProfile
    {
        public List<Split> Template { get; set; } = new();
        public Dictionary<DateTime, List<Split>> History { get; set; } = new();
    }
}
