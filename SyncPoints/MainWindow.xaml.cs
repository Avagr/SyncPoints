using GraphX.Controls;
using GraphX.PCL.Common;
using GraphX.PCL.Common.Enums;
using GraphX.PCL.Logic.Algorithms.OverlapRemoval;
using LiveCharts;
using LiveCharts.Configurations;
using Microsoft.Win32;
using ModernWpf;
using ModernWpf.Controls;
using QuickGraph;
using QuickGraph.Collections;
using QuickGraph.Serialization;
using SyncPointsLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        /// <summary>
        /// Determines the type of sandpile model to use. 0 is standart, 1 is BTW and 2 is Oslo
        /// </summary>
        public int UseSandpileModel { get => useSandpileModel; set { useSandpileModel = value; OnPropertyChanged("UseSandpileModel"); } }

        public bool DisableDots { get; set; }

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Charting

        public List<PointValues> BlueChartValues { get; set; }
        public List<PointValues> GreenChartValues { get; set; }

        public bool IsReading { get; set; }

        void InitializeChart()
        {
            var mapper = Mappers.Xy<PointValues>()
            .X(model => model.TimeElapsed.Ticks)   //use TimeElapsed.Ticks as X
            .Y(model => model.PointNumber);
            Charting.For<PointValues>(mapper);
            BlueChartValues = new List<PointValues>();
            GreenChartValues = new List<PointValues>();
            IsReading = false;
        }

        private void Read()
        {
            while (IsReading)
            {
                Thread.Sleep(500);

                BlueChartValues.Add(new PointValues
                {
                    TimeElapsed = Stats.TimeElapsed,
                    PointNumber = Stats.CurrentBlueDotCount
                });
                GreenChartValues.Add(new PointValues
                {
                    TimeElapsed = Stats.TimeElapsed,
                    PointNumber = Stats.CurrentGreenDotCount
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
            StartingEdgesGreen = new List<WeightedEdge>();
            InitializeComponent();
            graphArea.LogicCore = GenerateLogicCore();
            ZoomControl.SetViewFinderVisibility(zoomcontrol, Visibility.Collapsed);
            ThemeManager.Current.AccentColor = Colors.RoyalBlue;
            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
            NewGraphButtonEnabled = true;
            LoadGraphButtonEnabled = true;
        }

        #region Edge animation

        private int totalDotCount;

        private HashSet<WeightedEdge> BlueActiveEdges;
        private HashSet<WeightedEdge> GreenActiveEdges;

        /// <summary>
        /// Animates a single edge of the graph
        /// </summary>
        /// <param name="edge"> Edge to animate</param>
        /// <returns> A DotAnimation class that contains the Path and Storyboard of an animation</returns>
        private Storyboard InvisibleAnimateEdge(WeightedEdge edge, bool isBlue)
        {
            if (isBlue)
            {
                Stats.BlueDotCount++;
                Stats.CurrentBlueDotCount++;
            }
            else
            {
                Stats.GreenDotCount++;
                Stats.CurrentGreenDotCount++;
            }
            totalDotCount++;
            string dotName = "dot" + totalDotCount; // Naming the object with a unique ID
            FrameworkElement empty = new FrameworkElement
            {
                Visibility = Visibility.Collapsed,
                Height = 0
            };
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
            if (isBlue)
            {
                BlueActiveEdges.Add(edge);
                if (GreenActiveEdges.Contains(edge)) Stats.ColorMeetings++;
            }
            else
            {
                GreenActiveEdges.Add(edge);
                if (BlueActiveEdges.Contains(edge)) Stats.ColorMeetings++;
            }
            animStoryboard.Completed += (object e, EventArgs args) =>
            {
                if (!isStopping)
                {
                    if (!isBlue) GreenActiveEdges.Remove(edge);
                    else BlueActiveEdges.Remove(edge);
                    if (isBlue)
                    {
                        Stats.CurrentBlueDotCount--;
                        edge.Target.BlueSync--;
                        Stats.vertexStatistics[edge.Target].BlueDotsIn++;
                        Stats.vertexStatistics[edge.Target].BlueSyncHistory.Add(edge.Target.BlueSync);
                    }
                    else
                    {
                        Stats.CurrentGreenDotCount--;
                        edge.Target.GreenSync--;
                        Stats.vertexStatistics[edge.Target].GreenDotsIn++;
                        Stats.vertexStatistics[edge.Target].GreenSyncHistory.Add(edge.Target.GreenSync);
                    }
                    Stats.DistanceTravelled += edge.Weight;
                    ActiveStoryboards.Remove(animStoryboard);
                    if (ActiveStoryboards.Count == 0 && !((edge.Target.BlueSync < 1 || edge.Target.GreenSync < 1) && !graph.IsOutEdgesEmpty(edge.Target)))
                    {
                        StopAnimation();
                        HighlightDeadEnds();
                        return;
                    }
                    if ((edge.Target.BlueSync < 1 || edge.Target.GreenSync < 1) && !graph.IsOutEdgesEmpty(edge.Target))
                    {
                        if (!isPaused)
                        {
                            int newSync;
                            if (UseSandpileModel == 2)
                            {
                                newSync = rnd.Next(Math.Max(1, edge.Target.InitSync - 3), edge.Target.InitSync + 2);
                                if (edge.Target.InitSync < newSync)
                                {
                                    edge.Target.BlueSync += newSync - edge.Target.InitSync;
                                    edge.Target.GreenSync += newSync - edge.Target.InitSync;
                                }
                            }
                            else newSync = edge.Target.InitSync;
                            foreach (var outEdge in graph.OutEdges(edge.Target))
                            {
                                if (UseSandpileModel > 0)
                                {
                                    if (isBlue)
                                    {
                                        if (edge.Target.BlueSync >= newSync) break;
                                        edge.Target.BlueSync++;
                                    }
                                    else
                                    {
                                        if (edge.Target.GreenSync >= newSync) break;
                                        edge.Target.GreenSync++;
                                    }
                                }
                                if (isStopping) break;
                                Storyboard story = InvisibleAnimateEdge(outEdge, isBlue);
                                ActiveStoryboards.Add(story);
                                if (isStopping) break;
                                if (isBlue) Stats.vertexStatistics[outEdge.Source].BlueDotsOut++;
                                else Stats.vertexStatistics[outEdge.Source].GreenDotsOut++;
                                zoomcontrol.BeginStoryboard(story, HandoffBehavior.SnapshotAndReplace, true);
                                story.SetSpeedRatio(zoomcontrol, Math.Exp(ExpConst * AnimationSpeed));
                            }
                        }
                        else foreach (var outEdge in graph.OutEdges(edge.Target)) QueuedAnimations.Add(AnimateEdge(edge, isBlue));
                        if (UseSandpileModel == 0)
                        {
                            if (isBlue)
                            {
                                edge.Target.ResetBlueSync();
                                Stats.vertexStatistics[edge.Target].BlueSyncHistory.Add(edge.Target.BlueSync);
                            }
                            else
                            {
                                edge.Target.ResetGreenSync();
                                Stats.vertexStatistics[edge.Target].GreenSyncHistory.Add(edge.Target.GreenSync);
                            }
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
        private DotAnimation AnimateEdge(WeightedEdge edge, bool isBlue)
        {
            EdgeControl edgeControl = graphArea.EdgesList[edge];
            edgeControl.ManualDrawing = true;
            EllipseGeometry dot = new EllipseGeometry(new Point(0, 0), 7.5, 7.5); // 7.5
            if (isBlue)
            {
                Stats.BlueDotCount++;
                Stats.CurrentBlueDotCount++;
            }
            else
            {
                Stats.GreenDotCount++;
                Stats.CurrentGreenDotCount++;
            }
            totalDotCount++;
            string dotName = "dot" + totalDotCount; // Naming the object with a unique ID
            RegisterName(dotName, dot);
            Path dotPath = new Path
            {
                Data = dot,
                Fill = Brushes.Blue
            };
            if (isBlue) dotPath.Fill = Brushes.Blue;
            else dotPath.Fill = new SolidColorBrush(Color.FromRgb(34, 139, 34));

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
            if (isBlue)
            {
                BlueActiveEdges.Add(edge);
                if (GreenActiveEdges.Contains(edge)) Stats.ColorMeetings++;
            }
            else
            {
                GreenActiveEdges.Add(edge);
                if (BlueActiveEdges.Contains(edge)) Stats.ColorMeetings++;
            }
            animStoryboard.Completed += (object e, EventArgs args) =>
            {
                if (!isStopping)
                {
                    mainPanel.Children.Remove(dotPath);
                    ActivePaths.Remove(dotPath);
                    if (!isBlue) GreenActiveEdges.Remove(edge);
                    else BlueActiveEdges.Remove(edge);
                    if (isBlue)
                    {
                        Stats.CurrentBlueDotCount--;
                        edge.Target.BlueSync--;
                        Stats.vertexStatistics[edge.Target].BlueDotsIn++;
                        Stats.vertexStatistics[edge.Target].BlueSyncHistory.Add(edge.Target.BlueSync);
                    }
                    else
                    {
                        Stats.CurrentGreenDotCount--;
                        edge.Target.GreenSync--;
                        Stats.vertexStatistics[edge.Target].GreenDotsIn++;
                        Stats.vertexStatistics[edge.Target].GreenSyncHistory.Add(edge.Target.GreenSync);
                    }
                    Stats.DistanceTravelled += edge.Weight;
                    ActiveStoryboards.Remove(animStoryboard);
                    if (ActiveStoryboards.Count == 0 && !((edge.Target.BlueSync < 1 || edge.Target.GreenSync < 1) && !graph.IsOutEdgesEmpty(edge.Target)))
                    {
                        StopAnimation();
                        HighlightDeadEnds();
                        return;
                    }
                    if ((edge.Target.BlueSync < 1 || edge.Target.GreenSync < 1) && !graph.IsOutEdgesEmpty(edge.Target))
                    {
                        if (!isPaused)
                        {
                            int newSync;
                            if (UseSandpileModel == 2)
                            {
                                newSync = rnd.Next(Math.Max(1, edge.Target.InitSync - 3), edge.Target.InitSync + 2);
                                if (edge.Target.InitSync < newSync)
                                {
                                    edge.Target.BlueSync += newSync - edge.Target.InitSync;
                                    edge.Target.GreenSync += newSync - edge.Target.InitSync;
                                }
                            }
                            else newSync = edge.Target.InitSync;
                            foreach (var outEdge in graph.OutEdges(edge.Target))
                            {
                                if (UseSandpileModel > 0)
                                {
                                    if (isBlue)
                                    {
                                        if (edge.Target.BlueSync >= newSync) break;
                                        edge.Target.BlueSync++;
                                    }
                                    else
                                    {
                                        if (edge.Target.GreenSync >= newSync) break;
                                        edge.Target.GreenSync++;
                                    }
                                }
                                if (isStopping) break;
                                DotAnimation anim = AnimateEdge(outEdge, isBlue);
                                if (!mainPanel.Children.Contains(anim.Path)) mainPanel.Children.Add(anim.Path);
                                ActiveStoryboards.Add(anim.Storyboard);
                                if (isStopping) break;
                                ActivePaths.Add(anim.Path);
                                if (isBlue) Stats.vertexStatistics[outEdge.Source].BlueDotsOut++;
                                else Stats.vertexStatistics[outEdge.Source].GreenDotsOut++;
                                zoomcontrol.BeginStoryboard(anim.Storyboard, HandoffBehavior.SnapshotAndReplace, true);
                                anim.Storyboard.SetSpeedRatio(zoomcontrol, Math.Exp(ExpConst * AnimationSpeed));
                            }
                        }
                        else foreach (var outEdge in graph.OutEdges(edge.Target)) QueuedAnimations.Add(AnimateEdge(edge, isBlue));
                        if (UseSandpileModel == 0)
                        {
                            if (isBlue)
                            {
                                edge.Target.ResetBlueSync();
                                Stats.vertexStatistics[edge.Target].BlueSyncHistory.Add(edge.Target.BlueSync);
                            }
                            else
                            {
                                edge.Target.ResetGreenSync();
                                Stats.vertexStatistics[edge.Target].GreenSyncHistory.Add(edge.Target.GreenSync);
                            }
                        }
                    }
                }
            };

            return (new DotAnimation(animStoryboard, dotPath));
        }

        #endregion

        #region Animation Control Panel

        private const double ExpConst = 0.321888; // A constant for speed transformations

        public double AnimationSpeed { get; set; } // Speed of the animation

        public string AnimationSpeedText
        {
            get
            {
                return "Speed: " + Math.Round(Math.Exp(ExpConst * AnimationSpeed), 2) + "x";
            }
        }

        private void ResumeButton_Click(object sender, RoutedEventArgs e)
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

        private void PauseButton_Click(object sender, RoutedEventArgs e)
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

        private void SpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
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
                vert.ResetBlueSync();
                vert.ResetGreenSync();
                vert.Background = Brushes.OrangeRed;
            }
            isStopping = false;
            isPaused = false;
            Stats = new StatisticsModule(graph);
            BlueActiveEdges = new HashSet<WeightedEdge>();
            GreenActiveEdges = new HashSet<WeightedEdge>();
            StartStopButton.Content = "Stop";
            int startBlueEdgeCount = 0;
            int startGreenEdgeCount = 0;
            if (DisableDots)
            {
                foreach (var edge in StartingEdgesBlue)
                {
                    for (int i = 0; i < edge.BlueDotsCount; i++)
                    {
                        startBlueEdgeCount++;
                        Storyboard story = InvisibleAnimateEdge(edge, true);
                        Stats.vertexStatistics[edge.Source].BlueDotsOut++;
                        zoomcontrol.BeginStoryboard(story, HandoffBehavior.SnapshotAndReplace, true);
                        story.SetSpeedRatio(zoomcontrol, Math.Exp(ExpConst * AnimationSpeed));
                        ActiveStoryboards.Add(story);
                    }
                }
                foreach (var edge in StartingEdgesGreen)
                {
                    for (int i = 0; i < edge.GreenDotsCount; i++)
                    {
                        startGreenEdgeCount++;
                        Storyboard story = InvisibleAnimateEdge(edge, false);
                        Stats.vertexStatistics[edge.Source].GreenDotsOut++;
                        zoomcontrol.BeginStoryboard(story, HandoffBehavior.SnapshotAndReplace, true);
                        story.SetSpeedRatio(zoomcontrol, Math.Exp(ExpConst * AnimationSpeed));
                        ActiveStoryboards.Add(story);
                    }
                }
            }
            else
            {
                foreach (var edge in StartingEdgesBlue)
                {
                    for (int i = 0; i < edge.BlueDotsCount; i++)
                    {
                        startBlueEdgeCount++;
                        DotAnimation anim = AnimateEdge(edge, true);
                        Stats.vertexStatistics[edge.Source].BlueDotsOut++;
                        if (!mainPanel.Children.Contains(anim.Path)) mainPanel.Children.Add(anim.Path);
                        zoomcontrol.BeginStoryboard(anim.Storyboard, HandoffBehavior.SnapshotAndReplace, true);
                        anim.Storyboard.SetSpeedRatio(zoomcontrol, Math.Exp(ExpConst * AnimationSpeed));
                        ActiveStoryboards.Add(anim.Storyboard);
                        ActivePaths.Add(anim.Path);
                    }
                }
                foreach (var edge in StartingEdgesGreen)
                {
                    for (int i = 0; i < edge.GreenDotsCount; i++)
                    {
                        startGreenEdgeCount++;
                        DotAnimation anim = AnimateEdge(edge, false);
                        Stats.vertexStatistics[edge.Source].GreenDotsOut++;
                        if (!mainPanel.Children.Contains(anim.Path)) mainPanel.Children.Add(anim.Path);
                        zoomcontrol.BeginStoryboard(anim.Storyboard, HandoffBehavior.SnapshotAndReplace, true);
                        anim.Storyboard.SetSpeedRatio(zoomcontrol, Math.Exp(ExpConst * AnimationSpeed));
                        ActiveStoryboards.Add(anim.Storyboard);
                        ActivePaths.Add(anim.Path);
                    }
                }
            }
            StartChartReader();
            BlueChartValues.Clear();
            BlueChartValues.Add(new PointValues { PointNumber = startBlueEdgeCount, TimeElapsed = TimeSpan.FromSeconds(0) });
            GreenChartValues.Clear();
            GreenChartValues.Add(new PointValues { PointNumber = startGreenEdgeCount, TimeElapsed = TimeSpan.FromSeconds(0) });
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
            if ((Stats.CurrentBlueDotCount + Stats.CurrentGreenDotCount > 3000 && !DisableDots) || Stats.CurrentBlueDotCount + Stats.CurrentGreenDotCount > 5000)
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
            totalDotCount = 0;
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
            for (int i = 0; i < Stats.BlueDotCount + Stats.GreenDotCount; i++)
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
            else throw new NotImplementedException("Only triangles and squares are implemented");
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
                        var cascvert = new SyncVertex(vertindex, 1)
                        {
                            Background = Brushes.Transparent
                        };
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

        private void BuildGraphArea_VertexSelected(object sender, GraphX.Controls.Models.VertexSelectedEventArgs args)
        {
            if (GraphManParams.SelectedVertex != null) GraphManParams.SelectedVertex.Background = Brushes.OrangeRed;
            GraphManParams.SelectedVertex = (SyncVertex)args.VertexControl.Vertex;
            ((SyncVertex)args.VertexControl.Vertex).Background = Brushes.RoyalBlue;
        }

        private void BuildGraphArea_EdgeSelected(object sender, GraphX.Controls.Models.EdgeSelectedEventArgs args)
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
            StartingEdgesGreen = new List<WeightedEdge>();
            StartingEdgesBlue = new List<WeightedEdge>();
            UseSandpileModel = 0;
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

        private WeightedEdge selectedEdge;
        private int useSandpileModel;

        public List<WeightedEdge> StartingEdgesBlue { get; set; } // Edges that will contain a blue point at the start of the animation

        public List<WeightedEdge> StartingEdgesGreen { get; set; } // Edges that will contain a green point at the start of the animation

        public string StartingEdgeProbabilityString { get; set; }

        public WeightedEdge SelectedEdge
        {
            get => selectedEdge; set
            {
                selectedEdge = value;
                if (!StartingEdgesBlue.Contains(selectedEdge)) StartingEdgesBlue.Add(selectedEdge);
                if (!StartingEdgesGreen.Contains(selectedEdge)) StartingEdgesGreen.Add(selectedEdge);
                OnPropertyChanged("SelectedEdge");
            }
        } // Last clicked edge

        private void GenerateStartingEdges_Click(object sender, RoutedEventArgs e)
        {
            if (StartingEdgeProbabilityString != null && ValidateProbability(StartingEdgeProbabilityString, out double probability))
            {
                if (SelectedEdge != null) graphArea.EdgesList[SelectedEdge].Foreground = Brushes.Black;
                foreach (var edge in graph.Edges)
                {
                    if (1 - rnd.NextDouble() <= probability)
                    {
                        StartingEdgesBlue.Add(edge);
                        edge.BlueDotsCount++;
                    }
                    if (1 - rnd.NextDouble() <= probability)
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
                StartingEdgeProbabilityString = "";
                UseSandpileModel = 0;
            }
            else WrongSEParams.Text = "Invalid probability format";
        }

        private void GraphArea_EdgeSelected(object sender, GraphX.Controls.Models.EdgeSelectedEventArgs args)
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
            if (SelectedEdge != null) graphArea.EdgesList[SelectedEdge].Foreground = Brushes.Black;
            InitializeChart();
            mainPanel.IsHitTestVisible = false;
            WrongSEParams.Text = "";
            titleText.Text = "";
            UseSandpileModel = 0;
        }

        private async void ExportEdges_Click(object sender, RoutedEventArgs e)
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

        private async void ImportEdges_Click(object sender, RoutedEventArgs e)
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
                        if (!StartingEdgesBlue.Contains(edge)) StartingEdgesBlue.Add(edge);
                        if (!StartingEdgesGreen.Contains(edge)) StartingEdgesGreen.Add(edge);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Looks like something went wrong with " + ex.Message);
                }
            }
        }

        #endregion

        #region Graph stats panel

        /// <summary>
        /// The statistics module for the current simulation
        /// </summary>
        public StatisticsModule Stats { get => stats; set { stats = value; OnPropertyChanged("Stats"); } }

        private void ChartButton_Click(object sender, RoutedEventArgs e)
        {
            var chartWindow = new ChartWindow("Active points", BlueChartValues, GreenChartValues);
            chartWindow.Show();
        }

        private void ViewVertexStats_Click(object sender, RoutedEventArgs e)
        {
            var statsWindow = new VertexStatsWindow(Stats.vertexStatistics);
            statsWindow.Show();
        }

        private async void ExportStats_Click(object sender, RoutedEventArgs e)
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

        private void NewGraphButton_Click(object sender, RoutedEventArgs e)
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
            StartingEdgeProbabilityString = null;
            WrongParams.Text = "";
            WrongSEParams.Text = "";
            mainPanel.IsHitTestVisible = true;
        }

        private void LoadGraphButton_Click(object sender, RoutedEventArgs e)
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
                        vert.ResetBlueSync();
                        vert.ResetGreenSync();
                    }
                    InitializeGraphArea();
                    mainPanel.IsHitTestVisible = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Looks like something went wrong with " + ex.Message);
                }
            }
        }

        private void SaveGraphButton_Click(object sender, RoutedEventArgs e)
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

        private void InfoButton_Click(object sender, RoutedEventArgs e)
        {
            var infoWindow = new InfoWindow();
            infoWindow.Show();
        }

        #endregion
    }
}
