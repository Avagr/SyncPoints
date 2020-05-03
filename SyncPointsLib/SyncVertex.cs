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
        /// Sync counter
        /// </summary>
        [JsonIgnore]
        public int Sync
        {
            get => Background == Brushes.Transparent ? 0 : sync;
            set
            {
                sync = value;
                if (sync < 0) Background = Brushes.Purple;
                OnPropertyChanged("Sync");
            }
        }

        [JsonIgnore]
        public Brush Background { get => background; set { background = value; OnPropertyChanged("Background"); } }

        [XmlAttribute("InitSync")]
        public int InitSync { get => initSync; set { initSync = value; Sync = value; OnPropertyChanged("InitSync"); OnPropertyChanged("Sync"); } } // Synchronization to reset to 
        private int sync;
        private Brush background;
        private int initSync;

        public SyncVertex()
        {
            Background = Brushes.OrangeRed;
            sync = 0;
        }

        public SyncVertex(int id, int sync)
        {
            if (sync < 1) throw new ArgumentException("Sync counter cannot be less than 1");
            ID = id;
            InitSync = sync;
            Sync = sync;
            Background = Brushes.OrangeRed;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Resets the synchronization counter
        /// </summary>
        public void ResetSync()
        {
            Sync = InitSync;
        }

        public override string ToString() => Sync.ToString();

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
