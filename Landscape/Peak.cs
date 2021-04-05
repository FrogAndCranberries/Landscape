using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        // General constructor that initializes all fields
        public Peak(bool fromHighPrice, PeakType peakType, DateTime datetime, int barIndex, double price, int sourcePeriod, double intensity = 1)
        {
            //Initialize all fields
            FromHighPrice = fromHighPrice;

            PeakType = peakType;

            DateTime = datetime;

            BarIndex = barIndex;

            Price = price;

            SourcePeriod = sourcePeriod;

            Intensity = intensity;
        }

        // Override of the ToString method returning a representation useful in logs
        public override string ToString()
        {
            return string.Format("{0} price {1} at index {2}, time {3}, price {4}",
                FromHighPrice ? "High" : "Low", PeakType, BarIndex, DateTime, Price);
        }
    }
}
