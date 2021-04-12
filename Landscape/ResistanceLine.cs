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
    abstract class ResistanceLine
    {
        public double Intensity;
        abstract public void Visualize(Chart chart);
        abstract public double IntensityAtBar(int barIndex);
    }
}
