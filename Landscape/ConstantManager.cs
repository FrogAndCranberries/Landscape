using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace cAlgo
{
    static class ConstantManager
    {
        public static SupportLineConstantsRepository SupportLines = new SupportLineConstantsRepository();
        public static TrendConstantsRepository Trends = new TrendConstantsRepository();
    }

    class SupportLineConstantsRepository
    {
        public double IntensityDecay { get; set; }

        public double IntensityToColorMaximum { get; set; }

        public double IntensityToColorCenter { get; set; }

        public double IntensityToColorSteepness { get; set; }

        public SupportLineConstantsRepository()
        {
            IntensityDecay = 0.002;
            IntensityToColorMaximum = 255;
            IntensityToColorCenter = 40;
            IntensityToColorSteepness = 0.05;
        }
    }

    class TrendConstantsRepository
    {
        public double IntensityDecay { get; set; }

        public double LengthToIntensityMaximum { get; set; }

        public double LengthToIntensityCenter { get; set; }

        public double LengthToIntensitySteepness { get; set; }

        public TrendConstantsRepository()
        {
            IntensityDecay = 0.002;
            LengthToIntensityMaximum = 100;
            LengthToIntensityCenter = 50;
            LengthToIntensitySteepness = 0.05;
        }
    }
}
