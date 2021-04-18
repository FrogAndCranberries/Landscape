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
    class SupportLine : ResistanceLine
    {
        public double Price { get; private set; }
        public int StartIndex { get; private set; }
        public DateTime StartTime { get; private set; }

        public List<int> JointIndices { get; private set; }

        public SupportLine(Peak startPeak, double intensity)
        {
            Price = startPeak.Price;
            StartIndex = startPeak.BarIndex;
            StartTime = startPeak.DateTime;
            Intensity = intensity;
            JointIndices = new List<int>();
        }

        public override string ToString()
        {
            return string.Format("SupportLine at Price {0}, StartIndex {1}, Intensity {2}.", Price, StartIndex, Intensity);
        }

        public override double IntensityAtBar(int barIndex)
        {
            if (barIndex < StartIndex) return 0;

            double intensityDecayConstant = ConstantManager.SupportLines.IntensityDecay;

            return Intensity * Math.Exp(-intensityDecayConstant * (barIndex - StartIndex));
        }

        public void MergeWithLine (SupportLine other)
        {
            if(other.StartIndex < StartIndex)
            {
                string message = string.Format("Cannot merge {0} into {1} because it starts earlier.", other.ToString(), ToString());
                throw new ArgumentException(message);
            }

            JointIndices.Add(StartIndex);

            Price = (Price * Intensity + other.Price * other.Intensity) / (Intensity + other.Intensity);
            Intensity = IntensityAtBar(other.StartIndex) + other.Intensity / 100 * (100 - IntensityAtBar(other.StartIndex));
            StartIndex = other.StartIndex;
            StartTime = other.StartTime;
            
        }

        public override void Visualize(Chart chart)
        {
            string name = Guid.NewGuid().ToString();

            chart.DrawHorizontalLine(name, Price, GetColor(), 2);
            chart.DrawIcon(name + "start", ChartIconType.Diamond, StartIndex, Price, GetColor());

            foreach(int joint in JointIndices)
            {
                chart.DrawIcon(name + joint, ChartIconType.Star, joint, Price, Color.Aqua);
            }
        }

        private Color GetColor()
        {
            double maxShiftConstant = ConstantManager.SupportLines.IntensityToColorMaximum;
            double centerConstant = ConstantManager.SupportLines.IntensityToColorCenter;
            double steepnessConstant = ConstantManager.SupportLines.IntensityToColorSteepness;

            double shift = maxShiftConstant * SpecialFunctions.Logistic(steepnessConstant * (Intensity - centerConstant));
            int blue = 255 - (int)shift;
            int red = (int)shift;
            return Color.FromArgb(200, red, 30, blue);
        }
    }
}
