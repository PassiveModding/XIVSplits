using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XIVSplits
{
    public static class Extensions
    {
        public static string FormatTimeHHMMSS(this TimeSpan time, bool includeFractionalSeconds = true)
        {
            if (includeFractionalSeconds)
            {
                if (time.Hours > 0)
                {
                    return time.ToString(@"hh\hmm\mss\.ff\s");
                }
                else
                {
                    return time.ToString(@"mm\mss\.ff\s");
                }
            }
            else
            {
                if (time.Hours > 0)
                {
                    return time.ToString(@"hh\hmm\mss\s");
                }
                else
                {
                    return time.ToString(@"mm\mss\s");
                }
            }
        }


        public static string FormatTime(this TimeSpan time, bool includeFractionalSeconds = true)
        {
            if (includeFractionalSeconds)
            {
                if (time.Hours > 0)
                {
                    return time.ToString(@"hh\:mm\:ss\.ff");
                }
                else
                {
                    return time.ToString(@"mm\:ss\.ff");
                }
            }
            else
            {
                if (time.Hours > 0)
                {
                    return time.ToString(@"hh\:mm\:ss");
                }
                else
                {
                    return time.ToString(@"mm\:ss");
                }
            }
        }
    }
}
