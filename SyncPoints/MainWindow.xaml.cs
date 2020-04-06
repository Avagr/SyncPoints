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
using System.Windows.Shapes;
using System.Windows.Media.Animation;

namespace SyncPoints
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public IBidirectionalGraph<object, IEdge<object>> GraphToVisualize { get; set; }
        static Random rnd = new Random();

        public MainWindow()
        {
            this.DataContext = this;
            NameScope.SetNameScope(this, new NameScope());
            CreateGraphToVisualize();
            InitializeComponent();
        }

        private void CreateGraphToVisualize()
        {
            var graph = new BidirectionalGraph<object, IEdge<object>>();

            SyncVertex[] vert = new SyncVertex[10];
            for (int i = 0; i < 10; i++)
            {
                vert[i] = new SyncVertex(i, 1);
                graph.AddVertex(vert[i]);
            }
            int limit;
            for (int i = 0; i < 10; i++)
            {
                limit = rnd.Next(1, 5);
                for (int j = 0; j < limit; j++)
                {
                    graph.AddEdge(new WeightedEdge<object>(vert[i], vert[rnd.Next(10)], 3));
                }
            }
            GraphToVisualize = graph;
        }

        private void TestDot_Click(object sender, RoutedEventArgs e)
        {
            var edge = (GraphToVisualize.OutEdge(GraphToVisualize.Vertices.ToList()[0], 0));
            var edgeControl = graphLayout.GetEdgeControl(edge);
            edgeControl.Foreground = Brushes.Red;
            var edgePoints = GetPointsFromEdgeControl(edgeControl);
            EllipseGeometry ellipse = new EllipseGeometry(edgePoints.Item1, 5, 5);
            if (this.FindName("movingPoint") != null) this.UnregisterName("movingPoint");
            this.RegisterName("movingPoint", ellipse);
            Path pointPath = new Path();
            pointPath.Data = ellipse;
            pointPath.Fill = Brushes.Blue;
            pointPath.Margin = new Thickness(15);

            mainPanel.Children.Add(pointPath);

            PathGeometry animationPath = new PathGeometry();
            PathFigure fig = new PathFigure();
            fig.StartPoint = edgePoints.Item1;
            fig.Segments.Add(new LineSegment(edgePoints.Item2, false));
            animationPath.Figures.Add(fig);

            //animationPath.Freeze();

            PointAnimationUsingPath anim = new PointAnimationUsingPath();
            anim.PathGeometry = animationPath;
            anim.Duration = TimeSpan.FromSeconds(5);
            anim.RepeatBehavior = RepeatBehavior.Forever;

            Storyboard.SetTargetName(anim, "movingPoint");
            Storyboard.SetTargetProperty(anim,
                new PropertyPath(EllipseGeometry.CenterProperty));

            Storyboard pathAnimationStoryboard = new Storyboard();
            pathAnimationStoryboard.RepeatBehavior = RepeatBehavior.Forever;
            pathAnimationStoryboard.AutoReverse = true;
            pathAnimationStoryboard.Children.Add(anim);

            pointPath.Loaded += delegate (object send, RoutedEventArgs ee)
            {
                // Start the storyboard.
                pathAnimationStoryboard.Begin(this);
            };
        }

        public static (Point, Point) GetPointsFromEdgeControl(EdgeControl edge)
        {

            Point source = new Point(GraphLayout.GetX(edge.Source), GraphLayout.GetY(edge.Source));
            Point target = new Point(GraphLayout.GetX(edge.Target), GraphLayout.GetY(edge.Target));
            return (source, target);
        }
    }
}
