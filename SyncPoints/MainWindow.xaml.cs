using GraphX.Controls;
using GraphX.PCL.Common.Enums;
using GraphX.PCL.Logic.Algorithms.LayoutAlgorithms;
using GraphX.PCL.Logic.Algorithms.OverlapRemoval;
using QuickGraph;
using SyncPointsLib;
using System;
using System.Collections.Generic;
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
        static Random rnd = new Random();
        BidirectionalGraph<SyncVertex, WeightedEdge> graph;

        Dictionary<SyncVertex, List<DotAnimation>> vertexStoryboards;

        WeightedEdge StartingEdge { get; }

        int animCount;

        public MainWindow()
        {
            this.DataContext = this;
            vertexStoryboards = new Dictionary<SyncVertex, List<DotAnimation>>();
            InitializeComponent();
            CreateGraph();
            StartingEdge = graph.Edges.ToList()[rnd.Next(graph.Edges.Count())];
            gg_Area.EdgesList[StartingEdge].Foreground = Brushes.Red;
            animCount = 0;
        }

        private void CreateGraph()
        {
            //Create data graph object
            graph = new MyGraph();

            //Create and add vertices
            for (int i = 0; i < 30; i++)
            {
                graph.AddVertex(new SyncVertex(i, 1));
            }

            var vlist = graph.Vertices.ToList();
            //Generate random edges for the vertices
            foreach (var item1 in vlist)
            {
                foreach (var item2 in vlist)
                {
                    if (item1 != item2 && rnd.Next(0, 10) < 1) graph.AddEdge(new WeightedEdge(item1, item2, rnd.Next(1,4)));
                }
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
            AnimateEdge(StartingEdge);
            if (!mainPanel.Children.Contains(vertexStoryboards[StartingEdge.Source][0].Path)) mainPanel.Children.Add(vertexStoryboards[StartingEdge.Source][0].Path);
            gg_zoomctrl.BeginStoryboard(vertexStoryboards[StartingEdge.Source][0].Storyboard, HandoffBehavior.SnapshotAndReplace, true);
            foreach (var edge in graph.OutEdges(StartingEdge.Source))
            {
                AnimateEdge(edge);
            }
            PreAnimateOutEdges(StartingEdge.Target);
            PreAnimateOutEdges(StartingEdge.Source);
            //var edge = gg_Area.EdgesList[graph.Edges.ToList<WeightedEdge>()[0]];
            //animationList.Enqueue(AnimateEdge(edge));
            //gg_zoomctrl.BeginStoryboard(animationList.Dequeue());
            //foreach (var item in graph.OutEdges((SyncVertex)edge.Target.Vertex))
            //{
            //    animationList.Enqueue(AnimateEdge(gg_Area.EdgesList[item]));
            //}
            //gg_zoomctrl.BeginStoryboard(pathAnimationStoryboard);
        }

        private void AnimateEdge(WeightedEdge edge)
        {
            EdgeControl edgeControl = gg_Area.EdgesList[edge];
            edgeControl.ManualDrawing = true;
            EllipseGeometry dot = new EllipseGeometry(new Point(0, 0), 7.5, 7.5);
            animCount++;
            Console.WriteLine(animCount);
            string dotName = "dot" + animCount;
            this.RegisterName(dotName, dot);
            Path dotPath = new Path();
            dotPath.Data = dot;
            dotPath.Fill = Brushes.Blue;

            PathGeometry animPath = edgeControl.GetEdgePathManually();
            animPath.Freeze();

            PointAnimationUsingPath animation = new PointAnimationUsingPath();
            animation.PathGeometry = animPath;
            animation.Duration = TimeSpan.FromSeconds(edge.Weight);
            animation.RepeatBehavior = new RepeatBehavior(1);
            Storyboard.SetTargetName(animation, dotName);
            Storyboard.SetTargetProperty(animation, new PropertyPath(EllipseGeometry.CenterProperty));

            Storyboard animStoryboard = new Storyboard();
            animStoryboard.Children.Add(animation);
            animStoryboard.RepeatBehavior = new RepeatBehavior(1);

            animStoryboard.Completed += (object e, EventArgs args) =>
            {
                mainPanel.Children.Remove(dotPath);
                edge.Target.Sync--;
                if (edge.Target.Sync < 1 && !graph.IsOutEdgesEmpty(edge.Target))
                {
                    foreach (var outEdge in vertexStoryboards[edge.Target])
                    {
                        if (!mainPanel.Children.Contains(outEdge.Path)) mainPanel.Children.Add(outEdge.Path);
                        gg_zoomctrl.BeginStoryboard(outEdge.Storyboard, HandoffBehavior.SnapshotAndReplace, true);
                        PreAnimateOutEdges(outEdge.Edge.Target);
                        PreAnimateOutEdges(outEdge.Edge.Source);
                    }
                    edge.Target.ResetSync();
                }
            };

            if (!vertexStoryboards.ContainsKey(edge.Source)) vertexStoryboards[edge.Source] = new List<DotAnimation>();
            vertexStoryboards[edge.Source].Add(new DotAnimation(animStoryboard, dotPath, edge));
        }

        /// <summary>
        /// Prepares animations for all edges coming from a specified vertex
        /// </summary>
        /// <param name="vert"> The vertex to get edges from</param>
        private void PreAnimateOutEdges(SyncVertex vertex)
        {
            foreach (var outEdge in graph.OutEdges(vertex))
            {
                var vert = outEdge.Target;
                if (!vertexStoryboards.ContainsKey(vert))
                {
                    foreach (var edge in graph.OutEdges(vert))
                    {
                        AnimateEdge(edge);
                    }
                }
            }
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
                item.Sync = 1;
            }
        }
    }
}
