using System.Collections.Generic;

namespace XIVSplits.Models
{
    public class Profile
    {
        public bool Enabled { get; set; } = true;
        public List<Command> Commands { get; set; } = new List<Command>();
    }
}
