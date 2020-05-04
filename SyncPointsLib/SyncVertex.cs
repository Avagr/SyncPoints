using GraphX.PCL.Common.Models;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Windows.Media;
using System.Xml.Serialization;

namespace SyncPointsLib
{
    /// <summary>
    /// Vertex that supports syncing
    /// </summary>
    public class SyncVertex : VertexBase, INotifyPropertyChanged
    {

        /// <summary>
        /// Blue sync counter
        /// </summary>
        [JsonIgnore]
        public int BlueSync
        {
            get => Background == Brushes.Transparent ? 0 : blueSync;
            set
            {
                blueSync = value;
                if (blueSync < 0) Background = Brushes.Purple;
                OnPropertyChanged("BlueSync");
            }
        }

        /// <summary>
        /// Green sync counter
        /// </summary>
        [JsonIgnore]
        public int GreenSync
        {
            get => Background == Brushes.Transparent ? 0 : greenSync;
            set
            {
                greenSync = value;
                if (greenSync < 0) Background = Brushes.Purple;
                OnPropertyChanged("GreenSync");
            }
        }

        [JsonIgnore]
        public Brush Background { get => background; set { background = value; OnPropertyChanged("Background"); } }

        [XmlAttribute("InitSync")]
        public int InitSync { get => initSync; set { initSync = value; BlueSync = value; GreenSync = value;  OnPropertyChanged("InitSync"); OnPropertyChanged("BlueSync"); OnPropertyChanged("GreenSync"); } } // Synchronization to reset to 
        private int blueSync;
        private int greenSync;
        private Brush background;
        private int initSync;

        public SyncVertex()
        {
            Background = Brushes.OrangeRed;
            InitSync = 0;
        }

        public SyncVertex(int id, int sync)
        {
            if (sync < 1) throw new ArgumentException("Sync counter cannot be less than 1");
            ID = id;
            InitSync = sync;
            BlueSync = sync;
            GreenSync = sync;
            Background = Brushes.OrangeRed;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Resets the synchronization counter
        /// </summary>
        public void ResetBlueSync()
        {
            BlueSync = InitSync;
        }

        /// <summary>
        /// Resets the synchronization counter
        /// </summary>
        public void ResetGreenSync()
        {
            GreenSync = InitSync;
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
