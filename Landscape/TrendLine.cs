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
        public double SlopeConstant;
        public double IntersectionConstant;

        public DateTime CoreStart;
        public DateTime CoreEnd;

        public int CoreStartIndex;
        public int CoreEndIndex;

        Color Color;

        public TrendLine(double slopeConstant, double intersectionConstant, DateTime coreStart, DateTime coreEnd, int coreStartIndex, int coreEndIndex, Color color)
        {
            SlopeConstant = slopeConstant;
            IntersectionConstant = intersectionConstant;
            CoreStart = coreStart;
            CoreEnd = coreEnd;
            CoreStartIndex = coreStartIndex;
            CoreEndIndex = coreEndIndex;
            Color = color;
        }

        public override void Visualize(Chart chart)
        {
            string name = Guid.NewGuid().ToString();

            double coreStartPrice = SlopeConstant * CoreStartIndex + IntersectionConstant;
            double coreEndPrice = SlopeConstant * CoreEndIndex + IntersectionConstant;

            chart.DrawTrendLine(name, CoreStartIndex, coreStartPrice, CoreEndIndex, coreEndPrice, Color);
        }
    }
}
