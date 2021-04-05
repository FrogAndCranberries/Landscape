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
    /// <summary>
    /// Represents a price peak.
    /// </summary>
    class Peak
    {
        // Whether the peak came from high price
        public bool FromHighPrice;

        // Whether the peak is a maximum or a minimum
        public PeakType PeakType;

        // The date and time (x-coordinate) of the peak
        public DateTime DateTime;

        // The index of the bar with the peak in Bars
        public int BarIndex;

        // The price (y-coordinate) of the peak
        public double Price;

        // The search period at which was the peak found
        public int SourcePeriod;

        // How important the peak is
        public double Intensity;

        // Access to the Algo API
        private Algo AlgoAPI;

        // General constructor that initializes all fields
        public Peak(bool fromHighPrice, PeakType peakType, DateTime datetime, int barIndex, double price, int sourcePeriod, Algo algoAPI, double intensity = 1)
        {
            //Initialize all fields
            FromHighPrice = fromHighPrice;

            PeakType = peakType;

            DateTime = datetime;

            BarIndex = barIndex;

            Price = price;

            SourcePeriod = sourcePeriod;

            Intensity = intensity;

            AlgoAPI = algoAPI;
        }

        // Override of the ToString method returning a representation useful in logs
        public override string ToString()
        {
            return string.Format("{0} price {1} at index {2}, time {3}, price {4}",
                FromHighPrice ? "High" : "Low", PeakType, BarIndex, DateTime, Price);
        }

        /// <summary>
        /// Draws the peak on the active chart as colored dots
        /// </summary>
        public void Visualize()
        {
            //Determine the color of the peak
            Color peakColor = GetPeakColor();
            //The peak has a unique name in the format DateTime_high or DateTime_low
            string name = DateTime.ToString() + (FromHighPrice ? "_high" : "_low") + "_peak";
            //Draw the peak on the chart
            AlgoAPI.Chart.DrawIcon(name, ChartIconType.Circle, DateTime, Price, peakColor);
            AlgoAPI.Print(ToString());
        }

        /// <summary>
        /// Determines the color of a dot visualising the peak
        /// </summary>
        /// <returns>Color of the peak</returns>
        private Color GetPeakColor()
        {
            if (FromHighPrice)
            {
                switch (PeakType)
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
                switch (PeakType)
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
    }
}
