using GraphX.Controls;
using GraphX.PCL.Common.Enums;
using GraphX.PCL.Logic.Algorithms.LayoutAlgorithms;
using GraphX.PCL.Logic.Algorithms.OverlapRemoval;
using ModernWpf.Controls.Primitives;
using QuickGraph;
using SyncPointsLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private const double ExpConst = 0.321888; // A constant for speed transformations
        static Random rnd = new Random();

        BidirectionalGraph<SyncVertex, WeightedEdge> graph;

        List<Storyboard> ActiveStoryboards { get; set; }

        List<DotAnimation> QueuedAnimations { get; set; }

        List<WeightedEdge> StartingEdges { get; set; }

        int animCount, dotCount;

        bool isPaused = false;

        public double AnimationSpeed { get; set; }

        public string AnimationSpeedText
        {
            get
            {
                return "Speed: " + Math.Round(Math.Exp(ExpConst * AnimationSpeed), 3) + "x";
            }
        }

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public MainWindow()
        {
            //NameScope.SetNameScope(this, new NameScope());
            this.DataContext = this;
            InitializeComponent();
            CreateGraph();
            ZoomControl.SetViewFinderVisibility(zoomcontrol, Visibility.Collapsed);
            StartingEdges = new List<WeightedEdge>();
            ActiveStoryboards = new List<Storyboard>();
            QueuedAnimations = new List<DotAnimation>();
            AnimationSpeed = 0;
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
            LogicCore.DefaultLayoutAlgorithm = LayoutAlgorithmTypeEnum.KK;
            LogicCore.DefaultLayoutAlgorithmParams =
                               LogicCore.AlgorithmFactory.CreateLayoutParameters(LayoutAlgorithmTypeEnum.KK);
            ((KKLayoutParameters)LogicCore.DefaultLayoutAlgorithmParams).MaxIterations = 1000;
            LogicCore.DefaultOverlapRemovalAlgorithm = OverlapRemovalAlgorithmTypeEnum.FSA;
            LogicCore.DefaultOverlapRemovalAlgorithmParams =
                              LogicCore.AlgorithmFactory.CreateOverlapRemovalParameters(OverlapRemovalAlgorithmTypeEnum.FSA);
            ((OverlapRemovalParameters)LogicCore.DefaultOverlapRemovalAlgorithmParams).HorizontalGap = 50;
            ((OverlapRemovalParameters)LogicCore.DefaultOverlapRemovalAlgorithmParams).VerticalGap = 50;
            LogicCore.DefaultEdgeRoutingAlgorithm = EdgeRoutingAlgorithmTypeEnum.SimpleER;
            LogicCore.AsyncAlgorithmCompute = false;
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
            animation.Duration = TimeSpan.FromSeconds(edge.Weight * 0.5);
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
                    if (!isPaused)
                    {
                        foreach (var outEdge in graph.OutEdges(edge.Target))
                        {
                            DotAnimation anim = AnimateEdge(outEdge);
                            if (!mainPanel.Children.Contains(anim.Path)) mainPanel.Children.Add(anim.Path);
                            ActiveStoryboards.Add(anim.Storyboard);
                            zoomcontrol.BeginStoryboard(anim.Storyboard, HandoffBehavior.SnapshotAndReplace, true);
                            anim.Storyboard.SetSpeedRatio(zoomcontrol, Math.Exp(ExpConst * AnimationSpeed));
                        }
                    }
                    else foreach (var outEdge in graph.OutEdges(edge.Target)) QueuedAnimations.Add(AnimateEdge(edge));
                    edge.Target.ResetSync();
                }
            };

            return (new DotAnimation(animStoryboard, dotPath));
        }

        private void resumeButton_Click(object sender, RoutedEventArgs e)
        {
            if (isPaused)
            {
                foreach (var anim in QueuedAnimations)
                {
                    if (!mainPanel.Children.Contains(anim.Path)) mainPanel.Children.Add(anim.Path);
                    ActiveStoryboards.Add(anim.Storyboard);
                    zoomcontrol.BeginStoryboard(anim.Storyboard, HandoffBehavior.SnapshotAndReplace, true);
                }
                isPaused = false;
                foreach (var story in ActiveStoryboards)
                {
                    story.Resume(zoomcontrol);
                }
            }
        }

        private void speedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            OnPropertyChanged("AnimationSpeedText");
            foreach (var story in ActiveStoryboards)
            {
                Console.WriteLine(story.SpeedRatio);
                story.SetSpeedRatio(zoomcontrol, Math.Exp(ExpConst * AnimationSpeed));
                Console.WriteLine(story.SpeedRatio + "NEW");
            }
        }

        private void pauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isPaused)
            {
                isPaused = true;
                foreach (var story in ActiveStoryboards)
                {
                    story.Pause(zoomcontrol);
                }
            }
        }
    }
}
