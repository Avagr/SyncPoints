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
using System.Windows.Shapes;
using System.Windows.Media.Animation;
using GraphX.Controls;

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
        BidirectionalGraph<object, IEdge<object>> graph;

        public MainWindow()
        {
            this.DataContext = this;
            NameScope.SetNameScope(this, new NameScope());
            InitializeComponent();
        }


        private void TestDot_Click(object sender, RoutedEventArgs e)
        {
            var edge = (GraphToVisualize.OutEdge(GraphToVisualize.Vertices.ToList()[0], 0));
            //var edgeControl = graphLayout.GetEdgeControl(edge);
            //edgeControl.Foreground = Brushes.Red;
            //var edgePoints = GetPointsFromEdgeControl(edgeControl);
            EllipseGeometry ellipse = new EllipseGeometry(new Point(0,0), 5, 5);
            if (this.FindName("movingPoint") != null) this.UnregisterName("movingPoint");
            this.RegisterName("movingPoint", ellipse);
            Path pointPath = new Path();
            pointPath.Data = ellipse;
            pointPath.Fill = Brushes.Blue;

            //mainPanel.Children.Add(pointPath);

            PathGeometry animationPath = new PathGeometry();
            PathFigure fig = new PathFigure();
            //fig.StartPoint = edgePoints.Item1;
            //fig.Segments.Add(new LineSegment(edgePoints.Item2, false));
            animationPath.Figures.Add(fig);

            animationPath.Freeze();

            PointAnimationUsingPath anim = new PointAnimationUsingPath();
            anim.PathGeometry = animationPath;
            anim.Duration = TimeSpan.FromSeconds(5);
            anim.RepeatBehavior = new RepeatBehavior(1);

            Storyboard.SetTargetName(anim, "movingPoint");
            Storyboard.SetTargetProperty(anim,
                new PropertyPath(EllipseGeometry.CenterProperty));

            Storyboard pathAnimationStoryboard = new Storyboard();
            pathAnimationStoryboard.RepeatBehavior = new RepeatBehavior(1);
            pathAnimationStoryboard.Children.Add(anim);

            pointPath.Loaded += delegate (object send, RoutedEventArgs ee)
            {
                // Start the storyboard.
                pathAnimationStoryboard.Begin(this);
            };
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

        }
    }
}
