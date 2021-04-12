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
            // TODO: If trendline is almost flat, turn it into supportLine

            foreach (Trend trend in trends)
            {
                resistanceLines.Add(GetHighTrendLine(trend));
                resistanceLines.Add(GetLowTrendLine(trend));

                if (trend.FormsSupportLine()) AddSupportLine(trend, resistanceLines);
            }

            return resistanceLines;
        }

        private void AddSupportLine(Trend trend, List<ResistanceLine> resistanceLines)
        {
            SupportLine newSupportLine = GetSupportLine(trend);

            double minConstant = 0.00;

            List<ResistanceLine> supportLines = resistanceLines.FindAll(line => line is SupportLine);

            List<SupportLine> nearbySupportLines1 = resistanceLines.Select(line => line as SupportLine).ToList();

            List<SupportLine> nearbySupportLines = nearbySupportLines1.FindAll(line => line != null && Math.Abs(line.Price - newSupportLine.Price) < minConstant).ToList();

            if (nearbySupportLines.Count > 0)
            {
                SupportLine mergeLine = nearbySupportLines.OrderBy(line => line.Intensity).First();
                int index = resistanceLines.FindIndex(line => line == mergeLine);
                resistanceLines[index].Intensity += newSupportLine.Intensity;
            }
            else
            {
                resistanceLines.Add(newSupportLine);
            }
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

            bool isUptrend = trend.HighTrendType == TrendType.Uptrend && trend.LowTrendType == TrendType.Uptrend;

            Color lineColor = isUptrend ? Color.Green : Color.Red;

            double price = isUptrend ? trend.HighEndPeak.Price : trend.LowEndPeak.Price;

            double lineIntensity = GetSupportLineIntensity(trend);
            
            return new SupportLine(price, lineIntensity, trend.LowEndPeak.BarIndex, trend.LowEndPeak.DateTime, lineColor);
        }

        private double GetSupportLineIntensity(Trend trend)
        {
            return trend.Intensity;
        }
    }
}
