namespace XIVSplits.Models
{
    public class Objective
    {
        public string CompleteObjective { get; set; } = "";
        //public bool ParseFromChat { get; set; } = false;
        //public bool ParseFromDutyInfo { get; set; } = false;
        public GoalType GoalType { get; set; } = GoalType.Chat;
        public bool TriggerSplit { get; set; } = false;
    }
}
