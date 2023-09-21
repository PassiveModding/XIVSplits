namespace XIVSplits.Models
{
    public class Command
    {
        public Command(string trigger, string value)
        {
            Trigger = trigger;
            Value = value;
        }

        public string Trigger { get; set; }
        public string Value { get; set; }
        public bool SendToLiveSplit { get; set; }
        public bool SendToInternalTimer { get; set; }
        public bool Enabled { get; set; } = true;
    }
}
