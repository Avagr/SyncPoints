using GraphX.Controls;
using GraphX.PCL.Common.Enums;
using GraphX.PCL.Logic.Algorithms.LayoutAlgorithms;
using GraphX.PCL.Logic.Algorithms.OverlapRemoval;
using QuickGraph;
using SyncPointsLib;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace SyncPoints
{
    /// <summary>
    /// Layout visual class
    /// </summary>
    public class MyGraphArea : GraphArea<SyncVertex, WeightedEdge, BidirectionalGraph<SyncVertex, WeightedEdge>> { }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public IBidirectionalGraph<object, IEdge<object>> GraphToVisualize { get; set; }
        static Random rnd = new Random();
        BidirectionalGraph<SyncVertex, WeightedEdge> graph;
        Storyboard pathAnimationStoryboard;

        public MainWindow()
        {
            this.DataContext = this;
            InitializeComponent();
            //gg_Area.SetVerticesMathShape(VertexShape.Triangle);
            Random Rand = new Random();

            //Create data graph object
            graph = new MyGraph();

            //Create and add vertices using some DataSource for ID's
            for (int i = 0; i < 30; i++)
            {
                graph.AddVertex(new SyncVertex(i, 5));
            }

            var vlist = graph.Vertices.ToList();
            //Generate random edges for the vertices
            foreach (var item in vlist)
            {
                var vertex2 = vlist[Rand.Next(0, graph.VertexCount - 1)];
                graph.AddEdge(new WeightedEdge(item, vertex2, 5));
            }
            GenerateLogicCore();
            gg_Area.GenerateGraph(graph, true, true);
        }

        private void GenerateLogicCore()
        {
            var LogicCore = new MyGXLogicCore();
            //This property sets layout algorithm that will be used to calculate vertices positions
            //Different algorithms uses different values and some of them uses edge Weight property.
            LogicCore.DefaultLayoutAlgorithm = LayoutAlgorithmTypeEnum.KK;
            //Now we can set optional parameters using AlgorithmFactory
            //NOTE: default parameters can be automatically created each time you change Default algorithms
            LogicCore.DefaultLayoutAlgorithmParams =
                               LogicCore.AlgorithmFactory.CreateLayoutParameters(LayoutAlgorithmTypeEnum.KK);
            //Unfortunately to change algo parameters you need to specify params type which is different for every algorithm.
            ((KKLayoutParameters)LogicCore.DefaultLayoutAlgorithmParams).MaxIterations = 100;

            //This property sets vertex overlap removal algorithm.
            //Such algorithms help to arrange vertices in the layout so no one overlaps each other.
            LogicCore.DefaultOverlapRemovalAlgorithm = OverlapRemovalAlgorithmTypeEnum.FSA;
            //Setup optional params
            LogicCore.DefaultOverlapRemovalAlgorithmParams =
                              LogicCore.AlgorithmFactory.CreateOverlapRemovalParameters(OverlapRemovalAlgorithmTypeEnum.FSA);
            ((OverlapRemovalParameters)LogicCore.DefaultOverlapRemovalAlgorithmParams).HorizontalGap = 50;
            ((OverlapRemovalParameters)LogicCore.DefaultOverlapRemovalAlgorithmParams).VerticalGap = 50;

            //This property sets edge routing algorithm that is used to build route paths according to algorithm logic.
            //For ex., SimpleER algorithm will try to set edge paths around vertices so no edge will intersect any vertex.
            LogicCore.DefaultEdgeRoutingAlgorithm = EdgeRoutingAlgorithmTypeEnum.SimpleER;

            //This property sets async algorithms computation so methods like: Area.RelayoutGraph() and Area.GenerateGraph()
            //will run async with the UI thread. Completion of the specified methods can be catched by corresponding events:
            //Area.RelayoutFinished and Area.GenerateGraphFinished.
            LogicCore.AsyncAlgorithmCompute = false;

            //Finally assign logic core to GraphArea object
            gg_Area.LogicCore = LogicCore;
        }

        private void TestDot_Click(object sender, RoutedEventArgs e)
        {
            var edge = gg_Area.EdgesList[graph.Edges.ToList<WeightedEdge>()[0]];
            Path pointPath = AnimateEdge(edge);

            pointPath.Loaded += delegate (object send, RoutedEventArgs ee)
            {
                // Start the storyboard.
                pathAnimationStoryboard.Begin(this);
            };

            //gg_zoomctrl.BeginStoryboard(pathAnimationStoryboard);
        }

        private Path AnimateEdge(EdgeControl edge)
        {
            edge.ManualDrawing = true;
            var edgepath = edge.GetEdgePathManually();
            edge.Foreground = Brushes.Red;
            EllipseGeometry ellipse = new EllipseGeometry(new Point(0, 0), 7, 7);
            if (this.FindName("movingPoint") != null) this.UnregisterName("movingPoint");
            this.RegisterName("movingPoint", ellipse);
            Path pointPath = new Path();
            pointPath.Data = ellipse;
            pointPath.Fill = Brushes.Blue;

            mainPanel.Children.Add(pointPath);

            PathGeometry animationPath = edgepath;

            PointAnimationUsingPath anim = new PointAnimationUsingPath();
            anim.PathGeometry = animationPath;
            anim.Duration = TimeSpan.FromSeconds(5);
            anim.RepeatBehavior = new RepeatBehavior(1);

            Storyboard.SetTargetName(anim, "movingPoint");
            Storyboard.SetTargetProperty(anim, new PropertyPath(EllipseGeometry.CenterProperty));

            pathAnimationStoryboard = new Storyboard();
            pathAnimationStoryboard.RepeatBehavior = new RepeatBehavior(1);
            pathAnimationStoryboard.Children.Add(anim);

            pathAnimationStoryboard.Completed += (object e, EventArgs args) =>
            {
                mainPanel.Children.Remove(pointPath);
            };
            return pointPath;
        }

        ///// <summary>
        ///// Gets source and target Point objects from an EdgeControl
        ///// </summary>
        ///// <param name="edge"> EdgeControl</param>
        ///// <returns> Tuple of (SourcePoint, TargetPoint) </returns>
        //public static (Point, Point) GetPointsFromEdgeControl(EdgeControl edge)
        //{

        //    Point source = new Point(GraphLayout.GetX(edge.Source), GraphLayout.GetY(edge.Source));
        //    Point target = new Point(GraphLayout.GetX(edge.Target), GraphLayout.GetY(edge.Target));
        //    return (source, target);
        //}

        private void TestVert_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in graph.Vertices)
            {
                item.Sync = 77;
            }
        }
    }
}
