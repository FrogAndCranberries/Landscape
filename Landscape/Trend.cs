using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;
using MathNet.Numerics;

namespace cAlgo
{
    /// <summary>
    /// Represents a price trend
    /// </summary>
    class Trend
    {
        public Peak HighStartPeak;
        public Peak LowStartPeak;
        public Peak HighEndPeak;
        public Peak LowEndPeak;

        private int CoreStartIndex;
        private int CoreEndIndex;

        private DateTime CoreStart;
        private DateTime CoreEnd;


        // The type of the trend in high- and low- price segment
        // TODO: will be replaced by a double in interval(-1,1)
        public TrendType HighPriceTrendType;
        public TrendType LowPriceTrendType;

        public TimeSpan CoreLength;

        public int CoreLengthInBars;

        public int SourcePeakPeriod;

        public double Intensity;

        // Access to the Algo API
        private Algo AlgoAPI;

        // General constructor taking four bordering peaks and the Algo API
        public Trend(Peak highStartPeak, Peak lowStartPeak, Peak highEndPeak, Peak lowEndPeak, double trendHeightThreshold, Algo algoAPI)
        {
            HighStartPeak = highStartPeak;
            LowStartPeak = lowStartPeak;
            HighEndPeak = highEndPeak;
            LowEndPeak = lowEndPeak;
            AlgoAPI = algoAPI;

            Peak coreStartPeak = HighStartPeak.BarIndex > LowStartPeak.BarIndex ? HighStartPeak : LowStartPeak;
            Peak coreEndPeak = HighEndPeak.BarIndex < LowEndPeak.BarIndex ? HighEndPeak : LowEndPeak;

            CoreStart = coreStartPeak.DateTime;
            CoreEnd = coreEndPeak.DateTime;

            CoreStartIndex = coreStartPeak.BarIndex;
            CoreEndIndex = coreEndPeak.BarIndex;

            CoreLength = CoreEnd - CoreStart;
            CoreLengthInBars = CoreEndIndex - CoreStartIndex;

            // TODO: To be changed to a double, also wont need a threshold. Add a source period check and assignment
            GetTrendType(trendHeightThreshold);
        }

        public override string ToString()
        {
            return string.Format("Trend HP {0}, LP {1}, start at index HP {2}, LP {3}, end at HP {4}, LP {5}",
                HighPriceTrendType, LowPriceTrendType, HighStartPeak.BarIndex, LowStartPeak.BarIndex, HighEndPeak.BarIndex, LowEndPeak.BarIndex);
        }

        public TrendLine GetHighTrendLine()
        {
            int[] coreBarsIndices = Generate.LinearRangeInt32(CoreStartIndex, CoreEndIndex);

            double[] corePrices = coreBarsIndices.Select(index => AlgoAPI.Bars.HighPrices[index]).ToArray();

            double[] coreDateTimes = coreBarsIndices.Select(index => DateTimeTicksAtBarIndex(index)).ToArray();

            Tuple<double, double> lineCoefficients = Fit.Line(coreDateTimes, corePrices);

            return new TrendLine(lineCoefficients.Item2, lineCoefficients.Item1, CoreStart, CoreEnd);
        }

        private double DateTimeTicksAtBarIndex(int index)
        {
            DateTime openTime = AlgoAPI.Bars.OpenTimes[index];

            long openTimeInTicks = openTime.Ticks;

            return Convert.ToDouble(openTimeInTicks);
        }


        public TrendLine GetLowTrendLine()
        {
            int coreStartIndex = HighStartPeak.BarIndex > LowStartPeak.BarIndex ? HighStartPeak.BarIndex : LowStartPeak.BarIndex;
            int coreEndIndex = HighEndPeak.BarIndex < LowEndPeak.BarIndex ? HighEndPeak.BarIndex : LowEndPeak.BarIndex;

            int[] indices = Generate.LinearRangeInt32(coreStartIndex, coreEndIndex);
            double[] prices = indices.Select(index => AlgoAPI.Bars.HighPrices[index]).ToArray();
            double[] dateTimes = indices.Select(index => (double)AlgoAPI.Bars.OpenTimes[index].Ticks).ToArray();
            Tuple<double, double> result = Fit.Line(dateTimes, prices);

            return new TrendLine(result.Item2, result.Item1, AlgoAPI.Bars.OpenTimes[coreStartIndex], AlgoAPI.Bars.OpenTimes[coreEndIndex]);
        }
            

        #region Visualization
        /// <summary>
        /// Draws the contours of the trend on the active chart as colored lines
        /// </summary>
        public void VisualizeContours()
        {
            Color highPriceColor = GetTrendLineColor(HighPriceTrendType);
            Color lowPriceColor = GetTrendLineColor(LowPriceTrendType);


            DrawLineBetweenPeaks(HighStartPeak, HighEndPeak, highPriceColor);
            DrawLineBetweenPeaks(LowStartPeak, LowEndPeak, lowPriceColor);

            AlgoAPI.Print(ToString());

        }
        
        private Color GetTrendLineColor(TrendType trendType)
        {
            switch (trendType)
            {
                case TrendType.Uptrend:
                    return Color.Green;
                case TrendType.Downtrend:
                    return Color.Red;
                default:
                    return Color.Yellow;
            }
        }

        private void DrawLineBetweenPeaks(Peak startPeak, Peak endPeak, Color color)
        {
            string name = string.Format("{0}_{1}_to_{2}_{3}_trend", startPeak.DateTime, startPeak.Price, endPeak.DateTime, endPeak.Price);
            AlgoAPI.Chart.DrawTrendLine(name, startPeak.DateTime, startPeak.Price, endPeak.DateTime, endPeak.Price, color);
        }
        #endregion

        #region Useless
        // TODO: following functions either obsolete or need reworking
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
                throw new ArgumentException(string.Format("{0} does not follow {1}, so they cannot be merged.", followingTrend.ToString(), ToString()));
            }
            HighEndPeak = followingTrend.HighEndPeak;
            LowEndPeak = followingTrend.LowEndPeak;
        }

        #endregion
    }
}
