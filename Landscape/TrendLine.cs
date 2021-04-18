using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;
using MathNet.Numerics;

namespace cAlgo
{
    class TrendLine : ResistanceLine
    {
        // price = k*index + q
        public double SlopeConstant { get; private set; }
        public double IntersectionConstant { get; private set; }

        public TrendCore Core { get; private set; }

        public TrendLine(double slopeConstant, double intersectionConstant, TrendCore core, double intensity)
        {
            SlopeConstant = slopeConstant;
            IntersectionConstant = intersectionConstant;
            Core = core;
            Intensity = intensity;
        }

        public override double IntensityAtBar(int barIndex)
        {
            if (barIndex < Core.EndIndex) return 0;

            double intensityDecayConstant = ConstantManager.TrendLines.IntensityDecay;

            return Intensity * Math.Exp(-intensityDecayConstant * (barIndex - Core.EndIndex));
        }

        public override void Visualize(Chart chart)
        {
            string name = Guid.NewGuid().ToString();

            double coreStartPrice = SlopeConstant * Core.StartIndex + IntersectionConstant;
            double coreEndPrice = SlopeConstant * Core.EndIndex + IntersectionConstant;

            chart.DrawTrendLine(name, Core.StartIndex, coreStartPrice, Core.EndIndex, coreEndPrice, GetColor());
        }

        //TODO: base color on intensity at current bar
        private Color GetColor()
        {
            double maxShiftConstant = ConstantManager.TrendLines.IntensityToColorMaximum;
            double centerConstant = ConstantManager.TrendLines.IntensityToColorCenter;
            double steepnessConstant = ConstantManager.TrendLines.IntensityToColorSteepness;

            double shift = maxShiftConstant * SpecialFunctions.Logistic(steepnessConstant * (Intensity - centerConstant));
            int green = 255 - (int)shift;
            int red = (int)shift;
            return Color.FromArgb(200, red, green, 30);
        }
    }
}
