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
        public bool FromHighPrice { get; }

        public PeakType PeakType { get; }

        public DateTime DateTime { get; }

        public int BarIndex { get; }

        public double Price { get; }

        public int SourcePeriod { get; }

        public double Intensity { get; private set; }

        // General constructor that initializes all fields
        public Peak(bool fromHighPrice, PeakType peakType, DateTime datetime, int barIndex, double price, int sourcePeriod)
        {
            FromHighPrice = fromHighPrice;

            PeakType = peakType;

            DateTime = datetime;

            BarIndex = barIndex;

            Price = price;

            SourcePeriod = sourcePeriod;

            CalculateIntensity();
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
        public void Visualize(Chart chart)
        {
            Color peakColor = GetPeakColor();

            string name = Guid.NewGuid().ToString();

            chart.DrawIcon(name, ChartIconType.Circle, DateTime, Price, peakColor);
        }

        /// <summary>
        /// Determines the color of a dot visualising the peak
        /// </summary>
        /// <returns>Color of the peak</returns>
        private Color GetPeakColor()
        {
            if (FromHighPrice)
            {
                return (PeakType == PeakType.Maximum) ? Color.Green : Color.Yellow;
            }
            return (PeakType == PeakType.Maximum) ? Color.Orange : Color.Red;
        }

        private void CalculateIntensity()
        {
            Intensity = 1;
        }
    }
}
