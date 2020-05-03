using QuickGraph;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json.Serialization;
using System.Windows.Media;

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

        public double DistanceTravelled { get => distanceTravelled; set { distanceTravelled = value; OnPropertyChanged("DistanceTravelled"); } }

        public DateTime StartTime { get; }

        [JsonIgnore]
        public TimeSpan PausedTime { get; set; }

        public TimeSpan TimeElapsed { get => DateTime.Now - StartTime - PausedTime; }

        public List<KeyValuePair<SyncVertex, VertexData>> VertexStatList { get => VertexStatistics.ToList(); }

        /// <summary>
        /// Records how many dots have come in and out of each vertex
        /// </summary>
        public Dictionary<SyncVertex, VertexData> VertexStatistics;

        public List<SyncVertex> DeadEndVertices { get; set; }
        private double distanceTravelled;

        public StatisticsModule(BidirectionalGraph<SyncVertex, WeightedEdge> graph)
        {
            PausedTime = new TimeSpan(0);
            StartTime = DateTime.Now;
            DotCount = 0;
            DistanceTravelled = 0;
            CurrentDotCount = 0;
            VertexStatistics = new Dictionary<SyncVertex, VertexData>();
            foreach (var vert in graph.Vertices)
            {
                if (vert.Background != Brushes.Transparent) VertexStatistics.Add(vert, new VertexData(vert.InitSync));
            }
            DeadEndVertices = new List<SyncVertex>();
        }

        public void FindDeadEnds()
        {
            foreach (var vert in VertexStatistics.Keys)
            {
                if (vert.Sync != vert.InitSync) DeadEndVertices.Add(vert);
            }
        }
    }
}
