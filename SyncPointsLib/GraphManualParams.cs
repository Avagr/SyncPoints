using QuickGraph;
using System.ComponentModel;
using System.Linq;

namespace SyncPointsLib
{
    /// <summary>
    /// Parameters for creating a graph manually
    /// </summary>
    public class GraphManualParams : INotifyPropertyChanged
    {
        private BidirectionalGraph<SyncVertex, WeightedEdge> graph;
        private SyncVertex selectedVertex;
        private WeightedEdge selectedEdge;
        private double createEdgeWeight;

        /// <summary>
        /// The graph that is being built
        /// </summary>
        public BidirectionalGraph<SyncVertex, WeightedEdge> Graph { get => graph; set { graph = value; OnPropertyChanged("Graph"); } }

        /// <summary>
        /// Selected vertex for displaying
        /// </summary>
        public SyncVertex SelectedVertex { get => selectedVertex; set { selectedVertex = value; OnPropertyChanged("SelectedVertex"); } }

        /// <summary>
        /// Selected edge for displaying
        /// </summary>
        public WeightedEdge SelectedEdge { get => selectedEdge; set { selectedEdge = value; OnPropertyChanged("SelectedEdge"); } }

        /// <summary>
        /// Synchronization for a created vertex
        /// </summary>
        public int CreateVertexSync { get; set; }

        /// <summary>
        /// ID of the vertex that is teh source of the created edge
        /// </summary>
        public int CreateEdgeSourceID { get; set; }

        /// <summary>
        /// ID of the vertex that is the target of the created edge
        /// </summary>
        public int CreateEdgeTargetID { get; set; }

        /// <summary>
        /// Weight of the edge being created
        /// </summary>
        public double CreateEdgeWeight { get => createEdgeWeight; set { if (value > 0) createEdgeWeight = value; } }

        public int nextVertexId;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public GraphManualParams()
        {
            Graph = new BidirectionalGraph<SyncVertex, WeightedEdge>();
            nextVertexId = 0;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
