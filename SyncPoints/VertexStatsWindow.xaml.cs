using SyncPointsLib;
using System.Collections.Generic;
using System.Windows;

namespace SyncPoints
{
    /// <summary>
    /// Interaction logic for VertexStatsWindow.xaml
    /// </summary>
    public partial class VertexStatsWindow : Window
    {
        public Dictionary<SyncVertex, VertexData> Stats { get; set; } // Stats of the graph

        public VertexStatsWindow(Dictionary<SyncVertex, VertexData> stats)
        {
            Stats = stats;
            //statsView.ItemsSource = stats;
            Resources["Stats"] = Stats;
            InitializeComponent();
            DataContext = this;
        }
    }
}
