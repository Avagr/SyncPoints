using System.ComponentModel;
using System.Runtime.CompilerServices;
using GraphX.PCL.Common.Models;
using System;

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
            if (sync < 1) throw new ArgumentException("Sync counter cannot be less than 1");
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
