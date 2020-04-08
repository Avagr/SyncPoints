using GraphX.PCL.Common.Models;

namespace SyncPointsLib
{
    /// <summary>
    /// Weighted directed edge
    /// </summary>
    /// <typeparam name="TVertex"> Type of vertex</typeparam>
    public class WeightedEdge : EdgeBase<SyncVertex>
    {
        public WeightedEdge() : base(null, null, 1) { }

        public WeightedEdge(SyncVertex source, SyncVertex target, double weight = 1) : base(source, target, weight) { }

        public override string ToString()
        {
            return Weight.ToString();
        }
    }
}
