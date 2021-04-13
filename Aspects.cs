using System;

namespace Temtem_EncounterTracker{
    public enum AspectRatio{
        Aspect16by9, Aspect4by3 
    }
    public static class Aspect{
        public static AspectRatio GetRatio(int width, int height){
            if(Math.Round((width / (double)height) * 3, 0) == 4){
                return AspectRatio.Aspect4by3;
            } else {
                return AspectRatio.Aspect16by9;
            }
        }

        public static double Temtem2PercentageLeft(AspectRatio ratio){
            switch(ratio){
                case AspectRatio.Aspect16by9:
                return 0.61125;
                case AspectRatio.Aspect4by3:
                return 0.61;
            }

            return 0;
        }

        public static double Temtem2PercentageTop(AspectRatio ratio){
            switch(ratio){
                case AspectRatio.Aspect16by9:
                return 0.0278;
                case AspectRatio.Aspect4by3:
                return 0.0197;
            }

            return 0;
        }

        public static double Temtem1PercentageLeft(AspectRatio ratio){
            switch(ratio){
                case AspectRatio.Aspect16by9:
                return 0.818125;
                case AspectRatio.Aspect4by3:
                return 0.8178;
            }

            return 0;
        }

        public static double Temtem1PercentageTop(AspectRatio ratio){
            switch(ratio){
                case AspectRatio.Aspect16by9:
                return 0.0789;
                case AspectRatio.Aspect4by3:
                return 0.057;
            }

            return 0;
        }

        public static double NameWidthPercentage(AspectRatio ratio){
            switch(ratio){
                case AspectRatio.Aspect16by9:
                return 0.1;
                case AspectRatio.Aspect4by3:
                return 0.1;
            }

            return 0;
        }

        public static double NameHeightPercentage(AspectRatio ratio){
            switch(ratio){
                case AspectRatio.Aspect16by9:
                return 0.0256;
                case AspectRatio.Aspect4by3:
                return 0.021;
            }

            return 0;
        }

        public static double Map1LeftPercentage(AspectRatio ratio){
            switch(ratio){
                case AspectRatio.Aspect16by9:
                return 0.8741;
                case AspectRatio.Aspect4by3:
                return 0.8555;
            }

            return 0;
        }

        public static double Map1TopPercentage(AspectRatio ratio){
            switch(ratio){
                case AspectRatio.Aspect16by9:
                return 0.0692;
                case AspectRatio.Aspect4by3:
                return 0.121;
            }

            return 0;
        }

        public static double Map2LeftPercentage(AspectRatio ratio){
            switch(ratio){
                case AspectRatio.Aspect16by9:
                return 0.977;
                case AspectRatio.Aspect4by3:
                return 0.9766;
            }

            return 0;
        }

        public static double Map2TopPercentage(AspectRatio ratio){
            switch(ratio){
                case AspectRatio.Aspect16by9:
                return 0.1927;
                case AspectRatio.Aspect4by3:
                return 0.0844;
            }

            return 0;
        }
    }
}