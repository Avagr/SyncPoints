using System.ComponentModel;

namespace SyncPointsLib
{
    /// <summary>
    /// Parameter for randomly generating a graph
    /// </summary>
    public class GraphGenerationParams : INotifyPropertyChanged
    {
        private int vertexCount;
        private double edgeProbability;
        private int syncLowerBound;
        private int syncUpperBound;
        private double weightLowerBound;
        private double weightUpperBound;

        /// <summary>
        /// Number of vertices in a graph
        /// </summary>
        public int VertexCount
        {
            get => vertexCount; set
            {
                if (value >= 0) vertexCount = value;
                OnPropertyChanged("VertexCount");
            }
        }

        /// <summary>
        /// The probability that there will be an edge from any vertice to any other
        /// </summary>
        public double EdgeProbability
        {
            get => edgeProbability;
            set
            {
                if (value > 0 && value <= 1) edgeProbability = value;
                OnPropertyChanged("EdgeProbability");
            }
        }

        /// <summary>
        /// The string to bind to a textbox
        /// </summary>
        public string EdgeProbabilityString { get; set; }

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

        /// <summary>
        /// A lower bound of random edge weight generation
        /// </summary>
        public double WeightLowerBound
        {
            get => weightLowerBound; set
            {
                if (value > 0)
                {
                    if (value > weightUpperBound && weightUpperBound != 0)
                    {
                        WeightUpperBound = value;
                        OnPropertyChanged("WeightUpperBound");
                    }
                    weightLowerBound = value;
                    OnPropertyChanged("WeightLowerBound");
                }
            }
        }

        /// <summary>
        /// An upper bound of random edge weight generation
        /// </summary>
        public double WeightUpperBound
        {
            get => weightUpperBound; set
            {
                if (value > 0)
                {
                    if (value < weightLowerBound && weightLowerBound != 0)
                    {
                        WeightLowerBound = value;
                        OnPropertyChanged("WeightLowerBound");
                    }
                    weightUpperBound = value;
                    OnPropertyChanged("WeightUpperBound");
                }
            }
        }

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Checks if all of the filed were filled
        /// </summary>
        public bool CheckIfFilled()
        {
            if (VertexCount == 0 || EdgeProbabilityString == null || SyncLowerBound == 0 || SyncUpperBound == 0 || WeightLowerBound == 0 || WeightUpperBound == 0) return false;
            return true;
        }
    }
}
