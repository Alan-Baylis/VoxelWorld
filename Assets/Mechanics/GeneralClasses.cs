using UnityEngine;
using System.Collections;
using System;
using GeneralClasses;

namespace GeneralClasses
{
    public class MathExtension
    {
        public static int Round(double d)
        {
            
            if (d < 0)
            {
                return (int)Math.Ceiling(d);
            }
            else
            {
                return (int)Math.Floor(d);
            }
        }
    }

    [Serializable]
    public class Range
    {
        public int LowerLimit { get; set; }
        public int UpperLimit { get; set; }

        public Range(int LowerLimit, int UpperLimit)
        {
            this.UpperLimit = UpperLimit;
            this.LowerLimit = LowerLimit;
        }

        public int GetRange()
        {
            if (LowerLimit < UpperLimit)
            {
                return UpperLimit - LowerLimit;
            }
            else
            {
                return LowerLimit - UpperLimit;
            }
        }

        public int GetMidpointValue()
        {
            if (LowerLimit < UpperLimit)
            {
                return LowerLimit + (GetRange() / 2);
            }
            else
            {
                return UpperLimit + (GetRange() / 2);
            }
        }
    }
}


