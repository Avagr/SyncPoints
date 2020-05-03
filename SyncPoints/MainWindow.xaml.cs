using GraphX.Controls;
using GraphX.PCL.Common;
using GraphX.PCL.Common.Enums;
using GraphX.PCL.Logic.Algorithms.LayoutAlgorithms;
using GraphX.PCL.Logic.Algorithms.OverlapRemoval;
using LiveCharts;
using LiveCharts.Configurations;
using Microsoft.Win32;
using ModernWpf;
using ModernWpf.Controls;
using QuickGraph;
using QuickGraph.Serialization;
using SyncPointsLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Xml;
using Path = System.Windows.Shapes.Path;

namespace SyncPoints
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region Fields and properties

        public event PropertyChangedEventHandler PropertyChanged;

        private const double ExpConst = 0.321888; // A constant for speed transformations
        static Random rnd = new Random();

        BidirectionalGraph<SyncVertex, WeightedEdge> graph;

        List<Storyboard> ActiveStoryboards { get; set; } // Storyboards that are active at the time

        List<Path> ActivePaths { get; set; }

        List<DotAnimation> QueuedAnimations { get; set; } // Animations that should be queued when the model is paused

        private bool isPaused = false;
        private bool isStopping = false;
        private bool animationStarted = false;
        private DateTime pauseTime;
        private StatisticsModule stats;
        private bool newGraphButtonEnabled;
        private bool loadGraphButtonEnabled;
        private bool saveGraphButtonEnabled;

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

        /// <summary>
        /// The statistics module for the current simulation
        /// </summary>
        public StatisticsModule Stats { get => stats; set { stats = value; OnPropertyChanged("Stats"); } }

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

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
            SaveGraphButtonEnabled = false;
            this.DataContext = this;
            ActiveStoryboards = new List<Storyboard>();
            ActivePaths = new List<Path>();
            QueuedAnimations = new List<DotAnimation>();
            AnimationSpeed = 0;
            GraphGenParams = new GraphGenerationParams();
            LatticeGenParams = new LatticeGenParams();
            GraphManParams = new GraphManualParams();
            StartingEdgesBlue = new List<WeightedEdge>();
            InitializeComponent();
            graphArea.LogicCore = GenerateLogicCore();
            ZoomControl.SetViewFinderVisibility(zoomcontrol, Visibility.Collapsed);
            ThemeManager.Current.AccentColor = Colors.RoyalBlue;
            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
            NewGraphButtonEnabled = true;
            LoadGraphButtonEnabled = true;
        }

        private void TestDot_Click(object sender, RoutedEventArgs e)
        {
            buildGraphArea.GenerateGraph(new BidirectionalGraph<SyncVertex, WeightedEdge>());
        }

        #region Edge animation

        /// <summary>
        /// Animates a single edge of the graph
        /// </summary>
        /// <param name="edge"> Edge to animate</param>
        /// <returns> A DotAnimation class that contains the Path and Storyboard of an animation</returns>
        private Storyboard InvisibleAnimateEdge(WeightedEdge edge)
        {
            Stats.DotCount++;
            Stats.CurrentDotCount++;
            string dotName = "dot" + Stats.DotCount; // Naming the object with a unique ID
            FrameworkElement empty = new FrameworkElement();
            empty.Visibility = Visibility.Collapsed;
            empty.Height = 0;
            var parent = VisualTreeHelper.GetParent(empty);
            this.RegisterName(dotName, empty);
            DoubleAnimation animation = new DoubleAnimation(1, TimeSpan.FromSeconds(edge.Weight * 0.5));
            Storyboard.SetTargetName(animation, dotName);
            Storyboard.SetTargetProperty(animation, new PropertyPath(FrameworkElement.HeightProperty));
            empty = null;
            Storyboard animStoryboard = new Storyboard();
            animation.RepeatBehavior = new RepeatBehavior(1);
            animStoryboard.Children.Add(animation);
            this.RegisterName(dotName + "Storyboard", animStoryboard);
            CheckDotCount(null, null);
            animStoryboard.RepeatBehavior = new RepeatBehavior(1); // Repeats one
            animStoryboard.Completed += (object e, EventArgs args) =>
            {
                if (!isStopping)
                {
                    Stats.CurrentDotCount--;
                    edge.Target.Sync--;
                    Stats.VertexStatistics[edge.Target].DotsIn++;
                    Stats.VertexStatistics[edge.Target].DecreaseSync();
                    Stats.DistanceTravelled += edge.Weight;
                    ActiveStoryboards.Remove(animStoryboard);
                    if (ActiveStoryboards.Count == 0)
                    {
                        StopAnimation();
                        HighlightDeadEnds();
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
                                    if (edge.Target.Sync >= edge.Target.InitSync) break;
                                }
                                if (isStopping) break;
                                Storyboard story = InvisibleAnimateEdge(outEdge);
                                ActiveStoryboards.Add(story);
                                if (isStopping) break;
                                Stats.VertexStatistics[outEdge.Source].DotsOut++;
                                zoomcontrol.BeginStoryboard(story, HandoffBehavior.SnapshotAndReplace, true);
                                story.SetSpeedRatio(zoomcontrol, Math.Exp(ExpConst * AnimationSpeed));
                            }
                        }
                        //else foreach (var outEdge in graph.OutEdges(edge.Target)) QueuedAnimations.Add(AnimateEdge(edge));
                        if (!UseSandpileModel)
                        {
                            edge.Target.ResetSync();
                            Stats.VertexStatistics[edge.Target].SyncHistory.Add(edge.Target.Sync);
                        }
                    }
                }
            };

            return animStoryboard;
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
            EllipseGeometry dot = new EllipseGeometry(new Point(0, 0), 7.5, 7.5); // 7.5
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
                    Stats.DistanceTravelled += edge.Weight;
                    ActiveStoryboards.Remove(animStoryboard);
                    if (ActiveStoryboards.Count == 0)
                    {
                        StopAnimation();
                        HighlightDeadEnds();
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
                                    if (edge.Target.Sync >= edge.Target.InitSync) break;
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

        #endregion

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

        #region Animation starting and stopping

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
            OnPropertyChanged("AnimNotStarted");
            NewGraphButtonEnabled = false;
            LoadGraphButtonEnabled = false;
            SaveGraphButtonEnabled = false;
            // Cleaning all paths from the canvas, just in case
            foreach (var item in mainPanel.Children)
            {
                if (item is Path) ((UIElement)item).Visibility = Visibility.Collapsed;
            }
            foreach (var vert in graph.Vertices)
            {
                vert.ResetSync();
                vert.Background = Brushes.OrangeRed;
            }
            isStopping = false;
            isPaused = false;
            Stats = new StatisticsModule(graph);
            StartStopButton.Content = "Stop";
            int startEdgeCount = 0;
            if (DisableDots)
            {
                foreach (var edge in StartingEdgesBlue)
                {
                    startEdgeCount++;
                    Storyboard story = InvisibleAnimateEdge(edge);
                    Stats.VertexStatistics[edge.Source].DotsOut++;
                    zoomcontrol.BeginStoryboard(story, HandoffBehavior.SnapshotAndReplace, true);
                    story.SetSpeedRatio(zoomcontrol, Math.Exp(ExpConst * AnimationSpeed));
                    ActiveStoryboards.Add(story);
                }
            }
            else
            {
                foreach (var edge in StartingEdgesBlue)
                {
                    startEdgeCount++;
                    DotAnimation anim = AnimateEdge(edge);
                    Stats.VertexStatistics[edge.Source].DotsOut++;
                    if (!mainPanel.Children.Contains(anim.Path)) mainPanel.Children.Add(anim.Path);
                    zoomcontrol.BeginStoryboard(anim.Storyboard, HandoffBehavior.SnapshotAndReplace, true);
                    anim.Storyboard.SetSpeedRatio(zoomcontrol, Math.Exp(ExpConst * AnimationSpeed));
                    ActiveStoryboards.Add(anim.Storyboard);
                    ActivePaths.Add(anim.Path);

                }
            }
            StartChartReader();
            ChartValues.Clear();
            ChartValues.Add(new PointValues { PointNumber = startEdgeCount, TimeElapsed = TimeSpan.FromSeconds(0) });
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
        /// Checks whether the animation limit is exceeded
        /// </summary>
        private void CheckDotCount(object obj, EventArgs args)
        {
            if (Stats.CurrentDotCount > 2000 && !DisableDots || Stats.CurrentDotCount > 4000)
            {
                animationStarted = !animationStarted;
                OnPropertyChanged("AnimNotStarted"); // Disabling textboxes
                StopAnimation();
                MessageBox.Show("The animation had to be terminated due to an extremely high amount of dots on screen. Consider disabling the animations to save performace.");
            }
        }

        /// <summary>
        /// Stops the entire graph model
        /// </summary>
        private void StopAnimation()
        {
            animationStarted = false;
            OnPropertyChanged("AnimNotStarted");
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
            NewGraphButtonEnabled = true;
            LoadGraphButtonEnabled = true;
            SaveGraphButtonEnabled = true;
        }

        /// <summary>
        /// Highlights the dead ends of the graph
        /// </summary>
        private void HighlightDeadEnds()
        {
            Stats.FindDeadEnds();
            foreach (var vert in Stats.DeadEndVertices)
            {
                vert.Background = Brushes.Purple;
            }
        }

        #endregion

        #region Graph generation

        private GraphGenerationParams graphGenParams;
        private LatticeGenParams latticeGenParams;
        private GraphManualParams graphManParams;

        public GraphGenerationParams GraphGenParams { get => graphGenParams; set { graphGenParams = value; OnPropertyChanged("GraphGenParams"); } }
        public LatticeGenParams LatticeGenParams { get => latticeGenParams; set { latticeGenParams = value; OnPropertyChanged("LatticeGenParams"); } }
        public GraphManualParams GraphManParams { get => graphManParams; set { graphManParams = value; OnPropertyChanged("GraphManParams"); } }

        private void SelectGraphGenMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            graphGenPanel.Visibility = Visibility.Collapsed;
            graphBuildPanel.Visibility = Visibility.Collapsed;
            latticeGenPanel.Visibility = Visibility.Collapsed;
            if (buildGraphArea.LogicCore == null) buildGraphArea.LogicCore = GenerateLogicCore();
            buildGraphArea.Visibility = Visibility.Collapsed;
            switch (SelectGraphGenMode.SelectedIndex)
            {
                case 0: graphGenPanel.Visibility = Visibility.Visible; break;
                case 1:
                    buildGraphArea.Visibility = Visibility.Visible;
                    graphBuildPanel.Visibility = Visibility.Visible; break;
                case 2: latticeGenPanel.Visibility = Visibility.Visible; break;
            }
        }

        private void SelectLatticeNum_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SelectLatticeNum.SelectedIndex == 0) LatticeGenParams.PolygonEdgeNum = 3;
            else LatticeGenParams.PolygonEdgeNum = 4;
        }

        private void GenerateGraphButton_Click(object sender, RoutedEventArgs e)
        {
            if (!GraphGenParams.CheckIfFilled())
            {
                WrongParams.Text = "Invalid parameters. Every field must contain a valid value.";
                return;
            }
            if (!ValidateProbability(GraphGenParams.EdgeProbabilityString, out double tempProb))
            {
                WrongParams.Text = "Invalid probability format";
                return;
            }
            GraphGenParams.EdgeProbability = tempProb;
            WrongParams.Text = " ";
            //Create data graph object
            graph = new MyGraph();

            //Create and add vertices
            for (int i = 0; i < GraphGenParams.VertexCount; i++)
            {
                graph.AddVertex(new SyncVertex(i, rnd.Next(GraphGenParams.SyncLowerBound, GraphGenParams.SyncUpperBound + 1)));
            }

            var vlist = graph.Vertices.ToList();
            //Generate random edges for the vertices
            foreach (var item1 in vlist)
            {
                foreach (var item2 in vlist)
                {
                    if (item1 != item2 && 1 - rnd.NextDouble() <= GraphGenParams.EdgeProbability) graph.AddEdge(new WeightedEdge(item1, item2,
                        rnd.NextDouble() * (GraphGenParams.WeightUpperBound - GraphGenParams.WeightLowerBound) + GraphGenParams.WeightLowerBound));
                }
            }
            GenerateLogicCore();
            InitializeGraphArea();
        }

        private void GenerateLatticeGraph_Click(object sender, RoutedEventArgs e)
        {
            if (!LatticeGenParams.CheckIfFilled())
            {
                WrongLatticeParams.Text = "Invalid parameters. Every field must contain a valid value.";
                return;
            }
            WrongLatticeParams.Text = " ";
            int v = LatticeGenParams.VerticalTileCount;
            int h = LatticeGenParams.HorizontalTileCount;
            graph = new BidirectionalGraph<SyncVertex, WeightedEdge>();
            SyncVertex[,] lattice = new SyncVertex[v + 1, h + 1];
            if (LatticeGenParams.PolygonEdgeNum == 3)
            {
                for (int i = 0; i < lattice.GetLength(0); i++)
                {
                    for (int j = 0; j < lattice.GetLength(1) - i; j++)
                    {
                        lattice[i, j] = new SyncVertex(i + j, rnd.Next(LatticeGenParams.SyncLowerBound, LatticeGenParams.SyncUpperBound + 1));
                        graph.AddVertex(lattice[i, j]);
                        if (i != 0)
                        {
                            if (rnd.Next(2) == 0) graph.AddEdge(new WeightedEdge(lattice[i, j], lattice[i - 1, j], 1.5));
                            else graph.AddEdge(new WeightedEdge(lattice[i - 1, j], lattice[i, j], 1.5));
                            if (rnd.Next(2) == 0) graph.AddEdge(new WeightedEdge(lattice[i - 1, j + 1], lattice[i, j], 1.5));
                            else graph.AddEdge(new WeightedEdge(lattice[i, j], lattice[i - 1, j + 1], 1.5));
                        }
                        if (j != 0)
                        {
                            if (rnd.Next(2) == 0) graph.AddEdge(new WeightedEdge(lattice[i, j], lattice[i, j - 1], 1.5));
                            else graph.AddEdge(new WeightedEdge(lattice[i, j - 1], lattice[i, j], 1.5));
                        }
                    }
                }
            }
            else if (LatticeGenParams.PolygonEdgeNum == 4)
            {
                for (int i = 0; i < lattice.GetLength(0); i++)
                {
                    for (int j = 0; j < lattice.GetLength(1); j++)
                    {
                        lattice[i, j] = new SyncVertex(i + j, rnd.Next(LatticeGenParams.SyncLowerBound, LatticeGenParams.SyncUpperBound + 1));
                        graph.AddVertex(lattice[i, j]);
                        if (i != 0)
                        {
                            if (rnd.Next(2) == 0) graph.AddEdge(new WeightedEdge(lattice[i, j], lattice[i - 1, j], 1.5));
                            else graph.AddEdge(new WeightedEdge(lattice[i - 1, j], lattice[i, j], 1.5));
                        }
                        if (j != 0)
                        {
                            if (rnd.Next(2) == 0) graph.AddEdge(new WeightedEdge(lattice[i, j], lattice[i, j - 1], 1.5));
                            else graph.AddEdge(new WeightedEdge(lattice[i, j - 1], lattice[i, j], 1.5));
                        }
                    }
                }
            }
            else throw new NotImplementedException("WHAT?");
            int vertindex = graph.VertexCount;
            int compNumber; // Number to compare the amount of edges to
            if (LatticeGenParams.CreateBorderCascade)
            {
                if (LatticeGenParams.PolygonEdgeNum == 3) compNumber = 6;
                else compNumber = 4;
                foreach (var vert in graph.Vertices.ToList())
                {
                    if (graph.GetAllEdges(vert).Count() < compNumber)
                    {
                        var cascvert = new SyncVertex(vertindex, 1);
                        cascvert.Background = Brushes.Transparent;
                        graph.AddVertex(cascvert);
                        graph.AddEdge(new WeightedEdge(vert, cascvert, 0.1));
                    }
                }
            }
            InitializeGraphArea();
        }

        #region Manual Generation

        private void AddVertexButton_Click(object sender, RoutedEventArgs e)
        {
            if (GraphManParams.CreateVertexSync <= 0) return;
            GraphManParams.Graph.AddVertex(new SyncVertex(GraphManParams.nextVertexId, GraphManParams.CreateVertexSync));
            GraphManParams.nextVertexId++;
            buildGraphArea.GenerateGraph(GraphManParams.Graph);
        }

        private void buildGraphArea_VertexSelected(object sender, GraphX.Controls.Models.VertexSelectedEventArgs args)
        {
            if (GraphManParams.SelectedVertex != null) GraphManParams.SelectedVertex.Background = Brushes.OrangeRed;
            GraphManParams.SelectedVertex = (SyncVertex)args.VertexControl.Vertex;
            ((SyncVertex)args.VertexControl.Vertex).Background = Brushes.Blue;
        }

        private void buildGraphArea_EdgeSelected(object sender, GraphX.Controls.Models.EdgeSelectedEventArgs args)
        {
            if (GraphManParams.SelectedEdge != null) buildGraphArea.EdgesList[GraphManParams.SelectedEdge].Foreground = Brushes.Black;
            GraphManParams.SelectedEdge = (WeightedEdge)args.EdgeControl.Edge;
            args.EdgeControl.Foreground = Brushes.Red;
        }

        private void AddEdgeButton_Click(object sender, RoutedEventArgs e)
        {
            var g = GraphManParams.Graph;
            var source = (from v in g.Vertices where v.ID == GraphManParams.CreateEdgeSourceID select v).SingleOrDefault();
            var target = (from v in g.Vertices where v.ID == GraphManParams.CreateEdgeTargetID select v).SingleOrDefault();
            if (source != null && target != null && source != target && GraphManParams.CreateEdgeWeight > 0 && !g.ContainsEdge(source, target))
            {
                g.AddEdge(new WeightedEdge(source, target, GraphManParams.CreateEdgeWeight));
                buildGraphArea.GenerateGraph(GraphManParams.Graph);
            }
        }

        private void RemoveVertexButton_Click(object sender, RoutedEventArgs e)
        {
            buildGraphArea.RemoveVertexAndEdges(GraphManParams.SelectedVertex, EdgesType.All, true, true);
            GraphManParams.SelectedVertex = null;
        }

        private void RemoveEdgeButton_Click(object sender, RoutedEventArgs e)
        {
            buildGraphArea.RemoveEdge(GraphManParams.SelectedEdge, true);
            GraphManParams.SelectedEdge = null;
        }

        private void TextBox_KeyEnterUpdate(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TextBox tBox = (TextBox)sender;
                DependencyProperty prop = TextBox.TextProperty;

                BindingExpression binding = BindingOperations.GetBindingExpression(tBox, prop);
                if (binding != null) { binding.UpdateSource(); }
            }
        }

        private void FinishBuilding_Click(object sender, RoutedEventArgs e)
        {
            if (GraphManParams.SelectedVertex != null) GraphManParams.SelectedVertex.Background = Brushes.OrangeRed;
            graph = GraphManParams.Graph.Clone();
            buildGraphArea.GenerateGraph(null, true, true);
            buildGraphArea.Visibility = Visibility.Collapsed;
            InitializeGraphArea();
        }

        #endregion

        /// <summary>
        /// Generates a test graph for debugging purposes. Should not be used in a finished application
        /// </summary>
        private void GenerateTestGraph()
        {
            for (int i = 0; i < 15; i++)
            {
                graph.AddVertex(new SyncVertex(i, rnd.Next(3, 5 + 1)));
            }

            var vlist = graph.Vertices.ToList();
            //Generate random edges for the vertices
            foreach (var item1 in vlist)
            {
                foreach (var item2 in vlist)
                {
                    if (item1 != item2 && 1 - rnd.NextDouble() <= 0.4) graph.AddEdge(new WeightedEdge(item1, item2,
                        rnd.NextDouble() * (2) + 1));
                }
            }
            foreach (var edge in graph.Edges)
            {
                if (1 - rnd.NextDouble() <= 0.8) StartingEdgesBlue.Add(edge);
            }
        }

        /// <summary>
        /// Generates the logic core dor the graph
        /// </summary>
        private MyGXLogicCore GenerateLogicCore()
        {
            var LogicCore = new MyGXLogicCore
            {
                DefaultLayoutAlgorithm = LayoutAlgorithmTypeEnum.KK
            };
            //LogicCore.DefaultLayoutAlgorithmParams =
            //                   LogicCore.AlgorithmFactory.CreateLayoutParameters(LayoutAlgorithmTypeEnum.KK);
            //((KKLayoutParameters)LogicCore.DefaultLayoutAlgorithmParams).MaxIterations = 1000;
            LogicCore.DefaultOverlapRemovalAlgorithm = OverlapRemovalAlgorithmTypeEnum.FSA;
            LogicCore.DefaultOverlapRemovalAlgorithmParams =
                              LogicCore.AlgorithmFactory.CreateOverlapRemovalParameters(OverlapRemovalAlgorithmTypeEnum.FSA);
            ((OverlapRemovalParameters)LogicCore.DefaultOverlapRemovalAlgorithmParams).HorizontalGap = 50;
            ((OverlapRemovalParameters)LogicCore.DefaultOverlapRemovalAlgorithmParams).VerticalGap = 50;
            LogicCore.DefaultEdgeRoutingAlgorithm = EdgeRoutingAlgorithmTypeEnum.SimpleER;
            LogicCore.AsyncAlgorithmCompute = false;
            return LogicCore;
        }

        /// <summary>
        /// Initializes the GraphArea component and handles transition to selecting the starting edges
        /// </summary>
        private void InitializeGraphArea()
        {
            graphArea.GenerateGraph(graph, true, true);
            graphGenPanel.Visibility = Visibility.Collapsed;
            latticeGenPanel.Visibility = Visibility.Collapsed;
            graphBuildPanel.Visibility = Visibility.Collapsed;
            graphStatPanel.Visibility = Visibility.Collapsed;
            titleText.Visibility = Visibility.Visible;
            startingEdgePanel.Visibility = Visibility.Visible;
            SelectGraphGenMode.Visibility = Visibility.Collapsed;
            SelectedEdge = null;
            titleText.FontSize = 26;
            titleText.Text = "Select starting edges";
            titleText.HorizontalAlignment = HorizontalAlignment.Center;
            SaveGraphButtonEnabled = true;

        }



        /// <summary>
        /// Validates and parses the probability format
        /// </summary>
        /// <param name="input"> The string containing the probability</param>
        /// <param name="probability"> The probability to return</param>
        /// <returns> True if the probability is valid, false otherwise</returns>
        public bool ValidateProbability(string input, out double probability)
        {
            string[] splitArr = input.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (splitArr.Length == 2 && int.TryParse(splitArr[0], out int num) && int.TryParse(splitArr[1], out int denom) && denom != 0)
            {
                probability = (double)num / denom;
                if (probability <= 1 && probability >= 0) return true;
            }
            else if (double.TryParse(input, out probability) && probability <= 1 && probability >= 0) return true;
            probability = double.NaN;
            return false;
        }

        #endregion

        #region Starting edges

        private int timesToAddEdgeA;
        private int timesToAddEdgeB;
        private int timesToRemoveEdgeA;
        private int timesToRemoveEdgeB;
        private WeightedEdge selectedEdge;

        public List<WeightedEdge> StartingEdgesBlue { get; set; } // Edges that will contain a blue point at the start of the animation

        public List<WeightedEdge> StartingEdgesGreen { get; set; } // Edges that will contain a green point at the start of the animation

        public string StartingEdgeProbabilityString { get; set; }

        public WeightedEdge SelectedEdge { get => selectedEdge; set { selectedEdge = value; OnPropertyChanged("SelectedEdge"); } } // Last clicked edge

        public int TimesToAddEdgeA { get => timesToAddEdgeA; set { if (value > 0) timesToAddEdgeA = value; OnPropertyChanged("TimesToAddEdgeA"); } }
        public int TimesToAddEdgeB { get => timesToAddEdgeB; set { if (value > 0) timesToAddEdgeB = value; OnPropertyChanged("TimesToAddEdgeB"); } }
        public int TimesToRemoveEdgeA { get => timesToRemoveEdgeA; set { if (value > 0) timesToRemoveEdgeA = value; OnPropertyChanged("TimesToRemoveEdgeA"); } }
        public int TimesToRemoveEdgeB { get => timesToRemoveEdgeB; set { if (value > 0) timesToRemoveEdgeB = value; OnPropertyChanged("TimesToRemoveEdgeB"); } }

        private void GenerateStartingEdges_Click(object sender, RoutedEventArgs e)
        {
            if (StartingEdgeProbabilityString != null && ValidateProbability(StartingEdgeProbabilityString, out double probability))
            {
                foreach (var edge in graph.Edges)
                {
                    if (1 - rnd.NextDouble() <= probability)
                        if (rnd.Next(2) == 0)
                        {
                            StartingEdgesBlue.Add(edge);
                            edge.BlueDotsCount++;
                        }
                        else
                        {
                            StartingEdgesGreen.Add(edge);
                            edge.GreenDotsCount++;
                        }
                }
                startingEdgePanel.Visibility = Visibility.Collapsed;
                graphStatPanel.Visibility = Visibility.Visible;
                Stats = new StatisticsModule(graph);
                InitializeChart();
                mainPanel.IsHitTestVisible = false;
                WrongSEParams.Text = "";
                titleText.Text = "";
            }
            else WrongSEParams.Text = "Invalid probability format";
        }

        private void graphArea_EdgeSelected(object sender, GraphX.Controls.Models.EdgeSelectedEventArgs args)
        {
            if (SelectedEdge != null) graphArea.EdgesList[SelectedEdge].Foreground = Brushes.Black;
            SelectedEdge = (WeightedEdge)args.EdgeControl.Edge;
            args.EdgeControl.Foreground = Brushes.Red;
        }

        private void FinishStartingEdges_Click(object sender, RoutedEventArgs e)
        {
            startingEdgePanel.Visibility = Visibility.Collapsed;
            graphStatPanel.Visibility = Visibility.Visible;
            Stats = new StatisticsModule(graph);
            InitializeChart();
            mainPanel.IsHitTestVisible = false;
            WrongSEParams.Text = "";
            titleText.Text = "";
        }

        private async void exportEdges_Click(object sender, RoutedEventArgs e)
        {
            var edgeList = new List<StartingEdgeParams>();
            foreach (var edge in graph.Edges)
            {
                edgeList.Add(new StartingEdgeParams(edge.ID, edge.BlueDotsCount, edge.GreenDotsCount));
            }
            SaveFileDialog dialog = new SaveFileDialog
            {
                Filter = "JSON file (*.json)|*.json|Text file (*.txt)|*.txt",
                RestoreDirectory = true
            };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    using (FileStream fs = File.Create(dialog.FileName))
                    {
                        await JsonSerializer.SerializeAsync(fs, edgeList);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        /// <summary>
        /// An auxiliary class for serializing starting edges
        /// </summary>
        private class StartingEdgeParams
        {
            public StartingEdgeParams() { }

            public StartingEdgeParams(long iD, int blueDotsCount, int greenDotsCount)
            {
                ID = iD;
                BlueDotsCount = blueDotsCount;
                GreenDotsCount = greenDotsCount;
            }

            public long ID { get; set; }

            public int BlueDotsCount { get; set; }

            public int GreenDotsCount { get; set; }
        }

        private async void importEdges_Click(object sender, RoutedEventArgs e)
        {
            var edgeList = new List<StartingEdgeParams>();
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "JSON file (*.json)|*.json|Text file (*.txt)|*.txt",
                RestoreDirectory = true
            };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    using (FileStream fs = File.OpenRead(dialog.FileName))
                    {
                        edgeList = await JsonSerializer.DeserializeAsync<List<StartingEdgeParams>>(fs);
                    }
                    foreach (var edge in graph.Edges)
                    {
                        edge.BlueDotsCount = edgeList[(int)edge.ID - 1].BlueDotsCount;
                        edge.GreenDotsCount = edgeList[(int)edge.ID - 1].GreenDotsCount;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Looks like something went wrong with " + ex.Message);
                }
            }
        }

        #endregion

        #region Graph stats buttons

        private void chartButton_Click(object sender, RoutedEventArgs e)
        {
            var chartWindow = new ChartWindow("Active points", ChartValues);
            chartWindow.Show();
        }

        private void viewVertexStats_Click(object sender, RoutedEventArgs e)
        {
            var statsWindow = new VertexStatsWindow(Stats.VertexStatistics);
            statsWindow.Show();
        }

        private async void exportStats_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog
            {
                Filter = "JSON file (*.json)|*.json|Text file (*.txt)|*.txt",
                RestoreDirectory = true
            };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    using (FileStream fs = File.Create(dialog.FileName))
                    {
                        await JsonSerializer.SerializeAsync(fs, Stats);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Looks like something went wrong with " + ex.Message);
                }
            }
        }
        #endregion

        #region Left control panel buttons

        public bool NewGraphButtonEnabled { get => newGraphButtonEnabled; set { newGraphButtonEnabled = value; OnPropertyChanged("NewGraphButtonEnabled"); } }
        public bool LoadGraphButtonEnabled { get => loadGraphButtonEnabled; set { loadGraphButtonEnabled = value; OnPropertyChanged("LoadGraphButtonEnabled"); } }
        public bool SaveGraphButtonEnabled { get => saveGraphButtonEnabled; set { saveGraphButtonEnabled = value; OnPropertyChanged("SaveGraphButtonEnabled"); } }

        private void newGraphButton_Click(object sender, RoutedEventArgs e)
        {
            SaveGraphButtonEnabled = false;
            Flyout f = FlyoutService.GetFlyout(newGraphButton) as Flyout;
            f?.Hide();
            graph = new BidirectionalGraph<SyncVertex, WeightedEdge>();
            graphArea.GenerateGraph(graph, true, true);
            buildGraphArea.GenerateGraph(graph, true, true);
            graphGenPanel.Visibility = Visibility.Visible;
            graphStatPanel.Visibility = Visibility.Collapsed;
            graphBuildPanel.Visibility = Visibility.Collapsed;
            latticeGenPanel.Visibility = Visibility.Collapsed;
            titleText.Visibility = Visibility.Collapsed;
            startingEdgePanel.Visibility = Visibility.Collapsed;
            SelectGraphGenMode.SelectedIndex = 0;
            SelectGraphGenMode.Visibility = Visibility.Visible;
            Stats = null;
            SelectedEdge = null;
            GraphGenParams = new GraphGenerationParams();
            LatticeGenParams = new LatticeGenParams();
            GraphManParams = new GraphManualParams();
            mainPanel.IsHitTestVisible = true;
        }

        private void loadGraphButton_Click(object sender, RoutedEventArgs e)
        {
            Flyout f = FlyoutService.GetFlyout(loadGraphButton) as Flyout;
            f?.Hide();
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "XML file (GraphML Format) (*.xml)|*.xml",
                RestoreDirectory = true
            };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    graph = new BidirectionalGraph<SyncVertex, WeightedEdge>();
                    using (var xreader = XmlReader.Create(dialog.FileName))
                    {
                        graph.DeserializeFromGraphML(xreader, id => new SyncVertex(), (source, target, id) => new WeightedEdge(source, target));
                    }
                    foreach (var vert in graph.Vertices)
                    {
                        vert.ResetSync();
                    }
                    InitializeGraphArea();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Looks like something went wrong with " + ex.Message);
                }
            }
        }

        private void saveGraphButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog
            {
                Filter = "XML file (GraphML Format) (*.xml)|*.xml",
                RestoreDirectory = true
            };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    using (var xwriter = XmlWriter.Create(dialog.FileName))
                    {
                        graph.SerializeToGraphML<SyncVertex, WeightedEdge, BidirectionalGraph<SyncVertex, WeightedEdge>>(xwriter);

                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Looks like something went wrong with " + ex.Message);
                }
            }
        }

        private void infoButton_Click(object sender, RoutedEventArgs e)
        {
        }

        #endregion
    }
}
