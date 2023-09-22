using System;
using System.Diagnostics;
using System.Linq;
using XIVSplits.Config;
using XIVSplits.Models;

namespace XIVSplits.Timers
{
    public class InternalTimer : IDisposable
    {
        public InternalTimer(ConfigService configService)
        {
            ConfigService = configService;
        }

        public bool IsRunning { get; private set; }
        public Stopwatch RealTime { get; private set; } = new Stopwatch();
        public Stopwatch SegmentTime { get; private set; } = new Stopwatch();
        public int CurrentSplitIndex { get; private set; }
        public ConfigService ConfigService { get; }

        // 1. Start - start timer, start segment, set index to 0, set running to true
        // 2. Split - stop segment, set split time, set index to index + 1, start segment - only triggered by chat message
        // 3. Stop - stop timer, stop segment, set running to false, reset index to 0
        // 4. ManualSplit - stop segment, set split time, set index to index + 1, start segment - expand split array if needed
        public void Start(string? objective = null)
        {
            if (!IsRunning)
            {
                Config.Config config = ConfigService.Get();
                var currentProfile = config.GetCurrentProfile();
                // remove all tracked names and times
                foreach (Split split in currentProfile.Template)
                {
                    split.Objective = "";
                    split.SegmentParsed = TimeSpan.Zero;
                    split.Segment = TimeSpan.Zero;
                    split.SplitTime = TimeSpan.Zero;
                    split.Total = TimeSpan.Zero;
                }

                Split initSplit = currentProfile.Template[0];
                if (string.IsNullOrWhiteSpace(initSplit.Objective) && objective != null)
                {
                    initSplit.Objective = objective;
                }

                IsRunning = true;
                RealTime.Reset();
                SegmentTime.Reset();
                RealTime.Start();
                SegmentTime.Start();
                CurrentSplitIndex = 0;
            }
            else
            {
                SegmentTime.Start();
            }
        }

        public void Split(TimeSpan time, string? objective = null)
        {
            if (!IsRunning) return;
            SegmentTime.Stop();

            SetCurrentSplitData(time, objective);
            SegmentTime.Reset();
            CurrentSplitIndex++;

            var currentProfile = ConfigService.Get().GetCurrentProfile();
            if (CurrentSplitIndex >= currentProfile.Template.Count)
            {
                Stop();
            }

            // Not doing to resume segment time here as it is supposed to be a measure of the amount of time in combat.
            // The split time (RealTime.Elapsed - GetPrevSplit().Total) is the time between the last split and the current split.
        }

        private Split GetPrevSplit()
        {
            var currentProfile = ConfigService.Get().GetCurrentProfile();
            Split prevSplit;
            if (CurrentSplitIndex == 0)
            {
                prevSplit = new Split();
            }
            else
            {
                prevSplit = currentProfile.Template[CurrentSplitIndex - 1];
            }

            return prevSplit;
        }

        private void SetCurrentSplitData(TimeSpan parsedTime, string? objective = null)
        {
            var currentProfile = ConfigService.Get().GetCurrentProfile();
            Split currentSplit = currentProfile.Template[CurrentSplitIndex];
            currentSplit.Objective = objective ?? "Unknown";
            currentSplit.SegmentParsed = parsedTime;
            currentSplit.Segment = SegmentTime.Elapsed;
            currentSplit.SplitTime = RealTime.Elapsed - GetPrevSplit().Total;
            currentSplit.Total = RealTime.Elapsed;
        }

        public void ManualSplit(string? objective = null)
        {
            if (!IsRunning) return;
            SegmentTime.Stop();

            // Set parsed time to 0 since there is no chat message to read from
            // Potentially could read the duty time?
            SetCurrentSplitData(TimeSpan.Zero, objective);
            SegmentTime.Reset();

            // Resume segment time as we cannot know if the player is in combat or not
            SegmentTime.Start();
            CurrentSplitIndex++;

            // if currentsplitindex is greater than splitsarray length, expand splitsarray
            var currentProfile = ConfigService.Get().GetCurrentProfile();
            if (CurrentSplitIndex >= currentProfile.Template.Count)
            {
                var newSplit = new Split();
                currentProfile.Template.Add(newSplit);
            }
        }

        public void Stop()
        {
            IsRunning = false;
            RealTime.Stop();
            SegmentTime.Stop();

            Config.Config config = ConfigService.Get();
            // copy splits to history

            // shallow copy splits
            var currentProfile = config.GetCurrentProfile();

            foreach (var split in currentProfile.Template)
            {
                // update best segment if we have a non-zero segment
                if (split.SegmentParsed != TimeSpan.Zero && (split.SegmentParsed < split.BestSegmentParsed || split.BestSegmentParsed == TimeSpan.Zero))
                {
                    split.BestSegmentParsed = split.SegmentParsed;
                }

                if (split.Segment != TimeSpan.Zero && (split.Segment < split.BestSegment || split.BestSegment == TimeSpan.Zero))
                {
                    split.BestSegment = split.Segment;
                }
            }


            var splits = currentProfile.Template.Select(split => split.CloneSplit()).ToList();
            currentProfile.History[DateTime.Now] = splits;
            ConfigService.Save();
        }

        public void Dispose()
        {
        }

        internal void ResetSplits()
        {
            Config.Config config = ConfigService.Get();
            var currentProfile = config.GetCurrentProfile();
            // remove all tracked names and times
            foreach (Split split in currentProfile.Template)
            {
                split.Objective = "";
                split.SegmentParsed = TimeSpan.Zero;
                split.Segment = TimeSpan.Zero;
                split.SplitTime = TimeSpan.Zero;
                split.Total = TimeSpan.Zero;
            }

            ConfigService.Save();
        }
    }
}