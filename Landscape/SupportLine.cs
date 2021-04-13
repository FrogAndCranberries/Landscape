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
    class SupportLine : ResistanceLine
    {
        public double Price;

        public int StartIndex;
        public DateTime StartTime;

        public SupportLine(double price, double intensity, int startIndex, DateTime startTime)
        {
            Price = price;
            Intensity = intensity;
            StartIndex = startIndex;
            StartTime = startTime;
        }

        public override double IntensityAtBar(int barIndex)
        {
            if (barIndex < StartIndex) return 0;

            double intensityDecayConstant = 0.005;

            return Intensity * Math.Pow(10, -intensityDecayConstant * (barIndex - StartIndex));
        }

        public override void Visualize(Chart chart)
        {
            string name = Guid.NewGuid().ToString();

            chart.DrawHorizontalLine(name, Price, GetColor(), 2);
            chart.DrawIcon(name + "start", ChartIconType.Diamond, StartTime, Price, GetColor());
        }

        private Color GetColor()
        {
            double middleIntensityConstant = 40;
            double steepnessConstant = 0.05;

            double shift = 255 / (1 + Math.Pow(2, -steepnessConstant * (Intensity - middleIntensityConstant)));
            int blue = 255 - (int)shift;
            int red = (int)shift;
            return Color.FromArgb(200, red, 30, blue);
        }
    }
}
