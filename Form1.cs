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
        VideoCapture capture;
        Mat matInput;
        Thread cameraThread;
        readonly Size sizeObject = new Size(640, 480);
        readonly Size sizeObjectDraw = new Size(320, 240);
        Point2f[] sizeMatrixPoints = new Point2f[4] { new Point(0, 240), new Point(0, 0), new Point(320, 0), new Point(320, 240) };

        string pathToFile;
        Mat drawMat;

        public Form1()
        {
            InitializeComponent();
            drawMat = new Mat(sizeObjectDraw, MatType.CV_8UC3, Scalar.Black);
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
        Mat matWorkZone = new Mat();
        List<Point2f[]> points2f;
        private void CaptureCameraCallback()
        {
            while (runVideo)
            {
                matInput = radioButton1.Checked ? capture.RetrieveMat() : new Mat(pathToFile).Resize(sizeObject);

                if (test)
                {
                    SearchingContours(ref matInput, out countors, out points2f);
                    matInput.DrawContours(countors, -1, Scalar.Red);
                    if (countors.Count > 0)
                    {
                        matWorkZone = new Mat(matInput, Cv2.BoundingRect(countors[0])).Resize(new Size(160, 120)).CvtColor(ColorConversionCodes.BGR2GRAY).Threshold(128,255,ThresholdTypes.Binary);
                    }

                }

                Invoke(new Action(() =>
                {
                    pictureBox1.Image = BitmapConverter.ToBitmap(matInput);
                    pictureBox2.Image = BitmapConverter.ToBitmap(matWorkZone);

                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }));
            }
        }
        public void CutSomeCountors(Mat sourceImage, List<Point[]> contours, out Mat outputImage)
        {
            outputImage = new Mat();

            foreach(var item in countors)
            {
                var a = Cv2.BoundingRect(item);

            }
        }
        public void SearchingContours(ref Mat inputMat, out List<Point[]> flitredCountours, out List<Point2f[]> flitredCountours2f)
        {
            flitredCountours = new List<Point[]>();
            flitredCountours2f = new List<Point2f[]>();

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
                using (var contourMat = new Mat(1, contour.Length, MatType.CV_32FC2, contour.SelectMany(p => new[] { p.X, p.Y }).ToArray()))
                {
                    // Выполняем аппроксимацию контура
                    Cv2.ApproxPolyDP(contourMat, contourMat, 5, true);

                    // Копируем координаты аппроксимированного контура в массив
                    contourMat.GetArray(out approximatedContour2f);

                }

                // Добавляем аппроксимированный контур в список
                if (approximatedContour.Length == 4 && Cv2.ContourArea(approximatedContour) > 600)
                {
                    flitredCountours.Add(approximatedContour);
                    flitredCountours2f.Add(approximatedContour2f);
                }
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex != -1)
            {
                pathToFile = paths[listBox1.SelectedIndex];
            }
        }
        bool test;
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            test = checkBox1.Checked;
        }
    }
}
