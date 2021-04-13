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
    class TrendLine : ResistanceLine
    {
        // price = k*index + q
        public double SlopeConstant { get; }
        public double IntersectionConstant { get; }

        public TrendCore Core { get; }

        private Color Color { get; }

        public TrendLine(double slopeConstant, double intersectionConstant, TrendCore core, Color color)
        {
            SlopeConstant = slopeConstant;
            IntersectionConstant = intersectionConstant;
            Core = core;
            Color = color;
        }

        public override double IntensityAtBar(int barIndex)
        {
            throw new NotImplementedException();
        }

        public override void Visualize(Chart chart)
        {
            string name = Guid.NewGuid().ToString();

            double coreStartPrice = SlopeConstant * Core.StartIndex + IntersectionConstant;
            double coreEndPrice = SlopeConstant * Core.EndIndex + IntersectionConstant;

            chart.DrawTrendLine(name, Core.StartIndex, coreStartPrice, Core.EndIndex, coreEndPrice, Color);
        }
    }
}
