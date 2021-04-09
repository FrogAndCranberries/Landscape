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

            // Four peaks bordering the current trend reading frame
            // It has the high- and low-price component, each bordered by two peaks
            Peak highStartPeak;
            Peak lowStartPeak;
            Peak highEndPeak;
            Peak lowEndPeak;

            // Split the input list into High- and low-price peaks
            List<Peak> highPeaks = peaks.FindAll(peak => peak.FromHighPrice);
            List<Peak> lowPeaks = peaks.FindAll(peak => !peak.FromHighPrice);

            // Assign the first four peaks into the reading frame
            highStartPeak = highPeaks[0];
            highEndPeak = highPeaks[1];
            lowStartPeak = lowPeaks[0];
            lowEndPeak = lowPeaks[1];

            // Remove those four peaks from the lists
            highPeaks.RemoveRange(0, 2);
            lowPeaks.RemoveRange(0, 2);

            // Store the first trend
            trendSegments.Add(new Trend(algo, highStartPeak, lowStartPeak, highEndPeak, lowEndPeak, trendTypethreshold));

            // While there are uninvestigated high- and low-price peaks left
            while (highPeaks.Count > 0 && lowPeaks.Count > 0)
            {
                // If the low-price component of the reading frame is further, advance the high-price component
                if (highEndPeak.BarIndex < lowEndPeak.BarIndex)
                {
                    highStartPeak = highEndPeak;
                    highEndPeak = highPeaks[0];
                    highPeaks.RemoveAt(0);
                }
                // If the high-price component of the reading frame is further, advance the low-price component
                else if (highEndPeak.BarIndex > lowEndPeak.BarIndex)
                {
                    lowStartPeak = lowEndPeak;
                    lowEndPeak = lowPeaks[0];
                    lowPeaks.RemoveAt(0);
                }
                // Otherwise they must end at the same spot, so advance high-price component
                // But do not save a trend, as there is no overlap between the components
                else
                {
                    highStartPeak = highEndPeak;
                    highEndPeak = highPeaks[0];
                    highPeaks.RemoveAt(0);
                    continue;
                }

                // Save the trend in the new reading frame
                trendSegments.Add(new Trend(algo, highStartPeak, lowStartPeak, highEndPeak, lowEndPeak, trendTypethreshold));
            }

            // If there are high-price peaks left, move the high-component over them and save the last trends
            while (highPeaks.Count > 0)
            {
                highStartPeak = highEndPeak;
                highEndPeak = highPeaks[0];
                highPeaks.RemoveAt(0);
                trendSegments.Add(new Trend(algo, highStartPeak, lowStartPeak, highEndPeak, lowEndPeak, trendTypethreshold));
            }

            // If there are low-price peaks left, move the low-component over them and save the last trends
            while (lowPeaks.Count > 0)
            {
                lowStartPeak = lowEndPeak;
                lowEndPeak = lowPeaks[0];
                lowPeaks.RemoveAt(0);
                trendSegments.Add(new Trend(algo, highStartPeak, lowStartPeak, highEndPeak, lowEndPeak, trendTypethreshold));
            }

            return trendSegments;
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
