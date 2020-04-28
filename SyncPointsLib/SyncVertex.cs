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
            get => sync;
            set
            {
                sync = value;
                NotifyPropertyChanged("Sync");
            }
        }

        [JsonIgnore]
        public Brush Background { get => background; set { background = value; NotifyPropertyChanged("Background"); } }

        [XmlAttribute("InitSync")]
        public int InitSync { get; set; } // Synchronization to reset to 
        private int sync;
        private Brush background;

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

        public override string ToString()
        {
            return Sync.ToString();
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
