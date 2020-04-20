using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace SyncPointsLib
{
    /// <summary>
    /// Stores Path. Edge and Storyboard of an edge dot animation
    /// </summary>
    public class DotAnimation
    {
        public DotAnimation(Storyboard storyboard, Path path)
        {
            Storyboard = storyboard;
            Path = path;
        }

        /// <summary>
        /// The Storyboard object of DotAnimation
        /// </summary>
        public Storyboard Storyboard { get; }

        /// <summary>
        /// The Path object of DotAnimation
        /// </summary>
        public Path Path { get; }

    }
}
