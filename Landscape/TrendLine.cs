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
        public double k;
        public double q;

        public DateTime start;
        public DateTime end;

        public TrendLine(double k, double q, DateTime startD, DateTime endD)
        {
            this.k = k;
            this.q = q;
            this.start = startD;
            this.end = endD;

        }

        public void Visualize(Chart chart)
        {
            string name = start.ToString() + end.ToString();
            chart.DrawTrendLine(name, start, k*start.Ticks + q, end, k*end.Ticks + q, Color.Red);
        }
    }
}
