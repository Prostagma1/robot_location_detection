using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Point = OpenCvSharp.Point;
using Size = OpenCvSharp.Size;

namespace robot_location_detection
{
    public partial class Form1 : Form
    {
        bool runVideo;
        List<string> paths = new List<string> { };
        List<Point[]> countors;
        List<Point2f[]> countors2f;
        List<Point[]> detectedMarkers;
        List<Point> coordsReal;
        bool search, designations, testc;
        VideoCapture capture;
        Mat matInput;
        Thread cameraThread;
        readonly Size sizeObject = new Size(640, 480);
        readonly Size sizeObjectDraw = new Size(320, 240);
        Mat[] matSigns;
        Mat[] teamplates = new Mat[4];
        string pathToFile;
        Mat drawMat;
        List<Point> realPointsMarkers;
        double angle = 0;
        public Form1()
        {
            InitializeComponent();
            drawMat = new Mat(sizeObjectDraw, MatType.CV_8UC3, Scalar.Black);
            for (byte i = 0; i < 4; i++)
            {
                teamplates[i] = new Mat($@"D:\Study\4 sem\TechnicalVision\CVSLab6Photos\teamplates\{i}.jpg").Resize(new Size(162, 126)).CvtColor(ColorConversionCodes.BGR2GRAY).Threshold(128, 255, ThresholdTypes.Binary);
            }
            realPointsMarkers = new List<Point> {
                new Point(400,20),
                new Point(120,20),
                new Point(20,120),
                new Point(20,220),
                new Point(40,340),
                new Point(120,340)
            };
        }
        private void DisposeVideo()
        {
            pictureBox1.Image = null;
            if (cameraThread != null && cameraThread.IsAlive) cameraThread.Abort();
            matInput?.Dispose();
            capture?.Dispose();
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            DisposeVideo();
        }
        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            panel2.Enabled = false;
        }
        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            panel2.Enabled = true;
        }
        private void button2_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folder = new FolderBrowserDialog();
            folder.SelectedPath = @"D:\Study\4 sem\TechnicalVision\CVSLab6Photos";

            if (folder.ShowDialog() == DialogResult.OK)
            {
                listBox1.Items.Clear();
                paths.Clear();

                string[] files = Directory.GetFiles(folder.SelectedPath);

                foreach (var file in files)
                {
                    var ext = Path.GetExtension(file);

                    if (ext == ".bmp" || ext == ".png" || ext == ".jpg")
                    {
                        paths.Add(file);
                        listBox1.Items.Add(Path.GetFileName(file));
                    }
                }
                listBox1.SelectedIndex = 0;
                listBox1_SelectedIndexChanged(sender, e);
            }
            folder.Dispose();
        }
        private void button1_Click(object sender, EventArgs e)
        {
            if (runVideo)
            {
                runVideo = false;
                DisposeVideo();
                button1.Text = "Старт";
            }
            else
            {
                runVideo = true;
                matInput = new Mat();

                if (radioButton1.Checked)
                {
                    capture = new VideoCapture(0)
                    {
                        FrameHeight = sizeObject.Height,
                        FrameWidth = sizeObject.Width,
                        AutoFocus = true
                    };
                }
                cameraThread = new Thread(new ThreadStart(CaptureCameraCallback));
                cameraThread.Start();
                button1.Text = "Стоп";
            }
        }
        private void DrawMarkersAtMat(ref Mat mat)
        {
            mat = new Mat(350, 410, MatType.CV_8UC3, Scalar.All(255));
            foreach (var point in realPointsMarkers)
            {
                mat.Circle(point, 2, Scalar.Red, 2);
            }
            if (testPoint != null) mat.Circle(testPoint, 2, Scalar.Green, 2);
        }
        private void CaptureCameraCallback()
        {
            while (runVideo)
            {
                matInput = radioButton1.Checked ? capture.RetrieveMat() : new Mat(pathToFile).Resize(sizeObject);
                DrawMarkersAtMat(ref drawMat);
                if (search)
                {
                    SearchingContours(ref matInput, out countors, out countors2f);
                    GetPerspective(matInput, countors2f, out matSigns);
                    matInput.DrawContours(countors, -1, Scalar.Red);
                }
                if (designations)
                {
                    MatrixToTemplateComparison(matSigns, teamplates, ref matInput, out detectedMarkers, out coordsReal);
                    SearchZCoord();
                    drawMat.Line(testPoint, coordsReal[0], Scalar.Yellow);
                    drawMat.Line(testPoint, coordsReal[1], Scalar.Blue);
                    drawMat.Line(coordsReal[0], coordsReal[1], Scalar.Blue);

                }
                if (testc)
                {
                    Point camera = new Point(320, 480);
                    List<Point> marks = new List<Point> { };
                    for (int i = 0; i < detectedMarkers.Count; i++)
                    {
                        marks.Add(new Point(((detectedMarkers[i][0].X + detectedMarkers[i][1].X) / 2), (detectedMarkers[i][1].Y + detectedMarkers[i][2].Y) / 2));
                        matInput.Line(camera, marks[i], Scalar.Blue);
                    }
                    if (marks.Count > 1)
                    {
                        matInput.Line(marks[0], marks[1], Scalar.Blue);
                        Point high = new Point(320, (marks[1].Y + marks[0].Y) / 2);
                        matInput.Line(camera, high, Scalar.Blue);
                        var highDist = Point.Distance(high, camera);
                        var catet = Point.Distance(camera, marks[0]);
                        matInput.Line(camera, marks[0], Scalar.Yellow);
                        angle = Math.Asin(highDist / catet);
                    }
                }
                Invoke(new Action(() =>
                {
                    pictureBox1.Image = BitmapConverter.ToBitmap(matInput);
                    pictureBox2.Image = BitmapConverter.ToBitmap(drawMat);
                    label2.Text = "Угол = " + (angle * 180 / 3.14).ToString();
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }));
            }
        }
        Point testPoint;
        public void SearchZCoord()
        {
            double[] zCoord = new double[2];
            for (int i = 0; i < detectedMarkers.Count; i++)
            {
                zCoord[i] = 120 / Cv2.ContourArea(detectedMarkers[i]) * 100;
            }
            testPoint = CalculatePointC(coordsReal[0], coordsReal[1], zCoord[0], zCoord[1], angle);
        }
        public static Point CalculatePointC(Point pointA, Point pointB, double lengthAC, double lengthBC, double angleBAC)
        {
            double deltaX = pointB.X - pointA.X;
            double deltaY = pointB.Y - pointA.Y;

            double cosine = Math.Cos(angleBAC);
            double sine = Math.Sin(angleBAC);
            double rotationMatrixA = cosine * deltaX - sine * deltaY;
            double rotationMatrixB = sine * deltaX + cosine * deltaY;

            double ratio = lengthAC / lengthBC;
            double rotatedDeltaX = ratio * rotationMatrixA;
            double rotatedDeltaY = ratio * rotationMatrixB;

            double pointCX = pointA.X + rotatedDeltaX;
            double pointCY = pointA.Y + rotatedDeltaY;

            return new Point(pointCX, pointCY);
        }
        public void MatrixToTemplateComparison(Mat[] inputMats, Mat[] inputTemplates, ref Mat mat, out List<Point[]> markers, out List<Point> coordMarkers)
        {
            markers = new List<Point[]>();
            coordMarkers = new List<Point>();
            for (byte i = 0; i < inputMats.Length; i++)
            {
                double maxCom = 100;
                int id = -1;
                for (byte j = 0; j < 4; j++)
                {
                    var a = inputMats[i].BitwiseAnd(inputTemplates[j]).ToMat();
                    var b = a.CountNonZero();
                    if (b > maxCom)
                    {
                        maxCom = b;
                        id = j;
                    }
                }
                if (id != -1)
                {
                    markers.Add(countors[i]);
                    coordMarkers.Add(realPointsMarkers[id]);
                    mat.DrawContours(detectedMarkers, i, Scalar.Green, 3);
                    mat.PutText($"id:{id + 10} {realPointsMarkers[id]}", countors[i][0], HersheyFonts.HersheySimplex, 0.6, Scalar.Red, 2);
                }
            }
        }
        public void GetPerspective(Mat mat, List<Point2f[]> points, out Mat[] warpMat)
        {
            warpMat = new Mat[points.Count];
            Point2f[] sizeMatrix = new Point2f[4]{
                new Point2f(0, 0),
                new Point2f(162, 0),
                new Point2f(162, 126),
                new Point2f(0, 126)
            };
            for (int i = 0; i < points.Count; i++)
            {
                warpMat[i] = mat.Clone();
                var matrix = Cv2.GetPerspectiveTransform(points[i], sizeMatrix);
                Cv2.WarpPerspective(mat, warpMat[i], matrix, new Size(162, 126));
                warpMat[i] = warpMat[i].CvtColor(ColorConversionCodes.BGR2GRAY).Threshold(128, 255, ThresholdTypes.Binary);
            }
        }
        public void SearchingContours(ref Mat inputMat, out List<Point[]> countoursByCircle, out List<Point2f[]> countoursByCircle2f)
        {
            List<Point[]> flitredCountours = new List<Point[]>();

            Point[][] contours;

            inputMat.MedianBlur(3).Canny(70, 150).FindContours(out contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            foreach (var contour in contours)
            {

                var approximatedContour = new Point[contour.Length];
                var approximatedContour2f = new Point2f[contour.Length];

                using (var contourMat = new Mat(1, contour.Length, MatType.CV_32SC2, contour.SelectMany(p => new[] { p.X, p.Y }).ToArray()))
                {
                    Cv2.ApproxPolyDP(contourMat, contourMat, 5, true);

                    contourMat.GetArray(out approximatedContour);

                }

                if (approximatedContour.Length == 4 && Cv2.ContourArea(approximatedContour) > 600)
                {
                    flitredCountours.Add(approximatedContour);
                }
            }
            countoursByCircle = RectangleCornerFinder.FindRectangleCorners(flitredCountours, out countoursByCircle2f);
        }
        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex != -1)
            {
                pathToFile = paths[listBox1.SelectedIndex];
            }
        }
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            search = checkBox1.Checked;
        }
        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            testc = checkBox3.Checked;
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            designations = checkBox2.Checked;
        }
    }
}
