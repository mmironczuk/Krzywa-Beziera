using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Bezier
{
    public partial class MainWindow : Window
    {
        private List<Point> points;
        private short NumberOfSegments = 3;
        private int removingElement = 0;
        private Rectangle editedPoint = null;
        private Rectangle changedPoint = null;
        private bool isEdit = false;
        private int editX = 0;
        private int editY = 0;
        private Path lastLine = null;


        public MainWindow()
        {
            points = new List<Point>();
            InitializeComponent();
        }

        private void DegreeButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!short.TryParse(DegreeTB.Text, out NumberOfSegments)) MessageBox.Show("Podano złą wartość");
        }

        private void CanvasArea_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (isEdit)
            {
                var leftTop = VisualTreeHelper.GetOffset(changedPoint);

                points = DoChanges(points, (int)leftTop.X - 5, (int)leftTop.Y - 5);
                editX = (int)leftTop.X - 5;
                editY = (int)leftTop.Y - 5;

                DrawBezierLine(new Point(), false);

                isEdit = false;
                XChangeTextBox.Text = ((int)leftTop.X + 5).ToString();
                YChangeTextBox.Text = ((int)leftTop.Y + 5).ToString();
            }
        }

        private void CanvasArea_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
                DrawBezierLine(Mouse.GetPosition(CanvasArea));
            else
            {
                if (e.OriginalSource is Rectangle)
                {
                    isEdit = true;
                    editedPoint = e.OriginalSource as Rectangle;
                    changedPoint = e.OriginalSource as Rectangle;
                    var leftTop = VisualTreeHelper.GetOffset(editedPoint);

                    XChangeTextBox.Text = ((int)leftTop.X + 5).ToString();
                    YChangeTextBox.Text = ((int)leftTop.Y + 5).ToString();

                    editX = (int)leftTop.X + 5;
                    editY = (int)leftTop.Y + 5;
                }

            }
        }

        private void CanvasArea_OnMouseMove(object sender, MouseEventArgs e)
        {
            if (isEdit)
            {
                var mousePosition = Mouse.GetPosition(CanvasArea);

                Canvas.SetLeft(changedPoint, mousePosition.X);
                Canvas.SetTop(changedPoint, mousePosition.Y);
            }
        }

        private void AddButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (short.TryParse(XTextBox.Text, out var x) && short.TryParse(YTextBox.Text, out var y))
            {
                DrawBezierLine(new Point(x, y));
                XTextBox.Text = string.Empty;
                YTextBox.Text = string.Empty;
            }
            else
                MessageBox.Show("Podano złe wartości");
        }

        private void ChangeButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (points.Where(p => (int)p.X == editX && (int)p.Y == editY).FirstOrDefault() != null && Int32.TryParse(XChangeTextBox.Text, out var x) && Int32.TryParse(YChangeTextBox.Text, out var y))
            {
                var point = points.Where(p => (int)p.X == editX && (int)p.Y == editY).FirstOrDefault();

                point.X = x;
                point.Y = y;

                points = DoChanges(points, x, y);
                DrawBezierLine(new Point(), false);

                XChangeTextBox.Text = string.Empty;
                YChangeTextBox.Text = string.Empty;
                editedPoint = null;
            }
        }

        private void CleanButton_OnClick(object sender, RoutedEventArgs e)
        {
            ((Canvas)CanvasArea).Children.Clear();
            points.Clear();
        }


        private List<Point> DoChanges(List<Point> oldPoints, int x, int y)
        {
            var newPoints = new List<Point>();

            foreach (var point in oldPoints)
            {
                Console.WriteLine(point.Y + "dasdas");
                if ((int)point.X == editX && (int)point.Y == editY)
                {
                    newPoints.Add(new Point
                    {
                        X = x,
                        Y = y
                    });
                }
                else
                    newPoints.Add(point);
            }

            return newPoints;
        }


        private void DrawBezierLine(Point point = new Point(), bool newLine = true)
        {
            if (points.Count == NumberOfSegments && newLine)
            {
                MessageBox.Show("Nie można dodać większej ilości punktów");
                return;
            }

            if (newLine)
                points.Add(point);
            else
            {
                ((Canvas)CanvasArea).Children.Clear();
                DrawPoints();
            }

            var b = GetBezierApproximation(points, 256);
            var pf = new PathFigure(b.Points[0], new[] { b }, false);
            var pfc = new PathFigureCollection();
            var p = new Path();
            var pge = new PathGeometry();

            pfc.Add(pf);
            pge.Figures = pfc;
            p.Data = pge;
            p.Stroke = new SolidColorBrush(Color.FromRgb(0, 0, 0));
            lastLine = p;

            if (((Canvas)CanvasArea).Children.Count > 1 && newLine)
                ((Canvas)CanvasArea).Children.RemoveAt(removingElement);

            ((Canvas)CanvasArea).Children.Add(p);
            removingElement = ((Canvas)CanvasArea).Children.Count - 1;

            if (newLine)
            {
                var specialPoint = new Rectangle
                {
                    Height = 10,
                    Width = 10,
                    RadiusX = 5,
                    RadiusY = 5,
                    Fill = new SolidColorBrush(Color.FromRgb(255, 0, 0)),
                    Cursor = Cursors.Pen
                };

                Canvas.SetLeft(specialPoint, point.X - 5);
                Canvas.SetTop(specialPoint, point.Y - 5);
                ((Canvas)CanvasArea).Children.Add(specialPoint);
            }
        }

        private PolyLineSegment GetBezierApproximation(List<Point> controlPoints, int outputSegmentCount)
        {
            Point[] points = new Point[outputSegmentCount + 1];

            for (int i = 0; i <= outputSegmentCount; i++)
            {
                double t = (double)i / outputSegmentCount;
                points[i] = GetBezierPoint(t, controlPoints, 0, controlPoints.Count);
            }

            return new PolyLineSegment(points, true);
        }

        private Point GetBezierPoint(double t, List<Point> controlPoints, int index, int count)
        {
            if (count == 1)
                return controlPoints[index];

            var P0 = GetBezierPoint(t, controlPoints, index, count - 1);
            var P1 = GetBezierPoint(t, controlPoints, index + 1, count - 1);

            return new Point((1 - t) * P0.X + t * P1.X, (1 - t) * P0.Y + t * P1.Y);
        }


        private void DrawPoints()
        {
            foreach (var point in points)
            {
                var specialPoint = new Rectangle
                {
                    Height = 10,
                    Width = 10,
                    RadiusX = 5,
                    RadiusY = 5,
                    Fill = new SolidColorBrush(Color.FromRgb(255, 0, 0)),
                    Cursor = Cursors.Pen
                };

                Canvas.SetLeft(specialPoint, point.X - 5);
                Canvas.SetTop(specialPoint, point.Y - 5);
                ((Canvas)CanvasArea).Children.Add(specialPoint);
            }
        }
    }
}
