using System;
using System.Collections.Generic;

namespace XIVSplits.Models
{
    public class SplitCollection
    {
        public DateTime DateTime { get; set; }
        public List<Split> Splits { get; set; } = new();
    }
}
