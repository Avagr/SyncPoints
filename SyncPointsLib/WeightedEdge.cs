using GraphX.PCL.Common.Models;
using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SyncPointsLib
{
    /// <summary>
    /// Weighted directed edge
    /// </summary>
    /// <typeparam name="TVertex"> Type of vertex</typeparam>
    public class WeightedEdge : EdgeBase<SyncVertex>, INotifyPropertyChanged
    {
        private int blueDotsCount;
        private int greenDotsCount;

        public WeightedEdge() : base(null, null, 1)
        {
        }

        /// <summary>
        /// Weight of the edge
        /// </summary>
        [XmlAttribute("weight")]
        public new double Weight { get => base.Weight; set { base.Weight = value; OnPropertyChanged("Weight"); } }

        public WeightedEdge(SyncVertex source, SyncVertex target, double weight = 1) : base(source, target, weight) { }

        /// <summary>
        /// Number of blue dots to start on this edge
        /// </summary>
        public int BlueDotsCount { get => blueDotsCount; set { blueDotsCount = Math.Max(0, value); OnPropertyChanged("BlueDotsCount"); } }

        /// <summary>
        /// Number of green dots to start on this edge
        /// </summary>
        public int GreenDotsCount { get => greenDotsCount; set { greenDotsCount = Math.Max(0, value); OnPropertyChanged("GreenDotsCount"); } }

        public override string ToString()
        {
            return Weight.ToString();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
