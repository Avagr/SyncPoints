using System;

namespace SyncPoints
{
    /// <summary>
    /// Amount of points at a given elapsed time
    /// </summary>
    public class PointValues
    {
        public TimeSpan TimeElapsed { get; set; }
        public int PointNumber { get; set; }
    }
}
