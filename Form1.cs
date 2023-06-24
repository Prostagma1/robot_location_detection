using OpenCvSharp;
using OpenCvSharp.Aruco;
using OpenCvSharp.Extensions;
using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Size = OpenCvSharp.Size;

namespace robot_location_detection
{
    public partial class Form1 : Form
    {
        bool runVideo;
        VideoCapture capture;
        Dictionary markers;
        Mat matInput;
        Thread cameraThread;
        readonly Size sizeObject = new Size(640, 480);
        readonly Size sizeObjectDraw = new Size(320, 240);
        string pathToFile;
        int[] ids;
        bool showMarks, searchMarks;
        Point3d[] center;
        Mat drawMat;
        readonly int markerLength = 300;
        public Form1()
        {
            InitializeComponent();
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
            OpenFileDialog file = new OpenFileDialog()
            {
                Multiselect = false
            };
            if (file.ShowDialog() == DialogResult.OK)
            {
                var tempPath = file.FileName;
                if (File.Exists(tempPath))
                {
                    var ext = Path.GetExtension(tempPath);
                    if (ext == ".png" || ext == ".jpg" || ext == ".jpeg")
                    {
                        pathToFile = tempPath;
                        textBox1.Text = pathToFile;
                    }
                }
            }
            file.Dispose();
        }
        private void button1_Click(object sender, EventArgs e)
        {
            if (runVideo)
            {
                runVideo = false;
                panel3.Enabled = false;
                DisposeVideo();
                button1.Text = "Старт";
            }
            else
            {
                runVideo = true;
                panel3.Enabled = true;
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
                if (searchMarks)
                {
                    SearchAndShowMarks(markers, ref matInput, out ids, out center, showMarks);
                }
                Invoke(new Action(() =>
                {
                    pictureBox1.Image = BitmapConverter.ToBitmap(matInput);
                    pictureBox2.Image = BitmapConverter.ToBitmap(drawMat);
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }));
            }
        }

        private void SearchAndShowMarks(Dictionary marksDict, ref Mat inputMat, out int[] ids, out Point3d[] centers, bool drawDetectedMarks)
        {
            Point2f[][] corners;
            var param = new DetectorParameters();
            CvAruco.DetectMarkers(inputMat, marksDict, out corners, out ids, param, out _);
            centers = new Point3d[corners.Length];

            for (short i = 0; i < centers.Length; i++)
            {
                for (byte j = 0; j < 4; j++)
                {
                    centers[i].X += (int)corners[i][j].X;
                    centers[i].Y += (int)corners[i][j].Y;
                }
                centers[i].X /= 4;
                centers[i].Y /= 4;
                centers[i].Z = markerLength - (int)Point2f.Distance(corners[i][0], corners[i][1]);
            }


            if (drawDetectedMarks && corners.Length > 0)
            {
                drawMat = new Mat(sizeObjectDraw, MatType.CV_8UC3, Scalar.Black);
                CvAruco.DrawDetectedDiamonds(inputMat, corners);
                for (short i = 0; i < centers.Length; i++)
                {
                    float turnY = corners[0][0].Y - corners[0][1].Y;
                    float turnX = corners[0][0].X - corners[0][1].X;
                    double angle = Math.Atan2(turnY, turnX) * 180 / Math.PI;
                    double absAngle = Math.Abs(angle);

                    var pointForDrawingMat = new Point((int)(centers[i].X / 1.9), (int)(centers[i].Z));
                    drawMat.PutText($"{ids[i]}", pointForDrawingMat, HersheyFonts.HersheySimplex, 0.5, Scalar.YellowGreen);
                    drawMat.Circle(pointForDrawingMat, 2, Scalar.Red, 2);

                    drawMat.Line(new Point(centers[i].X / 1.9 - 5, centers[i].Z + Math.Tan(-angle * Math.PI / 180) * centers[i].X / 1.9 - 5), new Point(centers[i].X/ 1.9 + 5, centers[i].Z + Math.Tan(angle*Math.PI/180) * centers[i].X/ 1.9 + 5), Scalar.Red);

                    inputMat.Circle((int)centers[i].X, (int)centers[i].Y, 2, Scalar.Red, 2);
                    inputMat.PutText($"id:{ids[i]} a:{Math.Round(180 - absAngle, 2)} d:{Math.Round(22 * centers[i].Z / 160, 2)}cm", new Point((int)centers[i].X, (int)centers[i].Y), HersheyFonts.HersheySimplex, 0.5, Scalar.YellowGreen);
                }
            }
        }
        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            showMarks = checkBox2.Checked;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            markers = CvAruco.GetPredefinedDictionary(PredefinedDictionaryName.Dict7X7_50);
            drawMat = new Mat(sizeObjectDraw, MatType.CV_8UC3, Scalar.Black);
        }
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            searchMarks = checkBox1.Checked;
        }
    }
}
