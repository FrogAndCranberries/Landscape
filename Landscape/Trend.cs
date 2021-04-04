using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace cAlgo
{
    /// <summary>
    /// Represents a price trend
    /// </summary>
    class Trend
    {
        //TODO: Comment and structuralize the type, organise its function against IdentifyTrends function
        public TrendType HighPriceTrendType;
        public TrendType LowPriceTrendType;

        public Peak HighStartPeak;
        public Peak LowStartPeak;
        public Peak HighEndPeak;
        public Peak LowEndPeak;

        public int LengthInBars;

        public double Height;

        public int SourcePeriod;

        public double Gradient;

        public double Intensity;

        public Trend(Peak highStartPeak, Peak lowStartPeak, Peak highEndPeak, Peak lowEndPeak, double trendHeightThreshold)
        {
            HighStartPeak = highStartPeak;
            LowStartPeak = lowStartPeak;
            HighEndPeak = highEndPeak;
            LowEndPeak = lowEndPeak;

            GetTrendType(trendHeightThreshold);
        }

        public Trend(TrendType highPriceTrendType, TrendType lowPriceTrendType,
            Peak highStartPeak, Peak lowStartPeak, Peak highEndPeak, Peak lowEndPeak,
            int sourcePeriod, double intensity = 1)
        {
            HighPriceTrendType = highPriceTrendType;
            LowPriceTrendType = lowPriceTrendType;
            HighStartPeak = highStartPeak;
            LowStartPeak = lowStartPeak;
            HighEndPeak = highEndPeak;
            LowEndPeak = lowEndPeak;
            SourcePeriod = sourcePeriod;
            Intensity = intensity;
        }

        public override string ToString()
        {
            return string.Format("HP {0}, LP {1}, start at index HP {2}, LP {3}, end at HP {4}, LP {5}",
                HighPriceTrendType, LowPriceTrendType, HighStartPeak.BarIndex, LowStartPeak.BarIndex, HighEndPeak.BarIndex, LowEndPeak.BarIndex);
        }

        private void GetTrendType(double trendHeightThreshold)
        {
            HighPriceTrendType = GetTrendTypeBetweenPeaks(HighStartPeak, HighEndPeak, trendHeightThreshold);
            LowPriceTrendType = GetTrendTypeBetweenPeaks(LowStartPeak, LowEndPeak, trendHeightThreshold);
        }

        private TrendType GetTrendTypeBetweenPeaks(Peak start, Peak end, double threshold)
        {
            if (end.Price - start.Price > threshold)
            {
                return TrendType.Uptrend;
            }
            if (end.Price - start.Price < threshold * -1)
            {
                return TrendType.Downtrend;
            }
            return TrendType.Consolidation;
        }

        public bool HasSameTrendType(Trend other)
        {
            return HighPriceTrendType == other.HighPriceTrendType && LowPriceTrendType == other.LowPriceTrendType;
        }

        //TODO: behavior related to other properties
        public void CombineWithFollowingTrend(Trend followingTrend)
        {
            bool trendFollows = (HighEndPeak == followingTrend.HighStartPeak && LowEndPeak == followingTrend.LowStartPeak) ||
                (HighEndPeak == followingTrend.HighStartPeak && LowEndPeak == followingTrend.LowEndPeak) ||
                (LowEndPeak == followingTrend.LowStartPeak && HighEndPeak == followingTrend.HighEndPeak);
            if (!trendFollows)
            {
                throw new ArgumentException(this.ToString() + followingTrend.ToString());
            }
            HighEndPeak = followingTrend.HighEndPeak;
            LowEndPeak = followingTrend.LowEndPeak;
        }
    }
}
