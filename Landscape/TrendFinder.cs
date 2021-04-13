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
    class TrendFinder
    {
        private Algo AlgoAPI { get; }
        private double TrendTypeThreshold { get; }


        public TrendFinder(Algo algoAPI, int trendTypeThreshold)
        {
            AlgoAPI = algoAPI;
            TrendTypeThreshold = (double)trendTypeThreshold / 50000;
        }

        /// <summary>
        /// Finds all trends between peaks in a given list
        /// </summary>
        /// <param name="peaks">A list of at least two high price and at least two low price Peaks bordering the trends</param>
        /// <returns></returns>
        public List<Trend> FindTrends(List<Peak> peaks)
        {
            List<Trend> trendSegments = GetTrendSegments(peaks);

            return trendSegments;

            //TODO: What to do with short trend segemnts we have now?
        }

        /// <summary>
        /// Finds all shortest possible trends between peaks in a given list
        /// </summary>
        /// <param name="peaks">List of peaks bordering the serched trends</param>
        /// <returns>List of all found trends</returns>
        private List<Trend> GetTrendSegments(List<Peak> peaks)
        {
            ValidateInputPeakList(peaks);

            List<Peak> highPeaks = peaks.FindAll(peak => peak.FromHighPrice);
            List<Peak> lowPeaks = peaks.FindAll(peak => !peak.FromHighPrice);

            List<Trend> trendSegments = new List<Trend>();

            TrendReadingFrame readingFrame = new TrendReadingFrame(highPeaks[0], lowPeaks[0], TrendTypeThreshold);

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

        private void ValidateInputPeakList(List<Peak> peaks)
        {
            List<Peak> highPeaks = peaks.FindAll(peak => peak.FromHighPrice);
            List<Peak> lowPeaks = peaks.FindAll(peak => !peak.FromHighPrice);

            // TODO: Create Peak ordering based on barindex and check the input list was ordered
            if (highPeaks.Count < 2 || lowPeaks.Count < 2)
            {
                string message = "Cannot find trends between less than two high- and two low-price peaks.";
                throw new ArgumentException(message);
            }
        }

        //Useless rest
        private List<Trend> MergeTrendSegments(List<Trend> trendSegments)
        {
            if (trendSegments.Count == 1)
            {
                return trendSegments;
            }

            List<Trend> mergedTrends = new List<Trend>();

            //Combine the short trends if they have the same type
            Trend currentTrend = trendSegments[0];
            trendSegments.RemoveAt(0);

            foreach (Trend trend in trendSegments)
            {
                if (currentTrend.HasSameTrendType(trend))
                {
                    currentTrend.CombineWithFollowingTrend(trend);
                }
                else
                {
                    mergedTrends.Add(currentTrend);
                    currentTrend = trend;
                }
            }

            //Add the last checked trend
            mergedTrends.Add(currentTrend);

            return mergedTrends;
        }
    }
}
