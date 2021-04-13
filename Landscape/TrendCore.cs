using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace cAlgo
{
    /// <summary>
    /// Represents the centre of a trend where its high and low components overlap
    /// </summary>
    class TrendCore
    {
        public DateTime StartTime { get; private set; }
        public DateTime EndTime { get; private set; }

        public int StartIndex { get; private set; }
        public int EndIndex { get; private set; }

        public TimeSpan Length { get; private set; }
        public int LengthInBars { get; private set; }

        public TrendCore(Peak highStartPeak, Peak lowStartPeak, Peak highEndPeak, Peak lowEndPeak)
        {
            ValidateInputPeaks(highStartPeak, lowStartPeak, highEndPeak, lowEndPeak);

            Peak coreStartPeak = (highStartPeak.BarIndex > lowStartPeak.BarIndex) ? highStartPeak : lowStartPeak;
            Peak coreEndPeak = (highEndPeak.BarIndex < lowEndPeak.BarIndex) ? highEndPeak : lowEndPeak;

            StartTime = coreStartPeak.DateTime;
            EndTime = coreEndPeak.DateTime;

            StartIndex = coreStartPeak.BarIndex;
            EndIndex = coreEndPeak.BarIndex;

            Length = EndTime - StartTime;
            LengthInBars = EndIndex - StartIndex;
        }

        private void ValidateInputPeaks(Peak highStartPeak, Peak lowStartPeak, Peak highEndPeak, Peak lowEndPeak)
        {
            if (highStartPeak.BarIndex >= highEndPeak.BarIndex ||
                lowStartPeak.BarIndex >= lowEndPeak.BarIndex ||
                highStartPeak.BarIndex >= lowEndPeak.BarIndex ||
                lowStartPeak.BarIndex >= highEndPeak.BarIndex)
            {
                string message = string.Format("Peaks {0}, {1}, {2}, {3} do not form a valid trend core.",
                    highStartPeak, lowStartPeak, highEndPeak, lowEndPeak);
                throw new ArgumentException(message);
            }
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
