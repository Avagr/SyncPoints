using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace SyncPointsLib
{
    /// <summary>
    /// Stores Path. Edge and Storyboard of an edge dot animation
    /// </summary>
    public class DotAnimation
    {
        public DotAnimation(Storyboard storyboard, Path path, WeightedEdge edge)
        {
            Storyboard = storyboard;
            Path = path;
            Edge = edge;
        }

        /// <summary>
        /// The Storyboard object of DotAnimation
        /// </summary>
        public Storyboard Storyboard { get; }

        /// <summary>
        /// The Path object of DotAnimation
        /// </summary>
        public Path Path { get; }

        /// <summary>
        /// The Edge object that is animated
        /// </summary>
        public WeightedEdge Edge { get; }
    }
}
