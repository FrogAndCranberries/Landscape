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
        /// Determines the minimum time period between searched peaks
        /// </summary>
        [Parameter(DefaultValue = 10, MinValue = 1)]
        public int PeakSearchPeriod { get; set; }

        /// <summary>
        /// Determines a threshold for minimal gradient of a trend if that will be needed. Might have to be landcape-layer specific
        /// </summary>
        [Parameter(DefaultValue = 10, MinValue = 1)]
        public int trendTypeThreshold { get; set; }
        
        #endregion

        #region Variables
        /// <summary>
        /// Will determine the peakSearchPeriod for each timeframe layer of landscape creation. Values unknown yet
        /// </summary>
        List<int> Periods = new List<int>() { 5, 10, 20, 30, 50 };


        #endregion

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

        #region Class definitions

        /// <summary>
        /// Represents if a peak is a maximum or minimum.
        /// </summary>
        private enum PeakType
        {
            Maximum,
            Minimum
        }

        /// <summary>
        /// Represents a price peak.
        /// </summary>
        private class Peak
        {
            public bool FromHighPrice;

            public PeakType PeakType;

            public DateTime DateTime;

            public int BarIndex;

            public double Price;

            public int SourcePeriod;

            public double Intensity;

            public Peak(bool fromHighPrice, PeakType peakType, DateTime datetime, int barIndex, double price, int sourcePeriod, double intensity = 1)
            {
                FromHighPrice  = fromHighPrice;

                PeakType = peakType;

                DateTime = datetime;

                BarIndex = barIndex;

                Price = price;

                SourcePeriod = sourcePeriod;

                Intensity = intensity;
            }
        }

        /// <summary>
        /// Represents if trend is an uptrend, consolidation or downtrend
        /// </summary>
        private enum TrendType
        {
            Uptrend,
            Consolidation,
            Downtrend
        }

        /// <summary>
        /// Represents a price trend
        /// </summary>
        private class Trend
        {
            public TrendType TrendType;

            public Peak StartPeak;

            public Peak EndPeak;

            public int LengthInBars;

            public double Height;

            public int SourcePeriod;

            public double Gradient;

            public double Intensity;

            public Trend(Peak startPeak, int sourcePeriod)
            {
                StartPeak = startPeak;
                SourcePeriod = sourcePeriod;
            }

            public Trend(TrendType trendType, Peak startPeak, Peak endPeak, int sourcePeriod, double intensity = 1)
            {
                TrendType = trendType;
                StartPeak = startPeak;
                EndPeak = endPeak;
                SourcePeriod = sourcePeriod;
                Intensity = intensity;

            }

            public void Complete(Peak endPeak)
            {

            }
        }

        private class BaseLine
        {

        }

        #endregion

        #region Methods
        private void CreateLandscape()
        {
            List<BaseLine> BaseLines = new List<BaseLine>();

            foreach(int period in Periods)
            {
                BaseLines.AddRange(IdentifyLines(period, trendTypeThreshold));
            }
        }

        #region IdentifyLines

        private List<BaseLine> IdentifyLines(int period, int threshold)
        {
            // Stores all found baseLines
            List<BaseLine> BaseLines = new List<BaseLine>();

            // Get all peaks with given period
            List<Peak> Peaks = IdentifyPeaks(period);

            // Get all trends corresponding to those peaks
            List<Trend> Trends = IdentifyTrends(Peaks, threshold);


            //After finishing the logic
            return BaseLines;
        }

        private List<Trend> IdentifyTrends(List<Peak> peaks, int threshold)
        {
            // Stores found trends
            List<Trend> Trends = new List<Trend>();

            foreach(Peak peak in peaks)
            {

            }


            //After finishing the logic
            return Trends;
        }
        #endregion

        //Functional region
        #region IdentifyPeaks

        /// <summary>
        /// Finds all maxima and minima of high and low price with given period in Bars. 
        /// </summary>
        /// <param name="period">Minimal period the peaks must have before and after them</param>
        /// <returns>List of all found peaks</returns>
        private List<Peak> IdentifyPeaks(int period)
        {
            // Stores found peaks
            List<Peak> FoundPeaks = new List<Peak>();

            // For each index in Bars
            for (int index = 0; index < Bars.Count; index++)
            {
                // Check if the bar at index is a maximum or minimum of high price in the specified period
                // If so, it adds a new peak corresponding to it to found peaks
                if (isHighPriceMaximum(index, period))
                {
                    FoundPeaks.Add(new Peak(
                        fromHighPrice: true,
                        peakType: PeakType.Maximum, 
                        datetime: Bars.OpenTimes[index], 
                        barIndex: index, 
                        price: Bars.HighPrices[index], 
                        sourcePeriod: period));
                }
                else if (isHighPriceMinimum(index, period))
                {
                    FoundPeaks.Add(new Peak(
                        fromHighPrice: true,
                        peakType: PeakType.Minimum,
                        datetime: Bars.OpenTimes[index],
                        barIndex: index,
                        price: Bars.HighPrices[index],
                        sourcePeriod: period));
                }

                // Check if the bar at index is a maximum or minimum of low price in the specified period
                // If so, it adds a new peak corresponding to it to found peaks
                if (isLowPriceMaximum(index, period))
                {
                    FoundPeaks.Add(new Peak(
                        fromHighPrice: false,
                        peakType: PeakType.Maximum,
                        datetime: Bars.OpenTimes[index],
                        barIndex: index,
                        price: Bars.LowPrices[index],
                        sourcePeriod: period));
                }
                else if (isLowPriceMinimum(index, period))
                {
                    FoundPeaks.Add(new Peak(
                        fromHighPrice: false,
                        peakType: PeakType.Minimum,
                        datetime: Bars.OpenTimes[index],
                        barIndex: index,
                        price: Bars.LowPrices[index],
                        sourcePeriod: period));
                }
            }

            // For trend search, there should be a high and low price peak at first and last bar
            // If there is no high price peak at the beginning of Bars series, add a peak corresponding to first bar high price
            if(FoundPeaks.Find(peak => peak.FromHighPrice).BarIndex != 0)
            {
                FoundPeaks.Insert(0, new Peak(
                    fromHighPrice: true,
                    peakType: (Bars.HighPrices[0] > Bars.HighPrices[1]) ? PeakType.Maximum : PeakType.Minimum,
                    datetime: Bars.OpenTimes[0],
                    barIndex: 0,
                    price: Bars.HighPrices[0],
                    sourcePeriod: period));
            }

            // If there is no low price peak at the beginning of Bars series, add a peak corresponding to first bar low price
            if (FoundPeaks.Find(peak => !peak.FromHighPrice).BarIndex != 0)
            {
                FoundPeaks.Insert(0, new Peak(
                    fromHighPrice: false,
                    peakType: (Bars.LowPrices[0] > Bars.LowPrices[1]) ? PeakType.Maximum : PeakType.Minimum,
                    datetime: Bars.OpenTimes[0],
                    barIndex: 0,
                    price: Bars.LowPrices[0],
                    sourcePeriod: period));
            }

            // If there is no high price peak at the end of Bars series, add a peak corresponding to last bar high price
            if (FoundPeaks.FindLast(peak => peak.FromHighPrice).BarIndex != Bars.Count - 1)
            {
                FoundPeaks.Insert(0, new Peak(
                    fromHighPrice: true,
                    peakType: (Bars.HighPrices.LastValue > Bars.HighPrices.Last(1)) ? PeakType.Maximum : PeakType.Minimum,
                    datetime: Bars.OpenTimes.LastValue,
                    barIndex: Bars.Count - 1,
                    price: Bars.HighPrices.LastValue,
                    sourcePeriod: period));
            }

            // If there is no low price peak at the end of Bars series, add a peak corresponding to last bar low price
            if (FoundPeaks.FindLast(peak => !peak.FromHighPrice).BarIndex != Bars.Count - 1)
            {
                FoundPeaks.Insert(0, new Peak(
                    fromHighPrice: false,
                    peakType: (Bars.LowPrices.LastValue > Bars.LowPrices.Last(1)) ? PeakType.Maximum : PeakType.Minimum,
                    datetime: Bars.OpenTimes.LastValue,
                    barIndex: Bars.Count - 1,
                    price: Bars.LowPrices.LastValue,
                    sourcePeriod: period));
            }

            // Return all found peaks
            return FoundPeaks;
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

        #region Visualization

        //Functional region
        #region VisualizePeaks

        /// <summary>
        /// Draws all peaks in the list on the active chart as colored dots
        /// </summary>
        /// <param name="peaks">List of peaks to be shown</param>
        private void VisualizePeaks(List<Peak> peaks)
        {
            foreach(Peak peak in peaks)
            {
                //Determine the color of the peak
                Color peakColor = GetPeakColor(peak);
                //The peak has a unique name in the format DateTime_high or DateTime_low
                string name = peak.DateTime.ToString() + (peak.FromHighPrice ? "_high" : "_low");
                //Draw the peak on the chart
                Chart.DrawIcon(name, ChartIconType.Circle, peak.DateTime, peak.Price, peakColor);
            }
        }

        /// <summary>
        /// Determines the color of a dot visualising a given peak
        /// </summary>
        /// <param name="peak">Peak whose color is determined</param>
        /// <returns></returns>
        private Color GetPeakColor(Peak peak)
        {
            if (peak.FromHighPrice)
            {
                switch (peak.PeakType)
                {
                    case PeakType.Maximum:
                        //High price maximum is green
                        return Color.Green;
                    default:
                        //High price minimum is yellow
                        return Color.Yellow;
                }
            }
            else
            {
                switch (peak.PeakType)
                {
                    case PeakType.Maximum:
                        //Low price maximum is orange
                        return Color.Orange;
                    default:
                        //Low price minimum is red
                        return Color.Red;
                }
            }
            
        }

        #endregion

        #region VisualizeTrends

        #endregion

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