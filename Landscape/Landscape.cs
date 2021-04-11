﻿using System;
using System.Linq;
using System.Collections.Generic;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class Landscape : Robot
    {
        #region Parameters
        /// <summary>
        /// Determines the minimum number of bars between peaks of the same kind
        /// </summary>
        [Parameter(DefaultValue = 10, MinValue = 1)]
        public int PeakSearchPeriod { get; set; }

        /// <summary>
        /// Determines a minimal up- and downtrend gradient as price change per 50 000 bars. Might have to be landcape-layer specific
        /// </summary>
        [Parameter(DefaultValue = 1, MinValue = 0, MaxValue = 100)]
        public int trendTypeThreshold { get; set; }

        [Parameter(DefaultValue = false)]
        public bool ShouldVisualizePeaks { get; set; }

        [Parameter(DefaultValue = false)]
        public bool ShouldVisualizeTrendContours { get; set; }

        [Parameter(DefaultValue = false)]
        public bool ShouldVisualizeResistanceLines { get; set; }

        #endregion

        #region Fields
        /// <summary>
        /// Will determine the peakSearchPeriod for each timeframe layer of landscape creation. Values unknown yet
        /// </summary>
        List<int> Periods = new List<int>() {5, 10, 20};

        #endregion

        #region Core fuctions

        /// <summary>
        /// Instantiates the algorithm by creating a landscape and calculating trading conditions based on it
        /// </summary>
        protected override void OnStart()
        {
            CreateLandscape();
            CreateConditions();
        }

        /// <summary>
        /// Checks if the trading conditions were met after each tick
        /// </summary>
        protected override void OnTick()
        {
            CheckConditions();
        }

        /// <summary>
        /// Recreates the landscape and the trading conditions after each bar
        /// </summary>
        protected override void OnBar()
        {
            CreateLandscape();
            CreateConditions();
        }

        protected override void OnStop()
        {
            // Put your deinitialization logic here
        }

        #endregion

        #region Methods

        private void CreateLandscape()
        {
            PeakFinder peakFinder = new PeakFinder(this);

            List<Peak> peaks= peakFinder.FindPeaks(PeakSearchPeriod);

            TrendFinder trendFinder = new TrendFinder(this, trendTypeThreshold);

            List<Trend> trends = trendFinder.FindTrends(peaks);

            List<IResistanceLine> resistanceLines = IdentifyLines(peaks, trends);

            if (ShouldVisualizePeaks) VisualizePeaks(peaks);
            if (ShouldVisualizeTrendContours) VisualizeTrendsContours(trends);
            if (ShouldVisualizeResistanceLines) VisualizeResistanceLines(resistanceLines);

            //Will be used to get multiple landscape layers with different line id periods
            /*foreach(int period in Periods)
            {
                BaseLines.AddRange(IdentifyLines(period, trendTypeThreshold));
            }*/
        }

        #region IdentifyLines

        private List<IResistanceLine> IdentifyLines(List<Peak> peaks, List<Trend> trends)
        {
            List<IResistanceLine> resistanceLines = new List<IResistanceLine>();

            // TODO: Deal with very short trends

            foreach(Trend trend in trends)
            {
                resistanceLines.Add(trend.GetHighTrendLine());
                resistanceLines.Add(trend.GetLowTrendLine());
                if (trend.FormsSupportLine()) resistanceLines.Add(trend.GetSupportLine());
            }

            return resistanceLines;
        }
        #endregion

        #region FindTrends



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
        #endregion

        #region Visualization
        /// <summary>
        /// Visualizes each Peak in a list on the chart
        /// </summary>
        /// <param name="peaks"></param>
        private void VisualizePeaks(List<Peak> peaks)
        {
            foreach(Peak peak in peaks)
            {
                peak.Visualize(Chart);
            }
        }

        /// <summary>
        /// Visualizes the high- and low-price contours of each Trend in a list on the chart
        /// </summary>
        /// <param name="trends"></param>
        private void VisualizeTrendsContours(List<Trend> trends)
        {
            foreach(Trend trend in trends)
            {
                trend.VisualizeContours(Chart);
            }
        }

        /// <summary>
        /// Visualizes each ResistanceLine in a list on the chart
        /// </summary>
        /// <param name="resistanceLines"></param>
        private void VisualizeResistanceLines(List<IResistanceLine> resistanceLines)
        {
            foreach(IResistanceLine resistanceLine in resistanceLines)
            {
                resistanceLine.Visualize(Chart);
            }
        }
        #endregion

        private void CreateConditions()
        {

        }

        private void CheckConditions()
        {

        }

        #endregion
    }
}