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
        //TODO: right now there is actually half of this period between peaks
        /// <summary>
        /// Determines the minimum time period between searched peaks
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

            public override string ToString()
            {

                return string.Format("{0} price {1} at index {2}, time {3}, price {4}", 
                    FromHighPrice ? "High" : "Low", PeakType, BarIndex, DateTime, Price);
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
            //TODO: Comment and structuralize the type, organise its function against IdentifyTrends function
            public TrendType HighPriceTrendType;
            public TrendType LowPriceTrendType;

            public Peak HighStartPeak;
            public Peak LowStartPeak;
            public Peak HighEndPeak;
            public Peak LowEndPeak;

            public int LengthInBars;

            public double Height;

            public int SourcePeriod;

            public double Gradient;

            public double Intensity;

            public Trend(Peak highStartPeak, Peak lowStartPeak, Peak highEndPeak, Peak lowEndPeak, double trendHeightThreshold)
            {
                HighStartPeak = highStartPeak;
                LowStartPeak = lowStartPeak;
                HighEndPeak = highEndPeak;
                LowEndPeak = lowEndPeak;

                GetTrendType(trendHeightThreshold);
            }

            public Trend(TrendType highPriceTrendType, TrendType lowPriceTrendType, 
                Peak highStartPeak, Peak lowStartPeak, Peak highEndPeak, Peak lowEndPeak, 
                int sourcePeriod, double intensity = 1)
            {
                HighPriceTrendType = highPriceTrendType;
                LowPriceTrendType = lowPriceTrendType;
                HighStartPeak = highStartPeak;
                LowStartPeak = lowStartPeak;
                HighEndPeak = highEndPeak;
                LowEndPeak = lowEndPeak;
                SourcePeriod = sourcePeriod;
                Intensity = intensity;
            }

            public override string ToString()
            {
                return string.Format("HP {0}, LP {1}, start at index HP {2}, LP {3}, end at HP {4}, LP {5}",
                    HighPriceTrendType, LowPriceTrendType, HighStartPeak.BarIndex, LowStartPeak.BarIndex, HighEndPeak.BarIndex, LowEndPeak.BarIndex);
            }

            private void GetTrendType(double trendHeightThreshold)
            {
                HighPriceTrendType = GetTrendTypeBetweenPeaks(HighStartPeak, HighEndPeak, trendHeightThreshold);
                LowPriceTrendType = GetTrendTypeBetweenPeaks(LowStartPeak, LowEndPeak, trendHeightThreshold);
            }

            private TrendType GetTrendTypeBetweenPeaks(Peak start, Peak end, double threshold)
            {
                if (end.Price - start.Price > threshold)
                {
                    return TrendType.Uptrend;
                }
                if (end.Price - start.Price < threshold * -1)
                {
                    return TrendType.Downtrend;
                }
                return TrendType.Consolidation;
            }

            public bool HasSameTrendType(Trend other)
            {
                return HighPriceTrendType == other.HighPriceTrendType && LowPriceTrendType == other.LowPriceTrendType;
            }

            //TODO: behavior related to other properties
            public void CombineWithFollowingTrend(Trend followingTrend)
            {
                bool trendFollows = (HighEndPeak == followingTrend.HighStartPeak && LowEndPeak == followingTrend.LowStartPeak) ||
                    (HighEndPeak == followingTrend.HighStartPeak && LowEndPeak == followingTrend.LowEndPeak) ||
                    (LowEndPeak == followingTrend.LowStartPeak && HighEndPeak == followingTrend.HighEndPeak);
                if(!trendFollows)
                {
                    throw new ArgumentException(this.ToString() + followingTrend.ToString());
                }
                HighEndPeak = followingTrend.HighEndPeak;
                LowEndPeak = followingTrend.LowEndPeak;
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

            BaseLines.AddRange(IdentifyLines(PeakSearchPeriod));

            //Will be used to get multiple landscape layers with different line id periods
            /*foreach(int period in Periods)
            {
                BaseLines.AddRange(IdentifyLines(period, trendTypeThreshold));
            }*/
        }

        #region IdentifyLines

        private List<BaseLine> IdentifyLines(int period)
        {
            //Stores all found baseLines
            List<BaseLine> BaseLines = new List<BaseLine>();

            //Stores all peaks with a given period
            List<Peak> Peaks;

            //Find all peaks with a given period
            Peaks = IdentifyPeaks(period);

            //Find all trends corresponding to those peaks
            List<Trend> Trends = IdentifyTrends(Peaks);

            //Visualize found trends and peaks
            VisualizePeaks(Peaks);
            VisualizeTrendContours(Trends);

            //After finishing the logic
            return BaseLines;
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
            List<Trend> Trends = new List<Trend>();

            List<Trend> TrendSegments = GetTrendSegments(peaks);
            //return TrendSegments;
            if(TrendSegments.Count == 1)
            {
                return TrendSegments;
            }

            //Combine the short trends if they have the same type
            Trend CurrentTrend = TrendSegments[0];
            TrendSegments.RemoveAt(0);

            foreach(Trend trend in TrendSegments)
            {
                if(CurrentTrend.HasSameTrendType(trend))
                {
                    CurrentTrend.CombineWithFollowingTrend(trend);
                }
                else
                {
                    Trends.Add(CurrentTrend);
                    CurrentTrend = trend;
                }
            }

            //Add the last checked trend
            Trends.Add(CurrentTrend);

            //After finishing the logic
            return Trends;
        }

        private List<Trend> GetTrendSegments(List<Peak> peaks)
        {
            List<Trend> TrendSegments = new List<Trend>();

            double Threshold = trendIdThresholdPips * Symbol.PipSize;

            Peak HighStartPeak;
            Peak LowStartPeak;
            Peak HighEndPeak;
            Peak LowEndPeak;

            List<Peak> HighPeaks = peaks.FindAll(peak => peak.FromHighPrice);
            List<Peak> LowPeaks = peaks.FindAll(peak => !peak.FromHighPrice);

            HighStartPeak = HighPeaks[0];
            HighEndPeak = HighPeaks[1];
            LowStartPeak = LowPeaks[0];
            LowEndPeak = LowPeaks[1];

            HighPeaks.RemoveRange(0, 2);
            LowPeaks.RemoveRange(0, 2);

            TrendSegments.Add(new Trend(HighStartPeak, LowStartPeak, HighEndPeak, LowEndPeak, Threshold));

            while (HighPeaks.Count > 0 && LowPeaks.Count > 0)
            {

                if(HighEndPeak.BarIndex < LowEndPeak.BarIndex)
                {
                    HighStartPeak = HighEndPeak;
                    HighEndPeak = HighPeaks[0];
                    HighPeaks.RemoveAt(0);
                }
                else if(HighEndPeak.BarIndex > LowEndPeak.BarIndex)
                {
                    LowStartPeak = LowEndPeak;
                    LowEndPeak = LowPeaks[0];
                    LowPeaks.RemoveAt(0);
                }
                else
                {
                    HighStartPeak = HighEndPeak;
                    HighEndPeak = HighPeaks[0];
                    HighPeaks.RemoveAt(0);
                    continue;
                }

                TrendSegments.Add(new Trend(HighStartPeak, LowStartPeak, HighEndPeak, LowEndPeak, Threshold));
            }

            while(HighPeaks.Count > 0)
            {
                HighStartPeak = HighEndPeak;
                HighEndPeak = HighPeaks[0];
                HighPeaks.RemoveAt(0);
                TrendSegments.Add(new Trend(HighStartPeak, LowStartPeak, HighEndPeak, LowEndPeak, Threshold));
            }
            while (LowPeaks.Count > 0)
            {
                LowStartPeak = LowEndPeak;
                LowEndPeak = LowPeaks[0];
                LowPeaks.RemoveAt(0);
                TrendSegments.Add(new Trend(HighStartPeak, LowStartPeak, HighEndPeak, LowEndPeak, Threshold));
            }

            return TrendSegments;
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
                FoundPeaks.Add(new Peak(
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
                FoundPeaks.Add(new Peak(
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
                string name = peak.DateTime.ToString() + (peak.FromHighPrice ? "_high" : "_low") + "_peak";
                //Draw the peak on the chart
                Chart.DrawIcon(name, ChartIconType.Circle, peak.DateTime, peak.Price, peakColor);
                Print(peak);
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

        /// <summary>
        /// Draws the contours of the trends on the active chart as colored lines
        /// </summary>
        /// <param name="trends">List of trends to be shown</param>
        private void VisualizeTrendContours(List<Trend> trends)
        {
            foreach(Trend trend in trends)
            {
                Color HighPriceColor = GetTrendLineColor(trend.HighPriceTrendType);
                Color LowPriceColor = GetTrendLineColor(trend.LowPriceTrendType);


                DrawLineBetweenPeaks(trend.HighStartPeak, trend.HighEndPeak, HighPriceColor);
                DrawLineBetweenPeaks(trend.LowStartPeak, trend.LowEndPeak, LowPriceColor);

                Print(trend);
            }
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

        private void DrawLineBetweenPeaks(Peak startPeak, Peak endPeak, Color color)
        {
            string name = string.Format("{0}_{1}_to_{2}_{3}_trend", startPeak.DateTime, startPeak.Price, endPeak.DateTime, endPeak.Price);
            Chart.DrawTrendLine(name, startPeak.DateTime, startPeak.Price, endPeak.DateTime, endPeak.Price, color);
        }
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