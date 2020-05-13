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
        private int blueDotCount;
        private int curBlueDotCount;
        private int greenDotCount;
        private int curGreenDotCount;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Number of blue dots generated
        /// </summary>
        public int BlueDotCount { get => blueDotCount; set { blueDotCount = value; OnPropertyChanged("BlueDotCount"); } }

        /// <summary>
        /// Number of blue dots on screen at the time
        /// </summary>
        public int CurrentBlueDotCount { get => curBlueDotCount; set { curBlueDotCount = value; OnPropertyChanged("CurrentBlueDotCount"); } }

        /// <summary>
        /// Number of green dots generated
        /// </summary>
        public int GreenDotCount { get => greenDotCount; set { greenDotCount = value; OnPropertyChanged("GreenDotCount"); } }

        /// <summary>
        /// Number of green dots on screen at the time
        /// </summary>
        public int CurrentGreenDotCount { get => curGreenDotCount; set { curGreenDotCount = value; OnPropertyChanged("CurrentGreenDotCount"); } }

        /// <summary>
        /// Amount of meetings of point of different colors
        /// </summary>
        public int ColorMeetings { get => colorMeetings; set { colorMeetings = value; OnPropertyChanged("ColorMeetings"); } }

        public double DistanceTravelled { get => distanceTravelled; set { distanceTravelled = value; OnPropertyChanged("DistanceTravelled"); } }

        public DateTime StartTime { get; }

        [JsonIgnore]
        public TimeSpan PausedTime { get; set; }

        public TimeSpan TimeElapsed { get => DateTime.Now - StartTime - PausedTime; }

        public List<KeyValuePair<SyncVertex, VertexData>> VertexStatList { get => vertexStatistics.ToList(); }

        /// <summary>
        /// Records how many dots have come in and out of each vertex
        /// </summary>
        public Dictionary<SyncVertex, VertexData> vertexStatistics;

        public List<SyncVertex> DeadEndVertices { get; set; }
        private double distanceTravelled;
        private int colorMeetings;

        public StatisticsModule(BidirectionalGraph<SyncVertex, WeightedEdge> graph)
        {
            PausedTime = new TimeSpan(0);
            StartTime = DateTime.Now;
            BlueDotCount = 0;
            GreenDotCount = 0;
            DistanceTravelled = 0;
            CurrentBlueDotCount = 0;
            CurrentGreenDotCount = 0;
            ColorMeetings = 0;
            vertexStatistics = new Dictionary<SyncVertex, VertexData>();
            foreach (var vert in graph.Vertices)
            {
                if (vert.Background != Brushes.Transparent) vertexStatistics.Add(vert, new VertexData(vert.InitSync));
            }
            DeadEndVertices = new List<SyncVertex>();
        }

        public void FindDeadEnds()
        {
            foreach (var vert in vertexStatistics.Keys)
            {
                if (vert.BlueSync != vert.InitSync || vert.GreenSync != vert.InitSync) DeadEndVertices.Add(vert);
            }
        }
    }
}
