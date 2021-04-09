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

        #region Trend Creation
        /// <summary>
        /// Finds all shortest possible trends between peaks in a given list
        /// </summary>
        /// <param name="peaks">List of peaks bordering the serched trends</param>
        /// <returns>List of all found trends</returns>
        public static List<Trend> GetTrendSegments(Algo algo, List<Peak> peaks, double trendTypethreshold)
        {
            List<Peak> highPeaks = peaks.FindAll(peak => peak.FromHighPrice);
            List<Peak> lowPeaks = peaks.FindAll(peak => !peak.FromHighPrice);

            // Validate input
            // TODO: Create Peak ordering based on barindex and check the input list was ordered
            if (highPeaks.Count < 2 || lowPeaks.Count < 2)
            {
                string message = "Cannot find trends between less than two high- and two low-price peaks.";
                throw new ArgumentException(message);
            }

            List<Trend> trendSegments = new List<Trend>();

            TrendReadingFrame readingFrame = new TrendReadingFrame(algo, highPeaks[0], lowPeaks[0], trendTypethreshold);
            
            highPeaks.RemoveAt(0);
            lowPeaks.RemoveAt(0);

            // Ala mergesort

            while (highPeaks.Count > 0 && lowPeaks.Count > 0)
            {
                
                if (!readingFrame.ShouldAdvanceHigh() && !readingFrame.ShouldAdvanceLow())
                {
                    readingFrame.AdvanceHigh(highPeaks[0]);
                    highPeaks.RemoveAt(0);
                }
                if (readingFrame.ShouldAdvanceHigh())
                {
                    readingFrame.AdvanceHigh(highPeaks[0]);
                    trendSegments.Add(readingFrame.GetTrend());
                    highPeaks.RemoveAt(0);
                }
                if (readingFrame.ShouldAdvanceLow())
                {
                    readingFrame.AdvanceLow(lowPeaks[0]);
                    trendSegments.Add(readingFrame.GetTrend());
                    lowPeaks.RemoveAt(0);
                }
            }

            while (highPeaks.Count > 0)
            {
                readingFrame.AdvanceHigh(highPeaks[0]);
                trendSegments.Add(readingFrame.GetTrend());
                highPeaks.RemoveAt(0);
            }

            while (lowPeaks.Count > 0)
            {
                readingFrame.AdvanceLow(lowPeaks[0]);
                trendSegments.Add(readingFrame.GetTrend());
                lowPeaks.RemoveAt(0);
            }
            
            return trendSegments;
        }

        /// <summary>
        /// Represents the reading frame on two lists of peaks during trend segment identification
        /// </summary>
        private class TrendReadingFrame
        {
            Peak HighStartPeak;
            Peak LowStartPeak;
            Peak HighEndPeak;
            Peak LowEndPeak;

            Algo Algo;
            double TrendTypeThreshold;

            /// <summary>
            /// Sets three out of the four peaks, the fourth will be pushed in at the first advance
            /// </summary>
            /// <param name="algo"></param>
            /// <param name="highStartPeak"></param>
            /// <param name="highEndPeak"></param>
            /// <param name="lowEndPeak"></param>
            /// <param name="trendTypeThreshold"></param>
            public TrendReadingFrame(Algo algo, Peak highEndPeak, Peak lowEndPeak, double trendTypeThreshold)
            {
                ValidateInputPeaks(highEndPeak, lowEndPeak);

                HighEndPeak = highEndPeak;
                LowEndPeak = lowEndPeak;

                Algo = algo;
                TrendTypeThreshold = trendTypeThreshold;
            }

            private void ValidateInputPeaks(Peak highEndPeak, Peak lowEndPeak)
            {
                if(highEndPeak.BarIndex != lowEndPeak.BarIndex)
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
                return new Trend(Algo, HighStartPeak, LowStartPeak, HighEndPeak, LowEndPeak, TrendTypeThreshold);
            }
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
