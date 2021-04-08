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
    class TrendLine : IResistanceLine
    {
        // price = k*index + q
        public double SlopeConstant;
        public double IntersectionConstant;

        public DateTime CoreStart;
        public DateTime CoreEnd;

        Color Color;

        public TrendLine(double slopeConstant, double intersectionConstant, DateTime coreStart, DateTime coreEnd, Color color)
        {
            SlopeConstant = slopeConstant;
            IntersectionConstant = intersectionConstant;
            CoreStart = coreStart;
            CoreEnd = coreEnd;
            Color = color;
        }

        public void Visualize(Chart chart)
        {
            string name = Guid.NewGuid().ToString();

            double coreStartPrice = SlopeConstant * CoreStart.Ticks + IntersectionConstant;
            double coreEndPrice = SlopeConstant * CoreEnd.Ticks + IntersectionConstant;

            chart.DrawTrendLine(name, CoreStart, coreStartPrice, CoreEnd, coreEndPrice, Color);
        }
    }
}
