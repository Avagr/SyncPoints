using System.ComponentModel;
using System.Runtime.CompilerServices;
using GraphX.PCL.Common.Models;

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
        public int Sync
        {
            get => sync;
            set
            {
                sync = value;
                NotifyPropertyChanged();
            }
        }

        private readonly int initSync; // Synchronization to reset to 
        private int sync;

        public SyncVertex() { }

        public SyncVertex(int id, int sync)
        {
            ID = id;
            initSync = sync;
            Sync = sync;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Resets the synchronization counter
        /// </summary>
        public void ResetSync()
        {
            Sync = initSync;
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
