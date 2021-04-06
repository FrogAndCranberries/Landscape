using System;
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
        /// Determines a threshold for minimal gradient of a trend if that will be needed. Might have to be landcape-layer specific
        /// </summary>
        [Parameter(DefaultValue = 5, MinValue = 0)]
        public int trendIdThresholdPips { get; set; }
        
        #endregion

        #region Variables
        /// <summary>
        /// Will determine the peakSearchPeriod for each timeframe layer of landscape creation. Values unknown yet
        /// </summary>
        List<int> Periods = new List<int>() {5, 10, 20};

        #endregion

        #region Core fuctions
        protected override void OnStart()
        {
            CreateLandscape();
            CreateConditions();
        }

        protected override void OnTick()
        {
            CheckConditions();
        }

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

            VisualizePeaks(peaks);
            //VisualizeTrendsContours(trends);
            VisualizeResistanceLines(resistanceLines);

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

            foreach(Trend trend in trends)
            {
                resistanceLines.Add(trend.GetHighTrendLine());
                resistanceLines.Add(trend.GetLowTrendLine());
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
        /// Returns true if High price of bar at centralIndex is the maximum within given period before and after it
        /// </summary>
        /// <param name="centralIndex"></param>
        /// <param name="period">Period to check before and after centralIndex</param>
        /// <returns></returns>
        public bool isHighPriceMaximum(int centralIndex, int period)
        {

            // Checks if there aren't any high price values higher than value at centralIndex in the period around it
            for (int i = centralIndex - period; i < centralIndex + period; i++)
            {
                if (Bars.HighPrices[centralIndex] < Bars.HighPrices[i]) return false;
            }
            return true;
        }

        /// <summary>
        /// Returns true if High Price of bar at centralIndex is the minimum within given period before and after it
        /// </summary>
        /// <param name="centralIndex"></param>
        /// <param name="period">Period to check before and after centralIndex</param>
        /// <returns></returns>
        public bool isHighPriceMinimum(int centralIndex, int period)
        {
            // Checks if there aren't any high price values lower than value at centralIndex in the periods around it
            for (int i = centralIndex - period; i < centralIndex + period; i++)
            {
                if (Bars.HighPrices[centralIndex] > Bars.HighPrices[i]) return false;
            }
            return true;
        }

        /// <summary>
        /// Returns true if Low Price of bar at centralIndex is the minimum within given period before and after it
        /// </summary>
        /// <param name="centralIndex"></param>
        /// <param name="period">Period to check before and after centralIndex</param>
        /// <returns></returns>
        public bool isLowPriceMinimum(int centralIndex, int period)
        {
            // Checks if there aren't any low price values lower than value at centralIndex in the periods around it
            for (int i = centralIndex - period; i < centralIndex + period; i++)
            {
                if (Bars.LowPrices[centralIndex] > Bars.LowPrices[i]) return false;
            }
            return true;
        }

        /// <summary>
        /// Returns true if Low Price of bar at centralIndex is the maximum within given period before and after it
        /// </summary>
        /// <param name="centralIndex"></param>
        /// <param name="period">Period to check before and after centralIndex</param>
        /// <returns></returns>
        public bool isLowPriceMaximum(int centralIndex, int period)
        {
            // Checks if there aren't any low price values higher than value at centralIndex in the periods around it
            for (int i = centralIndex - period; i < centralIndex + period; i++)
            {
                if (Bars.LowPrices[centralIndex] < Bars.LowPrices[i]) return false;
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

            List<Trend> trendSegments = GetTrendSegments(peaks);

            return trendSegments;

            //TODO: What to do with short trend segemnts we have now?
            /*
            if(TrendSegments.Count == 1)
            {
                return TrendSegments;
            }

            Trends = MergeTrendSegments(TrendSegments);

            //After finishing the logic
            return Trends;
            */
        }

        private List<Trend> MergeTrendSegments(List<Trend> trendSegments)
        {
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

        /// <summary>
        /// Finds all shortest possible trends between peaks in a given list
        /// </summary>
        /// <param name="peaks">List of peaks bordering the serched trends</param>
        /// <returns>List of all found trends</returns>
        private List<Trend> GetTrendSegments(List<Peak> peaks)
        {
            // Store found short trends
            List<Trend> trendSegments = new List<Trend>();

            // Calculate the price threshold for trendType identification
            // TODO: obsolete soon
            double threshold = trendIdThresholdPips * Symbol.PipSize;

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
            trendSegments.Add(new Trend(highStartPeak, lowStartPeak, highEndPeak, lowEndPeak, threshold, this));

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
                trendSegments.Add(new Trend(highStartPeak, lowStartPeak, highEndPeak, lowEndPeak, threshold, this));
            }

            // If there are high-price peaks left, move the high-component over them and save the last trends
            while(highPeaks.Count > 0)
            {
                highStartPeak = highEndPeak;
                highEndPeak = highPeaks[0];
                highPeaks.RemoveAt(0);
                trendSegments.Add(new Trend(highStartPeak, lowStartPeak, highEndPeak, lowEndPeak, threshold, this));
            }

            // If there are low-price peaks left, move the low-component over them and save the last trends
            while (lowPeaks.Count > 0)
            {
                lowStartPeak = lowEndPeak;
                lowEndPeak = lowPeaks[0];
                lowPeaks.RemoveAt(0);
                trendSegments.Add(new Trend(highStartPeak, lowStartPeak, highEndPeak, lowEndPeak, threshold, this));
            }

            // Return all found trends
            return trendSegments;
        }

        #endregion

        private void VisualizePeaks(List<Peak> peaks)
        {
            foreach(Peak peak in peaks)
            {
                peak.Visualize();
            }
        }

        private void VisualizeTrendsContours(List<Trend> trends)
        {
            foreach(Trend trend in trends)
            {
                trend.VisualizeContours();
            }
        }

        private void VisualizeResistanceLines(List<IResistanceLine> resistanceLines)
        {
            foreach(IResistanceLine resistanceLine in resistanceLines)
            {
                resistanceLine.Visualize(Chart);
                Print((resistanceLine as TrendLine).SlopeConstant);
            }
        }


        private void CreateConditions()
        {

        }

        private void CheckConditions()
        {

        }

        #endregion
    }
}