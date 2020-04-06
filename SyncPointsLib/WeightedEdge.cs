using QuickGraph;

namespace SyncPointsLib
{
    /// <summary>
    /// Weighted directed edge
    /// </summary>
    /// <typeparam name="TVertex"> Type of vertex</typeparam>
    public class WeightedEdge<TVertex> : IEdge<TVertex>
    {
        public WeightedEdge(TVertex source, TVertex target, int weight)
        {
            Source = source;
            Target = target;
            Weight = weight;
        }

        /// <summary>
        /// Source vertex
        /// </summary>
        public TVertex Source { get; }

        /// <summary>
        /// Target vertex
        /// </summary>
        public TVertex Target { get; }

        /// <summary>
        /// Weight of the edge
        /// </summary>
        public int Weight { get; }
    }
}
