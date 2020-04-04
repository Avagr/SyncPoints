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

        public MainWindow()
        {
            this.DataContext = this;
            CreateGraphToVisualize();
            InitializeComponent();
        }

        private void CreateGraphToVisualize()
        {
            var g = new BidirectionalGraph<object, IEdge<object>>();
            var graph = new AdjacencyGraph<(int, int), IEdge<(int, int)>>();

            //add the vertices to the graph
            string[] vertices = new string[5];
            for (int i = 0; i < 5; i++)
            {
                vertices[i] = i.ToString();
                g.AddVertex(vertices[i]);
            }

            //add some edges to the graph
            g.AddEdge(new Edge<object>(vertices[0], vertices[1]));
            g.AddEdge(new Edge<object>(vertices[1], vertices[2]));
            g.AddEdge(new Edge<object>(vertices[2], vertices[3]));
            g.AddEdge(new Edge<object>(vertices[3], vertices[1]));
            g.AddEdge(new Edge<object>(vertices[1], vertices[4]));

            GraphToVisualize = g;
            var tup = (3, 5);
            tup.Item1 -= 3;
            Console.WriteLine(tup.Item1 + " " + tup.Item2);
        }
    }
}
