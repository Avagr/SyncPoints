using GraphX.Controls;
using GraphX.PCL.Common.Enums;
using GraphX.PCL.Logic.Algorithms.LayoutAlgorithms;
using GraphX.PCL.Logic.Algorithms.OverlapRemoval;
using LiveCharts;
using LiveCharts.Configurations;
using ModernWpf;
using QuickGraph;
using SyncPointsLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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

        List<Storyboard> ActiveStoryboards { get; set; } // Storyboards that are active at the time

        List<Path> ActivePaths { get; set; }

        List<DotAnimation> QueuedAnimations { get; set; } // Animations that should be queued when the model is paused

        List<WeightedEdge> StartingEdges { get; set; } // Edges with the starting dots

        private bool isPaused = false;
        private bool isStopping = false;
        private bool generateButtonOn;
        private bool animationStarted = false;
        private DateTime pauseTime;
        private StatisticsModule stats;

        public bool AnimNotStarted { get => !animationStarted; }

        public bool UseSandpileModel { get; set; }

        public bool DisableDots { get; set; }

        public double AnimationSpeed { get; set; } // Speed of the animation

        public string AnimationSpeedText
        {
            get
            {
                return "Speed: " + Math.Round(Math.Exp(ExpConst * AnimationSpeed), 2) + "x";
            }
        }

        public GraphGenerationParams GraphGenParams { get; set; }

        public StatisticsModule Stats { get => stats; set { stats = value; OnPropertyChanged("Stats"); } }

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region ButtonBools

        public bool GenerateButtonOn { get => generateButtonOn; set { generateButtonOn = value; OnPropertyChanged("GenerateButtonOn"); } }

        #endregion ButtonBools

        #region Charting

        public List<PointValues> ChartValues { get; set; }

        public bool IsReading { get; set; }

        void InitializeChart()
        {
            var mapper = Mappers.Xy<PointValues>()
            .X(model => model.TimeElapsed.Ticks)   //use TimeElapsed.Ticks as X
            .Y(model => model.PointNumber);
            Charting.For<PointValues>(mapper);
            ChartValues = new List<PointValues>();

            IsReading = false;
        }

        private void Read()
        {
            while (IsReading)
            {
                Thread.Sleep(500);

                ChartValues.Add(new PointValues
                {
                    TimeElapsed = Stats.TimeElapsed,
                    PointNumber = Stats.CurrentDotCount
                });
            }
        }
        #endregion

        public MainWindow()
        {
            this.DataContext = this;
            StartingEdges = new List<WeightedEdge>();
            ActiveStoryboards = new List<Storyboard>();
            ActivePaths = new List<Path>();
            QueuedAnimations = new List<DotAnimation>();
            AnimationSpeed = 0;
            GraphGenParams = new GraphGenerationParams();
            InitializeComponent();
            ZoomControl.SetViewFinderVisibility(zoomcontrol, Visibility.Collapsed);
            ThemeManager.Current.AccentColor = Colors.RoyalBlue;
            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
            GenerateButtonOn = false;
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
            var chartWindow = new ChartWindow("Test Label", ChartValues);
            chartWindow.Show();
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
            Stats.DotCount++;
            Stats.CurrentDotCount++;
            string dotName = "dot" + Stats.DotCount; // Naming the object with a unique ID
            this.RegisterName(dotName, dot);
            Path dotPath = new Path
            {
                Data = dot,
                Fill = Brushes.Blue
            };

            PathGeometry animPath = edgeControl.GetEdgePathManually();
            animPath.Freeze();

            PointAnimationUsingPath animation = new PointAnimationUsingPath
            {
                PathGeometry = animPath,
                Duration = TimeSpan.FromSeconds(edge.Weight * 0.5),
                RepeatBehavior = new RepeatBehavior(1) // Repeats once
            };
            Storyboard.SetTargetName(animation, dotName);
            Storyboard.SetTargetProperty(animation, new PropertyPath(EllipseGeometry.CenterProperty)); // Animating the center of the dot

            Storyboard animStoryboard = new Storyboard();
            animStoryboard.Children.Add(animation);
            this.RegisterName(dotName + "Storyboard", animStoryboard);
            CheckDotCount(null, null);
            animStoryboard.RepeatBehavior = new RepeatBehavior(1); // Repeats one
            animStoryboard.Completed += (object e, EventArgs args) =>
            {
                if (!isStopping)
                {
                    mainPanel.Children.Remove(dotPath);
                    ActivePaths.Remove(dotPath);
                    Stats.CurrentDotCount--;
                    edge.Target.Sync--;
                    Stats.VertexStatistics[edge.Target].DotsIn++;
                    Stats.VertexStatistics[edge.Target].DecreaseSync();
                    ActiveStoryboards.Remove(animStoryboard);
                    if (ActiveStoryboards.Count == 0)
                    {
                        StopAnimation();
                        return;
                    }
                    if (edge.Target.Sync < 1 && !graph.IsOutEdgesEmpty(edge.Target))
                    {
                        if (!isPaused)
                        {
                            foreach (var outEdge in graph.OutEdges(edge.Target))
                            {
                                if (UseSandpileModel)
                                {
                                    edge.Target.Sync++;
                                    Stats.VertexStatistics[edge.Target].IncreaseSync();
                                    if (edge.Target.Sync >= edge.Target.initSync) break;
                                }
                                if (isStopping) break;
                                DotAnimation anim = AnimateEdge(outEdge);
                                if (!mainPanel.Children.Contains(anim.Path)) mainPanel.Children.Add(anim.Path);
                                ActiveStoryboards.Add(anim.Storyboard);
                                if (isStopping) break;
                                ActivePaths.Add(anim.Path);
                                Stats.VertexStatistics[outEdge.Source].DotsOut++;
                                zoomcontrol.BeginStoryboard(anim.Storyboard, HandoffBehavior.SnapshotAndReplace, true);
                                anim.Storyboard.SetSpeedRatio(zoomcontrol, Math.Exp(ExpConst * AnimationSpeed));
                            }
                        }
                        else foreach (var outEdge in graph.OutEdges(edge.Target)) QueuedAnimations.Add(AnimateEdge(edge));
                        if (!UseSandpileModel)
                        {
                            edge.Target.ResetSync();
                            Stats.VertexStatistics[edge.Target].SyncHistory.Add(edge.Target.Sync);
                        }
                    }
                }
            };

            return (new DotAnimation(animStoryboard, dotPath));
        }

        /// <summary>
        /// Checks whether the animation limit is exceeded
        /// </summary>
        private void CheckDotCount(object obj, EventArgs args)
        {
            if (Stats.CurrentDotCount > 2000)
            {
                animationStarted = !animationStarted;
                OnPropertyChanged("AnimNotStarted"); // Disabling textboxes
                StopAnimation();
            }
        }

        #region Animation Control Panel

        private void resumeButton_Click(object sender, RoutedEventArgs e)
        {
            if (isPaused)
            {
                Stats.PausedTime += DateTime.Now - pauseTime;
                foreach (var anim in QueuedAnimations)
                {
                    if (!mainPanel.Children.Contains(anim.Path)) mainPanel.Children.Add(anim.Path);
                    ActiveStoryboards.Add(anim.Storyboard);
                    zoomcontrol.BeginStoryboard(anim.Storyboard, HandoffBehavior.SnapshotAndReplace, true);
                }
                isPaused = false;
                StartChartReader();
                foreach (var story in ActiveStoryboards)
                {
                    story.Resume(zoomcontrol);
                }
            }
        }

        private void pauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isPaused)
            {
                pauseTime = DateTime.Now;
                IsReading = false;
                isPaused = true;
                foreach (var story in ActiveStoryboards)
                {
                    story.Pause(zoomcontrol);
                }
            }
        }

        private void speedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            OnPropertyChanged("AnimationSpeedText");
            foreach (var story in ActiveStoryboards)
            {
                story.SetSpeedRatio(zoomcontrol, Math.Exp(ExpConst * AnimationSpeed));
            }
        }

        #endregion

        #region Checking textbox validity - not needed right now
        private void CheckGenButton(object sender, TextChangedEventArgs e)
        {
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
        }
        #endregion

        private void StartStopButton_Click(object sender, RoutedEventArgs e)
        {
            animationStarted = !animationStarted;
            OnPropertyChanged("AnimNotStarted"); // Disabling textboxes
            if (animationStarted)
            {
                StartAnimation();
            }
            else
            {
                StopAnimation();
            }
        }

        /// <summary>
        /// Starts the animation
        /// </summary>
        private void StartAnimation()
        {
            animationStarted = true;
            // Cleaning all paths from the canvas, just in case
            foreach (var item in mainPanel.Children)
            {
                if (item is Path) ((UIElement)item).Visibility = Visibility.Collapsed;
            }
            foreach (var vert in graph.Vertices)
            {
                vert.ResetSync();
            }
            isStopping = false;
            isPaused = false;
            Stats = new StatisticsModule(graph);
            StartStopButton.Content = "Stop";
            foreach (var edge in StartingEdges)
            {
                DotAnimation anim = AnimateEdge(edge);
                Stats.VertexStatistics[edge.Source].DotsOut++;
                if (!mainPanel.Children.Contains(anim.Path)) mainPanel.Children.Add(anim.Path);
                zoomcontrol.BeginStoryboard(anim.Storyboard, HandoffBehavior.SnapshotAndReplace, true);
                ActiveStoryboards.Add(anim.Storyboard);
                ActivePaths.Add(anim.Path);
            }
            StartChartReader();
            ChartValues.Clear();
            ChartValues.Add(new PointValues { PointNumber = StartingEdges.Count, TimeElapsed = TimeSpan.FromSeconds(0) });
        }

        /// <summary>
        /// Starts the updater method
        /// </summary>
        private void StartChartReader()
        {
            IsReading = true;
            Task.Factory.StartNew(Read);
        }

        /// <summary>
        /// Stops the entire graph model
        /// </summary>
        private void StopAnimation()
        {
            animationStarted = false;
            IsReading = false;
            isStopping = true;
            isPaused = true;
            StartStopButton.Content = "Start";
            foreach (var anim in ActiveStoryboards)
            {
                anim.Stop(zoomcontrol);
                anim.Remove(zoomcontrol);
            }
            foreach (var path in ActivePaths)
            {
                mainPanel.Children.Remove(path);
            }
            for (int i = 0; i < Stats.DotCount; i++)
            {
                UnregisterName("dot" + (i + 1));
                UnregisterName("dot" + (i + 1) + "Storyboard");
            }
            foreach (var item in mainPanel.Children)
            {
                if (item is Path) ((UIElement)item).Visibility = Visibility.Collapsed;
            }
            graphArea.UpdateLayout();
            QueuedAnimations.Clear();
            ActiveStoryboards.Clear();
            ActivePaths.Clear();
        }

        private void GenerateGraphButton_Click(object sender, RoutedEventArgs e)
        {
            //if (!GraphGenParams.CheckIfFilled())
            //{
            //    WrongParams.Text = "Invalid parameters. Every field must contain a valid value.";
            //    return;
            //}
            WrongParams.Foreground = Brushes.RoyalBlue;
            WrongParams.Text = "Generating...";
            //Create data graph object
            graph = new MyGraph();

            ////Create and add vertices
            //for (int i = 0; i < GraphGenParams.VertexCount; i++)
            //{
            //    graph.AddVertex(new SyncVertex(i, rnd.Next(GraphGenParams.SyncLowerBound, GraphGenParams.SyncUpperBound + 1)));
            //}

            //var vlist = graph.Vertices.ToList();
            ////Generate random edges for the vertices
            //foreach (var item1 in vlist)
            //{
            //    foreach (var item2 in vlist)
            //    {
            //        if (item1 != item2 && 1 - rnd.NextDouble() <= GraphGenParams.EdgeProbability) graph.AddEdge(new WeightedEdge(item1, item2,
            //            rnd.NextDouble() * (GraphGenParams.WeightUpperBound - GraphGenParams.WeightLowerBound) + GraphGenParams.WeightLowerBound));
            //    }
            //}
            //GenerateLogicCore();
            //StartingEdges.Clear();
            //foreach (var edge in graph.Edges)
            //{
            //    if (1 - rnd.NextDouble() <= GraphGenParams.StartingEdgeProbability) StartingEdges.Add(edge);
            //}
            //Create and add vertices
            for (int i = 0; i < 20; i++)
            {
                graph.AddVertex(new SyncVertex(i, rnd.Next(6, 8 + 1)));
            }

            var vlist = graph.Vertices.ToList();
            //Generate random edges for the vertices
            foreach (var item1 in vlist)
            {
                foreach (var item2 in vlist)
                {
                    if (item1 != item2 && 1 - rnd.NextDouble() <= 0.35) graph.AddEdge(new WeightedEdge(item1, item2,
                        rnd.NextDouble() * (2) + 1));
                }
            }
            GenerateLogicCore();
            StartingEdges.Clear();
            foreach (var edge in graph.Edges)
            {
                if (1 - rnd.NextDouble() <= 0.8) StartingEdges.Add(edge);
            }
            graphArea.GenerateGraph(graph, true, true);
            graphGenPanel.Visibility = Visibility.Collapsed;
            graphStatPanel.Visibility = Visibility.Visible;
            createGraph.FontSize = 26;
            createGraph.Text = "";
            Stats = new StatisticsModule(graph);
            InitializeChart();
        }

        private void chartButton_Click(object sender, RoutedEventArgs e)
        {
            var chartWindow = new ChartWindow("Active points", ChartValues);
            chartWindow.Show();
        }
    }
}
