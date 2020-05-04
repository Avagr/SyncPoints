using System.Collections.Generic;
using System.ComponentModel;

namespace SyncPointsLib
{
    public class VertexData : INotifyPropertyChanged
    {
        private int blueDotsOut;
        private int blueDotsIn;
        private int greenDotsOut;
        private int greenDotsIn;

        /// <summary>
        /// Number of dots that went into a vertex
        /// </summary>
        public int BlueDotsOut { get => blueDotsOut; set { blueDotsOut = value; OnPropertyChanged("BlueDotsOut"); } }

        /// <summary>
        /// Number of dots that went out of the vertex
        /// </summary>
        public int BlueDotsIn { get => blueDotsIn; set { blueDotsIn = value; OnPropertyChanged("BlueDotsIn"); } }

        /// <summary>
        /// Number of dots that went into a vertex
        /// </summary>
        public int GreenDotsOut { get => greenDotsOut; set { greenDotsOut = value; OnPropertyChanged("GreenDotsOut"); } }

        /// <summary>
        /// Number of dots that went out of the vertex
        /// </summary>
        public int GreenDotsIn { get => greenDotsIn; set { greenDotsIn = value; OnPropertyChanged("GreenDotsIn"); } }

        /// <summary>
        /// All the blue sync values
        /// </summary>
        public List<int> BlueSyncHistory { get; set; }

        /// <summary>
        /// All the green sync values
        /// </summary>
        public List<int> GreenSyncHistory { get; set; }

        public VertexData() { }

        public VertexData(int initSync)
        {
            BlueSyncHistory = new List<int> { initSync };
            GreenSyncHistory = new List<int> { initSync };
            BlueDotsIn = 0;
            BlueDotsOut = 0;
            GreenDotsIn = 0;
            GreenDotsOut = 0;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
