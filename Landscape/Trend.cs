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
        #region Fields
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

        #endregion

        /// <summary>
        /// General constructor taking four bordering peaks, the Algo API and the slope threshold for trend type determination
        /// </summary>
        /// <param name="algoAPI"></param>
        /// <param name="highStartPeak"></param>
        /// <param name="lowStartPeak"></param>
        /// <param name="highEndPeak"></param>
        /// <param name="lowEndPeak"></param>
        /// <param name="slopeThreshold"></param>
        public Trend(Algo algoAPI, Peak highStartPeak, Peak lowStartPeak, Peak highEndPeak, Peak lowEndPeak, double slopeThreshold)
        {
            ValidateInputPeaks(highStartPeak, lowStartPeak, highEndPeak, lowEndPeak);

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

        private void ValidateInputPeaks(Peak highStartPeak, Peak lowStartPeak, Peak highEndPeak, Peak lowEndPeak)
        {
            if (highStartPeak.BarIndex >= highEndPeak.BarIndex ||
                lowStartPeak.BarIndex >= lowEndPeak.BarIndex ||
                highStartPeak.BarIndex >= lowEndPeak.BarIndex ||
                lowStartPeak.BarIndex >= highEndPeak.BarIndex)
            {
                string message = string.Format("Peaks {0}, {1}, {2}, {3} do not form a valid trend core.",
                    highStartPeak, lowStartPeak, highEndPeak, lowEndPeak);
                throw new ArgumentException();
            }
        }

        public override string ToString()
        {
            return string.Format("Trend HP {0}, LP {1}, start at index HP {2}, LP {3}, end at HP {4}, LP {5}",
                HighTrendType, LowTrendType, HighStartPeak.BarIndex, LowStartPeak.BarIndex, HighEndPeak.BarIndex, LowEndPeak.BarIndex);
        }

        /// <summary>
        /// Returns the slope of a line between two peaks in price change per bar
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        private double GetTrendSlope(Peak start, Peak end)
        {
            return (end.Price - start.Price) / (end.BarIndex - start.BarIndex);
        }

        /// <summary>
        /// Determines the trend type of a line with a given slope and a slope threshold
        /// </summary>
        /// <param name="trendSlope"></param>
        /// <param name="slopeThreshold"></param>
        /// <returns></returns>
        private TrendType GetTrendType(double trendSlope, double slopeThreshold)
        {
            if (trendSlope > slopeThreshold) return TrendType.Uptrend;
            if (trendSlope < -slopeThreshold) return TrendType.Downtrend;
            return TrendType.Consolidation;
        }

        /// <summary>
        /// Returns the Trendline derived from the high prices in the trend core
        /// </summary>
        /// <returns></returns>
        public TrendLine GetHighTrendLine()
        {
            int[] coreBarsIndices = Generate.LinearRangeInt32(Core.StartIndex, Core.EndIndex);

            double[] corePrices = coreBarsIndices.Select(index => AlgoAPI.Bars.HighPrices[index]).ToArray();

            double[] coreIndicesAsDouble = coreBarsIndices.Select(index => (double)index).ToArray();

            Tuple<double, double> lineCoefficients = Fit.Line(coreIndicesAsDouble, corePrices);

            TrendLine highTrendLine = new TrendLine(lineCoefficients.Item2, lineCoefficients.Item1, Core.StartTime, Core.EndTime, Core.StartIndex, Core.EndIndex,Color.Green);

            return highTrendLine;
        }

        /// <summary>
        /// returns the TrendLine derived from the low prices in the trend core
        /// </summary>
        /// <returns></returns>
        public TrendLine GetLowTrendLine()
        {
            int[] coreBarsIndices = Generate.LinearRangeInt32(Core.StartIndex, Core.EndIndex);

            double[] corePrices = coreBarsIndices.Select(index => AlgoAPI.Bars.LowPrices[index]).ToArray();

            double[] coreIndicesAsDouble = coreBarsIndices.Select(index => (double)index).ToArray();

            Tuple<double, double> lineCoefficients = Fit.Line(coreIndicesAsDouble, corePrices);

            TrendLine lowTrendLine = new TrendLine(lineCoefficients.Item2, lineCoefficients.Item1, Core.StartTime, Core.EndTime, Core.StartIndex, Core.EndIndex, Color.Blue);

            return lowTrendLine;
        }

        public SupportLine GetSupportLine()
        {
            if(!FormsSupportLine())
            {
                string message = string.Format("Trend {0} cannot form a support line.", ToString());
                throw new InvalidOperationException(message);
            }

            if (HighTrendType == TrendType.Uptrend && LowTrendType == TrendType.Uptrend)
            {
                return new SupportLine(HighEndPeak.Price, HighEndPeak.BarIndex, HighEndPeak.DateTime, Color.Green);
            }
            return new SupportLine(LowEndPeak.Price, LowEndPeak.BarIndex, LowEndPeak.DateTime, Color.Red);
        }

        public bool FormsSupportLine()
        {
            return (HighTrendType == TrendType.Uptrend && LowTrendType == TrendType.Uptrend) ||
                (HighTrendType == TrendType.Downtrend && LowTrendType == TrendType.Downtrend);
        }
        #region Visualization
        /// <summary>
        /// Draws the contours of the trend on the given chart as colored lines
        /// </summary>
        public void VisualizeContours(Chart chart)
        {
            Color highPriceColor = GetTrendContourColor(HighTrendType);
            Color lowPriceColor = GetTrendContourColor(LowTrendType);

            DrawLineBetweenPeaks(chart, HighStartPeak, HighEndPeak, highPriceColor);
            DrawLineBetweenPeaks(chart, LowStartPeak, LowEndPeak, lowPriceColor);

            AlgoAPI.Print(ToString());
        }
        
        /// <summary>
        /// Returns the color of a trend contour based on its trendType
        /// </summary>
        /// <param name="trendType"></param>
        /// <returns></returns>
        private Color GetTrendContourColor(TrendType trendType)
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

        /// <summary>
        /// Draws a line of a given color between two peaks on the chart
        /// </summary>
        /// <param name="chart"></param>
        /// <param name="startPeak"></param>
        /// <param name="endPeak"></param>
        /// <param name="color"></param>
        private void DrawLineBetweenPeaks(Chart chart, Peak startPeak, Peak endPeak, Color color)
        {
            string name = string.Format("{0}_{1}_to_{2}_{3}_trend", startPeak.DateTime, startPeak.Price, endPeak.DateTime, endPeak.Price);
            chart.DrawTrendLine(name, startPeak.DateTime, startPeak.Price, endPeak.DateTime, endPeak.Price, color);
        }
        #endregion

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
