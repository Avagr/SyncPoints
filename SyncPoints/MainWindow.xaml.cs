using GraphX.Controls;
using GraphX.PCL.Common.Enums;
using GraphX.PCL.Logic.Algorithms.LayoutAlgorithms;
using GraphX.PCL.Logic.Algorithms.OverlapRemoval;
using ModernWpf.Controls.Primitives;
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

        List<Storyboard> ActiveStoryboards { get; set; }

        List<WeightedEdge> StartingEdges { get; set; }

        int animCount, dotCount;

        public MainWindow()
        {
            //NameScope.SetNameScope(this, new NameScope());
            this.DataContext = this;
            InitializeComponent();
            CreateGraph();
            ZoomControl.SetViewFinderVisibility(zoomcontrol, Visibility.Collapsed);
            StartingEdges = new List<WeightedEdge>();
            ActiveStoryboards = new List<Storyboard>();
            foreach (var edge in graphArea.EdgesList.Keys)
            {
                if (rnd.Next(2) != 0) StartingEdges.Add(edge);
            }
            animCount = dotCount = 0;
        }

        private void CreateGraph()
        {
            //Create data graph object
            graph = new MyGraph();

            //Create and add vertices
            for (int i = 0; i < 30; i++)
            {
                graph.AddVertex(new SyncVertex(i, rnd.Next(5, 8)));
            }

            var vlist = graph.Vertices.ToList();
            //Generate random edges for the vertices
            foreach (var item1 in vlist)
            {
                foreach (var item2 in vlist)
                {
                    if (item1 != item2 && rnd.Next(0, 5) < 1) graph.AddEdge(new WeightedEdge(item1, item2, rnd.NextDouble() * 8 + 1));
                }
            }
            GenerateLogicCore();
            graphArea.GenerateGraph(graph, true, true);
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
            ((KKLayoutParameters)LogicCore.DefaultLayoutAlgorithmParams).MaxIterations = 1000;

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
            graphArea.LogicCore = LogicCore;
        }

        private void TestDot_Click(object sender, RoutedEventArgs e)
        {
            DotAnimation anim;
            foreach (var edge in StartingEdges)
            {
                anim = AnimateEdge(edge);
                if (!mainPanel.Children.Contains(anim.Path)) mainPanel.Children.Add(anim.Path);
                zoomcontrol.BeginStoryboard(anim.Storyboard, HandoffBehavior.SnapshotAndReplace, true);
                ActiveStoryboards.Add(anim.Storyboard);
            }
        }

        /// <summary>
        /// Animates a single edge of the graph
        /// </summary>
        /// <param name="edge"> Edge to animate</param>
        /// <returns> A DotAnimation class that contains the Path and Storyboard of an animation</returns>
        private DotAnimation AnimateEdge(WeightedEdge edge)
        {
            EdgeControl edgeControl = graphArea.EdgesList[edge];
            edgeControl.ManualDrawing = true;
            EllipseGeometry dot = new EllipseGeometry(new Point(0, 0), 7.5, 7.5);
            animCount++;
            dotCount++;
            if (dotCount > 2000) Environment.Exit(1);
            //Console.WriteLine(dotCount);
            string dotName = "dot" + animCount; // Naming the object with a unique ID
            this.RegisterName(dotName, dot);
            Path dotPath = new Path();
            dotPath.Data = dot;
            dotPath.Fill = Brushes.Blue;

            PathGeometry animPath = edgeControl.GetEdgePathManually();
            animPath.Freeze();

            PointAnimationUsingPath animation = new PointAnimationUsingPath();
            animation.PathGeometry = animPath;
            animation.Duration = TimeSpan.FromSeconds(edge.Weight);
            animation.RepeatBehavior = new RepeatBehavior(1); // Repeats once
            Storyboard.SetTargetName(animation, dotName);
            Storyboard.SetTargetProperty(animation, new PropertyPath(EllipseGeometry.CenterProperty)); // Animating the center of the dot

            Storyboard animStoryboard = new Storyboard();
            animStoryboard.Children.Add(animation);
            this.RegisterName(dotName + "Storyboard", animStoryboard);
            animStoryboard.RepeatBehavior = new RepeatBehavior(1); // Repeats one

            animStoryboard.Completed += (object e, EventArgs args) =>
            {
                mainPanel.Children.Remove(dotPath);
                dotCount--;
                edge.Target.Sync--;
                ActiveStoryboards.Remove(animStoryboard);
                if (edge.Target.Sync < 1 && !graph.IsOutEdgesEmpty(edge.Target))
                {
                    foreach (var outEdge in graph.OutEdges(edge.Target))
                    {
                        DotAnimation anim = AnimateEdge(outEdge);
                        if (!mainPanel.Children.Contains(anim.Path)) mainPanel.Children.Add(anim.Path);
                        zoomcontrol.BeginStoryboard(anim.Storyboard, HandoffBehavior.SnapshotAndReplace, true);
                        ActiveStoryboards.Add(anim.Storyboard);
                    }
                    edge.Target.ResetSync();
                }
            };

            return (new DotAnimation(animStoryboard, dotPath));
        }

        private void TestVert_Click_1(object sender, RoutedEventArgs e)
        {
            Console.WriteLine(ActiveStoryboards.Count);
            foreach (var story in ActiveStoryboards)
            {
                story.SetSpeedRatio(zoomcontrol, 5);
            }
        }

        private void TestVert_Click(object sender, RoutedEventArgs e)
        {
        }
    }
}
