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
        Algo AlgoAPI;

        public LineFinder(Algo algoAPI)
        {
            AlgoAPI = algoAPI;
        }

        public List<ResistanceLine> FindLines(List<Peak> peaks, List<Trend> trends)
        {
            List<ResistanceLine> resistanceLines = new List<ResistanceLine>();

            // TODO: Deal with very short trends

            foreach (Trend trend in trends)
            {
                resistanceLines.Add(GetHighTrendLine(trend));
                resistanceLines.Add(GetLowTrendLine(trend));
                if (trend.FormsSupportLine()) resistanceLines.Add(GetSupportLine(trend));
            }

            return resistanceLines;
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

            TrendLine highTrendLine = new TrendLine(lineCoefficients.Item2, lineCoefficients.Item1, core.StartTime, core.EndTime, core.StartIndex, core.EndIndex, Color.Green);

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

            TrendLine lowTrendLine = new TrendLine(lineCoefficients.Item2, lineCoefficients.Item1, core.StartTime, core.EndTime, core.StartIndex, core.EndIndex, Color.Blue);

            return lowTrendLine;
        }

        public SupportLine GetSupportLine(Trend trend)
        {
            if (!trend.FormsSupportLine())
            {
                string message = string.Format("Trend {0} cannot form a support line.", ToString());
                throw new InvalidOperationException(message);
            }

            if (trend.HighTrendType == TrendType.Uptrend && trend.LowTrendType == TrendType.Uptrend)
            {
                return new SupportLine(trend.HighEndPeak.Price, trend.HighEndPeak.BarIndex, trend.HighEndPeak.DateTime, Color.Green);
            }
            return new SupportLine(trend.LowEndPeak.Price, trend.LowEndPeak.BarIndex, trend.LowEndPeak.DateTime, Color.Red);
        }
    }
}
