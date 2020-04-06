namespace SyncPointsLib
{
    /// <summary>
    /// Vertex that supports syncing
    /// </summary>
    public struct SyncVertex
    {
        /// <summary>
        /// ID of the vertex
        /// </summary>
        public int ID { get; }

        /// <summary>
        /// Sync counter
        /// </summary>
        public int Sync { get; set; }

        private readonly int initSync; // Synchronization to reset to 

        public SyncVertex(int id, int sync)
        {
            ID = id;
            initSync = sync;
            Sync = sync;
        }

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
    }
}
