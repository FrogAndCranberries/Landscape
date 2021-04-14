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
    class LineFinder
    {
        private Algo AlgoAPI { get; set; }

        public LineFinder(Algo algoAPI)
        {
            AlgoAPI = algoAPI;
        }

        public List<ResistanceLine> FindLines(List<Peak> peaks, List<Trend> trends, int supportLineDistanceToMergeInPips)
        {
            List<ResistanceLine> resistanceLines = new List<ResistanceLine>();

            // TODO: Deal with very short trends
            // TODO: If trendline is almost flat, turn it into supportLine

            foreach (Trend trend in trends)
            {
                resistanceLines.Add(GetHighTrendLine(trend));
                resistanceLines.Add(GetLowTrendLine(trend));
            }
            resistanceLines.AddRange(GetSupportLines(trends, supportLineDistanceToMergeInPips));

            return resistanceLines;
        }

        private List<ResistanceLine> GetSupportLines(List<Trend> trends, int supportLineDistanceToMergeInPips)
        {
            List<SupportLine> supportLines = new List<SupportLine>();

            double minimalSupportLineDistanceToMergeConstant = supportLineDistanceToMergeInPips * AlgoAPI.Symbol.PipSize;

            foreach(Trend trend in trends)
            {
                if (!trend.FormsSupportLine()) continue;

                SupportLine newLine = GetSupportLine(trend);

                List<SupportLine> closeLines = supportLines.FindAll(
                    line => line != null && Math.Abs(line.Price - newLine.Price) < minimalSupportLineDistanceToMergeConstant);
                
                if(closeLines.Count == 0)
                {
                    supportLines.Add(newLine);
                    continue;
                }

                SupportLine mostIntensiveLine = closeLines.OrderByDescending(line => line.Intensity).First();
                mostIntensiveLine.MergeWithLine(newLine);
            }

            return supportLines.Select(supportLine => supportLine as ResistanceLine).ToList();
        }

        /// <summary>
        /// Returns the Trendline derived from the high prices in the trend core
        /// </summary>
        /// <returns></returns>
        public TrendLine GetHighTrendLine(Trend trend)
        {
            TrendCore core = trend.Core;

            int[] coreBarsIndices = Generate.LinearRangeInt32(core.StartIndex, core.EndIndex);

            double[] corePrices = coreBarsIndices.Select(index => AlgoAPI.Bars.HighPrices[index]).ToArray();

            double[] coreIndicesAsDouble = coreBarsIndices.Select(index => (double)index).ToArray();

            Tuple<double, double> lineCoefficients = Fit.Line(coreIndicesAsDouble, corePrices);

            TrendLine highTrendLine = new TrendLine(lineCoefficients.Item2, lineCoefficients.Item1, core, Color.Green);

            return highTrendLine;
        }

        /// <summary>
        /// returns the TrendLine derived from the low prices in the trend core
        /// </summary>
        /// <returns></returns>
        public TrendLine GetLowTrendLine(Trend trend)
        {
            TrendCore core = trend.Core;

            int[] coreBarsIndices = Generate.LinearRangeInt32(core.StartIndex, core.EndIndex);

            double[] corePrices = coreBarsIndices.Select(index => AlgoAPI.Bars.LowPrices[index]).ToArray();

            double[] coreIndicesAsDouble = coreBarsIndices.Select(index => (double)index).ToArray();

            Tuple<double, double> lineCoefficients = Fit.Line(coreIndicesAsDouble, corePrices);

            TrendLine lowTrendLine = new TrendLine(lineCoefficients.Item2, lineCoefficients.Item1, core, Color.Blue);

            return lowTrendLine;
        }

        public SupportLine GetSupportLine(Trend trend)
        {
            if (!trend.FormsSupportLine())
            {
                string message = string.Format("Trend {0} cannot form a support line.", ToString());
                throw new InvalidOperationException(message);
            }

            bool isUptrend = trend.HighTrendType == TrendType.Uptrend && trend.LowTrendType == TrendType.Uptrend;

            Peak endPeak = isUptrend ? trend.HighEndPeak : trend.LowEndPeak;

            double lineIntensity = GetSupportLineIntensity(trend);

            SupportLine newSupportLine = new SupportLine(endPeak, lineIntensity);

            return newSupportLine;
        }

        private double GetSupportLineIntensity(Trend trend)
        {
            return trend.Intensity;
        }
    }
}
