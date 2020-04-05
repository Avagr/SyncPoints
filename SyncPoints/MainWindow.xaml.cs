using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using QuickGraph.Data;
using QuickGraph;
using SyncPointsLib;
using GraphSharp.Controls;

namespace SyncPoints
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public IBidirectionalGraph<object, IEdge<object>> GraphToVisualize { get; set; }
        //public AdjacencyGraph<object, IEdge<object>> GraphToVisualize { get; set; }
        static Random rnd = new Random();

        public MainWindow()
        {
            this.DataContext = this;
            CreateGraphToVisualize();
            InitializeComponent();
            
        }

        private void CreateGraphToVisualize()
        {
            var graph = new BidirectionalGraph<object, IEdge<object>>();

            SyncVertex[] vert = new SyncVertex[30];
            for (int i = 0; i < 30; i++)
            {
                vert[i] = new SyncVertex(i, 1);
                graph.AddVertex(vert[i]);
            }
            int limit;
            for (int i = 0; i < 30; i++)
            {
                limit = rnd.Next(1, 5);
                for (int j = 0; j < limit; j++)
                {
                    graph.AddEdge(new Edge<object>(vert[i], vert[rnd.Next(30)]));
                }
            }
            GraphToVisualize = graph;
        }
    }
}
