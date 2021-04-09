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
            List<Peak> peaks= IdentifyPeaks(PeakSearchPeriod);
            
            List<Trend> trends = IdentifyTrends(peaks);

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
                if (trend.ShouldGetSupportLine()) resistanceLines.Add(trend.GetSupportLine());
            }

            return resistanceLines;
        }
        #endregion

        #region IdentifyPeaks

        /// <summary>
        /// Finds all maxima and minima of high and low price with given period in Bars. 
        /// </summary>
        /// <param name="period">Minimal period the peaks must have before and after them</param>
        /// <returns>List of all found peaks</returns>
        private List<Peak> IdentifyPeaks(int period)
        {
            // Stores found peaks
            List<Peak> foundPeaks = new List<Peak>();

            // TODO: split new peak creation and foundPeaks.add
            // For each bar
            for (int index = 0; index < Bars.Count; index++)
            {
                // Check if the bar at index is a maximum or minimum of high price in the specified period
                // If so, it adds a new peak corresponding to it to found peaks
                if (isHighPriceMaximum(index, period))
                {
                    foundPeaks.Add(new Peak(
                        fromHighPrice: true,
                        peakType: PeakType.Maximum, 
                        datetime: Bars.OpenTimes[index], 
                        barIndex: index, 
                        price: Bars.HighPrices[index], 
                        sourcePeriod: period,
                        algoAPI: this));
                }
                else if (isHighPriceMinimum(index, period))
                {
                    foundPeaks.Add(new Peak(
                        fromHighPrice: true,
                        peakType: PeakType.Minimum,
                        datetime: Bars.OpenTimes[index],
                        barIndex: index,
                        price: Bars.HighPrices[index],
                        sourcePeriod: period,
                        algoAPI: this));
                }

                // Check if the bar at index is a maximum or minimum of low price in the specified period
                // If so, it adds a new peak corresponding to it to found peaks
                if (isLowPriceMaximum(index, period))
                {
                    foundPeaks.Add(new Peak(
                        fromHighPrice: false,
                        peakType: PeakType.Maximum,
                        datetime: Bars.OpenTimes[index],
                        barIndex: index,
                        price: Bars.LowPrices[index],
                        sourcePeriod: period,
                        algoAPI: this));
                }
                else if (isLowPriceMinimum(index, period))
                {
                    foundPeaks.Add(new Peak(
                        fromHighPrice: false,
                        peakType: PeakType.Minimum,
                        datetime: Bars.OpenTimes[index],
                        barIndex: index,
                        price: Bars.LowPrices[index],
                        sourcePeriod: period,
                        algoAPI: this));
                }
            }

            // For trend search, there should be a high and low price peak at first and last bar
            // If there is no high price peak at the beginning of Bars series, add a peak corresponding to first bar high price
            if(foundPeaks.Find(peak => peak.FromHighPrice).BarIndex != 0)
            {
                foundPeaks.Insert(0, new Peak(
                    fromHighPrice: true,
                    peakType: (Bars.HighPrices[0] > Bars.HighPrices[1]) ? PeakType.Maximum : PeakType.Minimum,
                    datetime: Bars.OpenTimes[0],
                    barIndex: 0,
                    price: Bars.HighPrices[0],
                    sourcePeriod: period,
                    algoAPI: this));
            }

            // If there is no low price peak at the beginning of Bars series, add a peak corresponding to first bar low price
            if (foundPeaks.Find(peak => !peak.FromHighPrice).BarIndex != 0)
            {
                foundPeaks.Insert(0, new Peak(
                    fromHighPrice: false,
                    peakType: (Bars.LowPrices[0] > Bars.LowPrices[1]) ? PeakType.Maximum : PeakType.Minimum,
                    datetime: Bars.OpenTimes[0],
                    barIndex: 0,
                    price: Bars.LowPrices[0],
                    sourcePeriod: period,
                    algoAPI: this));
            }

            // If there is no high price peak at the end of Bars series, add a peak corresponding to last bar high price
            if (foundPeaks.FindLast(peak => peak.FromHighPrice).BarIndex != Bars.Count - 1)
            {
                foundPeaks.Add(new Peak(
                    fromHighPrice: true,
                    peakType: (Bars.HighPrices.LastValue > Bars.HighPrices.Last(1)) ? PeakType.Maximum : PeakType.Minimum,
                    datetime: Bars.OpenTimes.LastValue,
                    barIndex: Bars.Count - 1,
                    price: Bars.HighPrices.LastValue,
                    sourcePeriod: period,
                    algoAPI: this));
            }

            // If there is no low price peak at the end of Bars series, add a peak corresponding to last bar low price
            if (foundPeaks.FindLast(peak => !peak.FromHighPrice).BarIndex != Bars.Count - 1)
            {
                foundPeaks.Add(new Peak(
                    fromHighPrice: false,
                    peakType: (Bars.LowPrices.LastValue > Bars.LowPrices.Last(1)) ? PeakType.Maximum : PeakType.Minimum,
                    datetime: Bars.OpenTimes.LastValue,
                    barIndex: Bars.Count - 1,
                    price: Bars.LowPrices.LastValue,
                    sourcePeriod: period,
                    algoAPI: this));
            }

            // Return all found peaks
            return foundPeaks;
        }

        /// <summary>
        /// Returns true if High price of the bar at centralIndex is the maximum within a given period before and after it
        /// If there are more bars with the same maximum value, only the first returns true
        /// </summary>
        /// <param name="centralIndex">Index of the checked bar</param>
        /// <param name="period">Number of bars to check before and after centralIndex</param>
        /// <returns></returns>
        public bool isHighPriceMaximum(int centralIndex, int period)
        {
            for (int i = centralIndex - period; i < centralIndex + period; i++)
            {
                if (Bars.HighPrices[centralIndex] < Bars.HighPrices[i]) return false;
            }

            for(int i = centralIndex - period; i < centralIndex; i++)
            {
                if (Bars.HighPrices[centralIndex] == Bars.HighPrices[i]) return false;
            }

            return true;
        }

        /// <summary>
        /// Returns true if High price of the bar at centralIndex is the minimum within a given period before and after it
        /// If there are more bars with the same minimum value, only the first returns true
        /// </summary>
        /// <param name="centralIndex">Index of the checked bar</param>
        /// <param name="period">Number of bars to check before and after centralIndex</param>
        /// <returns></returns>
        public bool isHighPriceMinimum(int centralIndex, int period)
        {
            // Checks if there aren't any high price values lower than value at centralIndex in the periods around it
            for (int i = centralIndex - period; i < centralIndex + period; i++)
            {
                if (Bars.HighPrices[centralIndex] > Bars.HighPrices[i]) return false;
            }

            for (int i = centralIndex - period; i < centralIndex; i++)
            {
                if (Bars.HighPrices[centralIndex] == Bars.HighPrices[i]) return false;
            }

            return true;
        }

        /// <summary>
        /// Returns true if Low price of the bar at centralIndex is the minimum within a given period before and after it
        /// If there are more bars with the same minimum value, only the first returns true
        /// </summary>
        /// <param name="centralIndex">Index of the checked bar</param>
        /// <param name="period">Number of bars to check before and after centralIndex</param>
        /// <returns></returns>
        public bool isLowPriceMinimum(int centralIndex, int period)
        {
            // Checks if there aren't any low price values lower than value at centralIndex in the periods around it
            for (int i = centralIndex - period; i < centralIndex + period; i++)
            {
                if (Bars.LowPrices[centralIndex] > Bars.LowPrices[i]) return false;
            }

            for (int i = centralIndex - period; i < centralIndex; i++)
            {
                if (Bars.LowPrices[centralIndex] == Bars.LowPrices[i]) return false;
            }

            return true;
        }

        /// <summary>
        /// Returns true if Low price of the bar at centralIndex is the maximum within a given period before and after it
        /// If there are more bars with the same maximum value, only the first returns true
        /// </summary>
        /// <param name="centralIndex">Index of the checked bar</param>
        /// <param name="period">Number of bars to check before and after centralIndex</param>
        /// <returns></returns>
        public bool isLowPriceMaximum(int centralIndex, int period)
        {
            // Checks if there aren't any low price values higher than value at centralIndex in the periods around it
            for (int i = centralIndex - period; i < centralIndex + period; i++)
            {
                if (Bars.LowPrices[centralIndex] < Bars.LowPrices[i]) return false;
            }

            for (int i = centralIndex - period; i < centralIndex; i++)
            {
                if (Bars.LowPrices[centralIndex] == Bars.LowPrices[i]) return false;
            }

            return true;
        }
        #endregion

        #region IdentifyTrends

        /// <summary>
        /// Finds all trends between peaks in a given list
        /// </summary>
        /// <param name="peaks">A list of at least two high price and at least two low price Peaks bordering the trends</param>
        /// <returns></returns>
        private List<Trend> IdentifyTrends(List<Peak> peaks)
        {
            //Stores finally merged TrendSegments
            List<Trend> trends = new List<Trend>();

            double trendTypeThresholdPerBar = (double)trendTypeThreshold / 50000;

            List<Trend> trendSegments = Trend.GetTrendSegments(this, peaks, trendTypeThresholdPerBar);

            return trendSegments;

            //TODO: What to do with short trend segemnts we have now?
        }

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