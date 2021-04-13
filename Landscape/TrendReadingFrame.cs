using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo
{
    /// <summary>
    /// Represents the reading frame on two lists of peaks during trend segment identification
    /// </summary>
    class TrendReadingFrame
    {
        private Peak HighStartPeak { get; set; }
        private Peak LowStartPeak { get; set; }
        private Peak HighEndPeak { get; set; }
        private Peak LowEndPeak { get; set; }

        private double TrendTypeThreshold { get; set; }

        /// <summary>
        /// Sets three out of the four peaks, the fourth will be pushed in at the first advance
        /// </summary>
        /// <param name="algo"></param>
        /// <param name="highStartPeak"></param>
        /// <param name="highEndPeak"></param>
        /// <param name="lowEndPeak"></param>
        /// <param name="trendTypeThreshold"></param>
        public TrendReadingFrame(Peak highEndPeak, Peak lowEndPeak, double trendTypeThreshold)
        {
            ValidateInputPeaks(highEndPeak, lowEndPeak);

            HighEndPeak = highEndPeak;
            LowEndPeak = lowEndPeak;

            TrendTypeThreshold = trendTypeThreshold;
        }

        private void ValidateInputPeaks(Peak highEndPeak, Peak lowEndPeak)
        {
            if (highEndPeak.BarIndex != lowEndPeak.BarIndex)
            {
                string message = string.Format("Peaks {0}, {1} do not form a valid initial reading frame.",
                    highEndPeak, lowEndPeak);
                throw new ArgumentException(message);
            }
        }

        /// <summary>
        /// Determines if the reading frame should now advance the high price half
        /// </summary>
        /// <returns></returns>
        public bool ShouldAdvanceHigh()
        {
            return HighEndPeak.BarIndex < LowEndPeak.BarIndex;
        }

        /// <summary>
        /// Moves the high half of the reading frame one peak forward and returns the new trend in the reading frame
        /// </summary>
        /// <param name="newHighPeak"></param>
        /// <returns></returns>
        public void AdvanceHigh(Peak newHighPeak)
        {
            HighStartPeak = HighEndPeak;
            HighEndPeak = newHighPeak;
        }

        /// <summary>
        /// Determines if the reading frame should now advance the low price half
        /// </summary>
        /// <returns></returns>
        public bool ShouldAdvanceLow()
        {
            return HighEndPeak.BarIndex > LowEndPeak.BarIndex;
        }

        /// <summary>
        /// Moves the low half of the reading frame one peak forward and returns the new trend in the reading frame
        /// </summary>
        /// <param name="newLowPeak"></param>
        /// <returns></returns>
        public void AdvanceLow(Peak newLowPeak)
        {
            LowStartPeak = LowEndPeak;
            LowEndPeak = newLowPeak;
        }

        /// <summary>
        /// Returns the trend currently in the reading frame
        /// </summary>
        /// <returns></returns>
        public Trend GetTrend()
        {
            return new Trend(HighStartPeak, LowStartPeak, HighEndPeak, LowEndPeak, TrendTypeThreshold);
        }
    }
}
