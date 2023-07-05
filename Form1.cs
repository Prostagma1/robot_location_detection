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
        bool search, designations;
        VideoCapture capture;
        Mat matInput;
        Thread cameraThread;
        readonly Size sizeObject = new Size(640, 480);
        readonly Size sizeObjectDraw = new Size(320, 240);
        Mat[] matSigns;
        Mat[] teamplates = new Mat[4];
        string pathToFile;
        Mat drawMat;

        public Form1()
        {
            InitializeComponent();
            drawMat = new Mat(sizeObjectDraw, MatType.CV_8UC3, Scalar.Black);
            for (byte i = 0; i < 4; i++)
            {
                teamplates[i] = new Mat($@"D:\Study\4 sem\TechnicalVision\CVSLab6Photos\teamplates\{i}.jpg").Resize(new Size(162, 126)).CvtColor(ColorConversionCodes.BGR2GRAY).Threshold(128, 255, ThresholdTypes.Binary);
            }
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
        private void CaptureCameraCallback()
        {
            while (runVideo)
            {
                matInput = radioButton1.Checked ? capture.RetrieveMat() : new Mat(pathToFile).Resize(sizeObject);

                if (search)
                {
                    SearchingContours(ref matInput, out countors, out countors2f);
                    GetPerspective(matInput, countors2f, out matSigns);
                    matInput.DrawContours(countors, -1, Scalar.Red);
                }
                Invoke(new Action(() =>
                {
                    pictureBox1.Image = BitmapConverter.ToBitmap(matInput);
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }));

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
                // Создаем новый массив для аппроксимированного контура
                var approximatedContour = new Point[contour.Length];
                var approximatedContour2f = new Point2f[contour.Length];


                // Создаем объект типа Mat, содержащий координаты контура
                using (var contourMat = new Mat(1, contour.Length, MatType.CV_32SC2, contour.SelectMany(p => new[] { p.X, p.Y }).ToArray()))
                {
                    // Выполняем аппроксимацию контура
                    Cv2.ApproxPolyDP(contourMat, contourMat, 5, true);

                    // Копируем координаты аппроксимированного контура в массив
                    contourMat.GetArray(out approximatedContour);

                }

                // Добавляем аппроксимированный контур в список
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
        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            designations = checkBox2.Checked;
        }
    }
}
