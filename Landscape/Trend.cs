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

        private TrendCore Core;

        public double HighTrendSlope;
        public double LowTrendSlope;

        public TrendType HighTrendType;
        public TrendType LowTrendType;

        public int SourcePeakPeriod;

        public double Intensity;

        // Access to the Algo API
        private Algo AlgoAPI;

        // General constructor taking four bordering peaks and the Algo API
        public Trend(Algo algoAPI, Peak highStartPeak, Peak lowStartPeak, Peak highEndPeak, Peak lowEndPeak, double slopeThreshold)
        {
            HighStartPeak = highStartPeak;
            LowStartPeak = lowStartPeak;
            HighEndPeak = highEndPeak;
            LowEndPeak = lowEndPeak;

            Core = new TrendCore(HighStartPeak, LowStartPeak, HighEndPeak, LowEndPeak);

            AlgoAPI = algoAPI;

            // TODO: Add a source period check and assignment
            HighTrendSlope = GetTrendSlope(highStartPeak, highEndPeak);
            LowTrendSlope = GetTrendSlope(lowStartPeak, lowEndPeak);
            HighTrendType = GetTrendType(HighTrendSlope, slopeThreshold);
            LowTrendType = GetTrendType(LowTrendSlope, slopeThreshold);
        }

        public override string ToString()
        {
            return string.Format("Trend HP {0}, LP {1}, start at index HP {2}, LP {3}, end at HP {4}, LP {5}",
                HighTrendType, LowTrendType, HighStartPeak.BarIndex, LowStartPeak.BarIndex, HighEndPeak.BarIndex, LowEndPeak.BarIndex);
        }

        public TrendLine GetHighTrendLine()
        {
            int[] coreBarsIndices = Generate.LinearRangeInt32(Core.StartIndex, Core.EndIndex);

            double[] corePrices = coreBarsIndices.Select(index => AlgoAPI.Bars.HighPrices[index]).ToArray();

            //double[] coreDateTimes = coreBarsIndices.Select(index => DateTimeTicksAtBarIndex(index)).ToArray();

            double[] coreIndices = coreBarsIndices.Select(index => (double)index).ToArray();

            Tuple<double, double> lineCoefficients = Fit.Line(coreIndices, corePrices);

            TrendLine highTrendLine = new TrendLine(lineCoefficients.Item2, lineCoefficients.Item1, Core.StartTime, Core.EndTime, Core.StartIndex, Core.EndIndex, Core.SpansWeekend() ? Color.Blue : Color.Green);

            return highTrendLine;
        }

        public TrendLine GetLowTrendLine()
        {
            int[] coreBarsIndices = Generate.LinearRangeInt32(Core.StartIndex, Core.EndIndex);

            double[] corePrices = coreBarsIndices.Select(index => AlgoAPI.Bars.LowPrices[index]).ToArray();

            //double[] coreDateTimes = coreBarsIndices.Select(index => DateTimeTicksAtBarIndex(index)).ToArray();


            double[] coreIndices = coreBarsIndices.Select(index => (double)index).ToArray();

            Tuple<double, double> lineCoefficients = Fit.Line(coreIndices, corePrices);

            TrendLine lowTrendLine = new TrendLine(lineCoefficients.Item2, lineCoefficients.Item1, Core.StartTime, Core.EndTime, Core.StartIndex, Core.EndIndex, Color.Red);

            return lowTrendLine;
        }

        private double DateTimeTicksAtBarIndex(int index)
        {
            DateTime openTime = AlgoAPI.Bars.OpenTimes[index];

            long openTimeInTicks = openTime.Ticks;

            return Convert.ToDouble(openTimeInTicks);
        }

        #region Visualization
        /// <summary>
        /// Draws the contours of the trend on the active chart as colored lines
        /// </summary>
        public void VisualizeContours(Chart chart)
        {
            Color highPriceColor = GetTrendLineColor(HighTrendType);
            Color lowPriceColor = GetTrendLineColor(LowTrendType);

            DrawLineBetweenPeaks(chart, HighStartPeak, HighEndPeak, highPriceColor);
            DrawLineBetweenPeaks(chart, LowStartPeak, LowEndPeak, lowPriceColor);
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

        private void DrawLineBetweenPeaks(Chart chart, Peak startPeak, Peak endPeak, Color color)
        {
            string name = string.Format("{0}_{1}_to_{2}_{3}_trend", startPeak.DateTime, startPeak.Price, endPeak.DateTime, endPeak.Price);
            chart.DrawTrendLine(name, startPeak.DateTime, startPeak.Price, endPeak.DateTime, endPeak.Price, color);
        }
        #endregion

        
        private double GetTrendSlope(Peak start, Peak end)
        {
            return (end.Price - start.Price) / (end.BarIndex - start.BarIndex);
        }

        private TrendType GetTrendType(double trendSlope, double slopeThreshold)
        {
            if (trendSlope > slopeThreshold) return TrendType.Uptrend;
            if (trendSlope < -slopeThreshold) return TrendType.Downtrend;
            return TrendType.Consolidation;
        }

        /// <summary>
        /// Finds all shortest possible trends between peaks in a given list
        /// </summary>
        /// <param name="peaks">List of peaks bordering the serched trends</param>
        /// <returns>List of all found trends</returns>
        public static List<Trend> GetTrendSegments(Algo algo, List<Peak> peaks, double trendTypethreshold)
        {
            List<Trend> trendSegments = new List<Trend>();

            List<Peak> highPeaks = peaks.FindAll(peak => peak.FromHighPrice);
            List<Peak> lowPeaks = peaks.FindAll(peak => !peak.FromHighPrice);

            // TODO: Check there are at least two low and two high Peaks

            TrendReadingFrame readingFrame = new TrendReadingFrame(algo, highPeaks[0], lowPeaks[0], highPeaks[1], lowPeaks[1], trendTypethreshold);

            highPeaks.RemoveRange(0, 2);
            lowPeaks.RemoveRange(0, 2);

            trendSegments.Add(readingFrame.GetCurrentTrend());

            while (highPeaks.Count > 0 && lowPeaks.Count > 0)
            {
                if(!readingFrame.ShouldAdvanceHigh() && !readingFrame.ShouldAdvanceLow())
                {
                    readingFrame.AdvanceHigh(highPeaks[0]);
                    highPeaks.RemoveAt(0);
                }
                if (readingFrame.ShouldAdvanceHigh())
                {
                    trendSegments.Add(readingFrame.AdvanceHigh(highPeaks[0]));
                    highPeaks.RemoveAt(0);
                }
                if (readingFrame.ShouldAdvanceLow())
                {
                    trendSegments.Add(readingFrame.AdvanceLow(lowPeaks[0]));
                    lowPeaks.RemoveAt(0);
                }
            }

            while (highPeaks.Count > 0)
            {
                trendSegments.Add(readingFrame.AdvanceHigh(highPeaks[0]));
                highPeaks.RemoveAt(0);
            }

            while (lowPeaks.Count > 0)
            {
                trendSegments.Add(readingFrame.AdvanceLow(lowPeaks[0]));
                lowPeaks.RemoveAt(0);
            }

            return trendSegments;
        }

        private class TrendReadingFrame
        {
            Peak HighStartPeak;
            Peak LowStartPeak;
            Peak HighEndPeak;
            Peak LowEndPeak;

            Algo Algo;
            double TrendTypeThreshold;

            public TrendReadingFrame(Algo algo, Peak highStartPeak, Peak lowStartPeak, Peak highEndPeak, Peak lowEndPeak, double trendTypeThreshold)
            {
                HighStartPeak = highStartPeak;
                LowStartPeak = lowStartPeak;
                HighEndPeak = highEndPeak;
                LowEndPeak = lowEndPeak;

                Algo = algo;
                TrendTypeThreshold = trendTypeThreshold;
            }

            public bool ShouldAdvanceHigh()
            {
                return HighEndPeak.BarIndex < LowEndPeak.BarIndex;
            }

            public Trend AdvanceHigh(Peak newHighPeak)
            {
                HighStartPeak = HighEndPeak;
                HighEndPeak = newHighPeak;
                return new Trend(Algo, HighStartPeak, LowStartPeak, HighEndPeak, LowEndPeak, TrendTypeThreshold);
            }

            public bool ShouldAdvanceLow()
            {
                return HighEndPeak.BarIndex > LowEndPeak.BarIndex;
            }

            public Trend AdvanceLow(Peak newLowPeak)
            {
                LowStartPeak = LowEndPeak;
                LowEndPeak = newLowPeak;
                return new Trend(Algo, HighStartPeak, LowStartPeak, HighEndPeak, LowEndPeak, TrendTypeThreshold);
            }

            public Trend GetCurrentTrend()
            {
                return new Trend(Algo, HighStartPeak, LowStartPeak, HighEndPeak, LowEndPeak, TrendTypeThreshold);
            }
        }

        #region Useless
        // TODO: following functions either obsolete or need reworking


        public bool HasSameTrendType(Trend other)
        {
            // TODO: implement or delete
            return false;
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
