using System.Collections.Generic;
using System.Linq;

namespace SyncPointsLib
{
    public class VertexData
    {
        /// <summary>
        /// Number of dots that went into a vertex
        /// </summary>
        public int DotsOut { get; set; }

        /// <summary>
        /// Number of dots that went out of the vertex
        /// </summary>
        public int DotsIn { get; set; }

        /// <summary>
        /// All the sync values
        /// </summary>
        public List<int> SyncHistory { get; set; }

        public VertexData(int initSync)
        {
            SyncHistory = new List<int> { initSync };
            DotsIn = 0;
            DotsOut = 0;
        }

        /// <summary>
        /// Adds a new sync value to the list that is decreased by one compared to the last one
        /// </summary>
        public void DecreaseSync()
        {
            SyncHistory.Add(SyncHistory.Last() - 1);
        }

        /// <summary>
        /// Adds a new sync value to the list that is increased by one compared to the last one
        /// </summary>
        public void IncreaseSync()
        {
            SyncHistory.Add(SyncHistory.Last() + 1);
        }
    }
}
