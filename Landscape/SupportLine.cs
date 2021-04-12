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

        public Color Color;

        public SupportLine(double price, double intensity, int startIndex, DateTime startTime, Color color)
        {
            Price = price;
            Intensity = intensity;
            StartIndex = startIndex;
            StartTime = startTime;
            Color = color;
        }

        public override double IntensityAtBar(int barIndex)
        {
            if (barIndex < StartIndex) return 0;

            double intensityDecayConstant = 0.01;

            return Intensity * Math.Pow(10, -intensityDecayConstant * (barIndex - StartIndex));
        }

        public override void Visualize(Chart chart)
        {

            string name = Guid.NewGuid().ToString();

            int thickness = Math.Max((int)Intensity / 30, 1); 

            chart.DrawHorizontalLine(name, Price, Color, thickness);
            chart.DrawIcon(name + "start", ChartIconType.Diamond, StartTime, Price, Color);
        }
    }
}
