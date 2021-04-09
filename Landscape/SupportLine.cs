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
    class SupportLine : IResistanceLine
    {
        public double Price;

        public int StartIndex;
        public DateTime StartTime;

        public Color Color;

        public SupportLine(double price, int startIndex, DateTime startTime, Color color)
        {
            Price = price;
            StartIndex = startIndex;
            StartTime = startTime;
            Color = color;
        }

        public void Visualize(Chart chart)
        {
            string name = Guid.NewGuid().ToString();

            chart.DrawHorizontalLine(name, Price, Color);
            chart.DrawIcon(name + "start", ChartIconType.Diamond, StartTime, Price, Color);
        }
    }
}
