using QuickGraph;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace SyncPointsLib
{
    /// <summary>
    /// Statistics of a graph animation
    /// </summary>
    public class StatisticsModule : INotifyPropertyChanged
    {
        private int dotCount;
        private int curDotCount;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Number of dots generated
        /// </summary>
        public int DotCount { get => dotCount; set { dotCount = value; OnPropertyChanged("DotCount"); } }

        /// <summary>
        /// Number of dots onscreen at the time
        /// </summary>
        public int CurrentDotCount { get => curDotCount; set { curDotCount = value; OnPropertyChanged("CurrentDotCount"); } }

        public DateTime StartTime { get; }

        public TimeSpan PausedTime { get; set; }

        public TimeSpan TimeElapsed { get => DateTime.Now - StartTime - PausedTime; }

        /// <summary>
        /// Records how many dots have come in and out of each vertex
        /// </summary>
        public Dictionary<SyncVertex, VertexData> VertexStatistics;

        public ObservableCollection<SyncVertex> DeadEndVertices;

        public StatisticsModule(BidirectionalGraph<SyncVertex, WeightedEdge> graph)
        {
            PausedTime = new TimeSpan(0);
            StartTime = DateTime.Now;
            DotCount = 0;
            CurrentDotCount = 0;
            VertexStatistics = new Dictionary<SyncVertex, VertexData>();
            foreach (var vert in graph.Vertices)
            {
                VertexStatistics.Add(vert, new VertexData(vert.initSync));
            }
            DeadEndVertices = new ObservableCollection<SyncVertex>();
        }
    }
}
