using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace cAlgo
{
    class TrendCore
    {
        public DateTime StartTime;
        public DateTime EndTime;

        public int StartIndex;
        public int EndIndex;

        public TimeSpan Length;
        public int LengthInBars;

        public TrendCore(Peak highStartPeak, Peak lowStartPeak, Peak highEndPeak, Peak lowEndPeak)
        {
            //TODO: Test this is a proper trend with a core

            Peak coreStartPeak = highStartPeak.BarIndex > lowStartPeak.BarIndex ? highStartPeak : lowStartPeak;
            Peak coreEndPeak = highEndPeak.BarIndex < lowEndPeak.BarIndex ? highEndPeak : lowEndPeak;

            StartTime = coreStartPeak.DateTime;
            EndTime = coreEndPeak.DateTime;

            StartIndex = coreStartPeak.BarIndex;
            EndIndex = coreEndPeak.BarIndex;

            Length = EndTime - StartTime;
            LengthInBars = EndIndex - StartIndex;
        }

        public bool SpansWeekend()
        {
            if (Length >= TimeSpan.FromDays(6) || EndTime.DayOfWeek < StartTime.DayOfWeek)
            {
                return true;
            }

            return false;
        }
    }
}
