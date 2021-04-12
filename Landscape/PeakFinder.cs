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
    class PeakFinder
    {

        private Algo AlgoAPI;

        public PeakFinder(Algo algoAPI)
        {
            AlgoAPI = algoAPI;
        }

        /// <summary>
        /// Finds all maxima and minima of high and low price with given period in bars. 
        /// </summary>
        /// <param name="period">Minimal period the peaks must have before and after them</param>
        /// <returns>List of all found peaks</returns>
        public List<Peak> FindPeaks(int period)
        {
            // Stores found peaks
            List<Peak> foundPeaks = new List<Peak>();

            Bars bars = AlgoAPI.Bars;

            // TODO: split new peak creation and foundPeaks.add
            // For each bar
            for (int index = 0; index < bars.Count; index++)
            {
                // Check if the bar at index is a maximum or minimum of high price in the specified period
                // If so, it adds a new peak corresponding to it to found peaks
                if (isHighPriceMaximum(index, period))
                {
                    foundPeaks.Add(new Peak(
                        fromHighPrice: true,
                        peakType: PeakType.Maximum,
                        datetime: bars.OpenTimes[index],
                        barIndex: index,
                        price: bars.HighPrices[index],
                        sourcePeriod: period));
                }
                else if (isHighPriceMinimum(index, period))
                {
                    foundPeaks.Add(new Peak(
                        fromHighPrice: true,
                        peakType: PeakType.Minimum,
                        datetime: bars.OpenTimes[index],
                        barIndex: index,
                        price: bars.HighPrices[index],
                        sourcePeriod: period));
                }

                // Check if the bar at index is a maximum or minimum of low price in the specified period
                // If so, it adds a new peak corresponding to it to found peaks
                if (isLowPriceMaximum(index, period))
                {
                    foundPeaks.Add(new Peak(
                        fromHighPrice: false,
                        peakType: PeakType.Maximum,
                        datetime: bars.OpenTimes[index],
                        barIndex: index,
                        price: bars.LowPrices[index],
                        sourcePeriod: period));
                }
                else if (isLowPriceMinimum(index, period))
                {
                    foundPeaks.Add(new Peak(
                        fromHighPrice: false,
                        peakType: PeakType.Minimum,
                        datetime: bars.OpenTimes[index],
                        barIndex: index,
                        price: bars.LowPrices[index],
                        sourcePeriod: period));
                }
            }

            // For trend search, there should be a high and low price peak at first and last bar
            // If there is no high price peak at the beginning of bars series, add a peak corresponding to first bar high price
            if (foundPeaks.Find(peak => peak.FromHighPrice).BarIndex != 0)
            {
                foundPeaks.Insert(0, new Peak(
                    fromHighPrice: true,
                    peakType: (bars.HighPrices[0] > bars.HighPrices[1]) ? PeakType.Maximum : PeakType.Minimum,
                    datetime: bars.OpenTimes[0],
                    barIndex: 0,
                    price: bars.HighPrices[0],
                    sourcePeriod: period));
            }

            // If there is no low price peak at the beginning of bars series, add a peak corresponding to first bar low price
            if (foundPeaks.Find(peak => !peak.FromHighPrice).BarIndex != 0)
            {
                foundPeaks.Insert(0, new Peak(
                    fromHighPrice: false,
                    peakType: (bars.LowPrices[0] > bars.LowPrices[1]) ? PeakType.Maximum : PeakType.Minimum,
                    datetime: bars.OpenTimes[0],
                    barIndex: 0,
                    price: bars.LowPrices[0],
                    sourcePeriod: period));
            }

            // If there is no high price peak at the end of bars series, add a peak corresponding to last bar high price
            if (foundPeaks.FindLast(peak => peak.FromHighPrice).BarIndex != bars.Count - 1)
            {
                foundPeaks.Add(new Peak(
                    fromHighPrice: true,
                    peakType: (bars.HighPrices.LastValue > bars.HighPrices.Last(1)) ? PeakType.Maximum : PeakType.Minimum,
                    datetime: bars.OpenTimes.LastValue,
                    barIndex: bars.Count - 1,
                    price: bars.HighPrices.LastValue,
                    sourcePeriod: period));
            }

            // If there is no low price peak at the end of bars series, add a peak corresponding to last bar low price
            if (foundPeaks.FindLast(peak => !peak.FromHighPrice).BarIndex != bars.Count - 1)
            {
                foundPeaks.Add(new Peak(
                    fromHighPrice: false,
                    peakType: (bars.LowPrices.LastValue > bars.LowPrices.Last(1)) ? PeakType.Maximum : PeakType.Minimum,
                    datetime: bars.OpenTimes.LastValue,
                    barIndex: bars.Count - 1,
                    price: bars.LowPrices.LastValue,
                    sourcePeriod: period));
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
        private bool isHighPriceMaximum(int centralIndex, int period)
        {
            Bars bars = AlgoAPI.Bars;

            for (int i = centralIndex - period; i < centralIndex + period; i++)
            {
                if (bars.HighPrices[centralIndex] < bars.HighPrices[i]) return false;
            }

            for (int i = centralIndex - period; i < centralIndex; i++)
            {
                if (bars.HighPrices[centralIndex] == bars.HighPrices[i]) return false;
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
        private bool isHighPriceMinimum(int centralIndex, int period)
        {
            Bars bars = AlgoAPI.Bars;

            for (int i = centralIndex - period; i < centralIndex + period; i++)
            {
                if (bars.HighPrices[centralIndex] > bars.HighPrices[i]) return false;
            }

            for (int i = centralIndex - period; i < centralIndex; i++)
            {
                if (bars.HighPrices[centralIndex] == bars.HighPrices[i]) return false;
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
        private bool isLowPriceMinimum(int centralIndex, int period)
        {
            Bars bars = AlgoAPI.Bars;

            for (int i = centralIndex - period; i < centralIndex + period; i++)
            {
                if (bars.LowPrices[centralIndex] > bars.LowPrices[i]) return false;
            }

            for (int i = centralIndex - period; i < centralIndex; i++)
            {
                if (bars.LowPrices[centralIndex] == bars.LowPrices[i]) return false;
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
        private bool isLowPriceMaximum(int centralIndex, int period)
        {
            Bars bars = AlgoAPI.Bars;

            for (int i = centralIndex - period; i < centralIndex + period; i++)
            {
                if (bars.LowPrices[centralIndex] < bars.LowPrices[i]) return false;
            }

            for (int i = centralIndex - period; i < centralIndex; i++)
            {
                if (bars.LowPrices[centralIndex] == bars.LowPrices[i]) return false;
            }

            return true;
        }
    }
}
