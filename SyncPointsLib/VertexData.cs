using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace SyncPointsLib
{
    public class VertexData : INotifyPropertyChanged
    {
        private int dotsOut;
        private int dotsIn;

        /// <summary>
        /// Number of dots that went into a vertex
        /// </summary>
        public int DotsOut { get => dotsOut; set { dotsOut = value; OnPropertyChanged("DotsOut"); } }

        /// <summary>
        /// Number of dots that went out of the vertex
        /// </summary>
        public int DotsIn { get => dotsIn; set { dotsIn = value; OnPropertyChanged("DotsIn"); } }

        /// <summary>
        /// All the sync values
        /// </summary>
        public List<int> SyncHistory { get; set; }

        public VertexData() { }

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

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
