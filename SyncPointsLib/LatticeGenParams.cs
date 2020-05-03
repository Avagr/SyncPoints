using System.ComponentModel;

namespace SyncPointsLib
{
    /// <summary>
    /// Parameters for generating a lattice graph
    /// </summary>
    public class LatticeGenParams : INotifyPropertyChanged
    {
        private int polygonEdgeNum;
        private int horizontalTiles;
        private int verticalTiles;
        private bool createBorderCascade;
        private int syncLowerBound;
        private int syncUpperBound;

        /// <summary>
        /// Number of edgets in a lattice's tile
        /// </summary>
        public int PolygonEdgeNum { get => polygonEdgeNum; set { polygonEdgeNum = value; OnPropertyChanged("PolygonEdgeNum"); } }

        /// <summary>
        /// Number of tiles horizontally
        /// </summary>
        public int HorizontalTileCount { get => horizontalTiles; set { if (value > 0) horizontalTiles = value; OnPropertyChanged("HorizontalTileCount"); } }

        /// <summary>
        /// Number of tiles vertically
        /// </summary>
        public int VerticalTileCount { get => verticalTiles; set { if (value > 0) verticalTiles = value; OnPropertyChanged("VerticalTileCount"); } }

        /// <summary>
        /// Whether to create a cascade at the borders of the graph
        /// </summary>
        public bool CreateBorderCascade { get => createBorderCascade; set { createBorderCascade = value; OnPropertyChanged("CreateBorderCscade"); } }

        /// <summary>
        /// A lower bound of random synchronization number generation
        /// </summary>
        public int SyncLowerBound
        {
            get => syncLowerBound; set
            {
                if (value >= 1)
                {
                    if (value > syncUpperBound && syncUpperBound != 0)
                    {
                        SyncUpperBound = value;
                        OnPropertyChanged("SyncUpperBound");
                    }
                    syncLowerBound = value;
                    OnPropertyChanged("SyncLowerBound");
                }
            }
        }

        /// <summary>
        /// An upper bound of random synchronization number generation
        /// </summary>
        public int SyncUpperBound
        {
            get => syncUpperBound; set
            {
                if (value >= 1)
                {
                    if (value < SyncLowerBound && syncLowerBound != 0)
                    {
                        SyncLowerBound = value;
                        OnPropertyChanged("SyncLowerBound");
                    }
                    syncUpperBound = value;
                    OnPropertyChanged("SyncUpperBound");
                }
            }
        }

        public bool CheckIfFilled()
        {
            if (PolygonEdgeNum != 0 && HorizontalTileCount != 0 && VerticalTileCount != 0 && SyncLowerBound != 0 && SyncUpperBound != 0) return true;
            return false;
        }

        public LatticeGenParams()
        {
            PolygonEdgeNum = 3;
        }

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
