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
        string pathToFile;
        int[] ids;
        bool showMarks, searchMarks;
        string stringForLabel = "Координаты метки: ";
        Point3d[] center;
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
                    label4.Text = stringForLabel;
                    pictureBox1.Image = BitmapConverter.ToBitmap(matInput);
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
                label1.Text = corners[0][0].ToString();
                label2.Text = corners[0][1].ToString();
                float turnY = corners[0][0].Y - corners[0][1].Y;
                float turnX = corners[0][0].X - corners[0][1].X;

                label5.Text = (Math.Atan2(turnY, turnX)*180/Math.PI).ToString();
                label3.Text = Math.Abs(turnY) > 5 ? (turnY < 0 ? "->" : "<-") : "||";

                CvAruco.DrawDetectedDiamonds(inputMat, corners);
                for (short i = 0; i < centers.Length; i++)
                {
                    inputMat.Circle((int)centers[i].X, (int)centers[i].Y, 2, Scalar.Red, 2);
                    stringForLabel = $"Координаты метки:    X:{(int)centers[i].X} Y:{(int)centers[i].Y} Z:{(int)centers[i].Z}";
                    inputMat.PutText($"X:{(int)centers[i].X} Y:{(int)centers[i].Y} Z:{(int)centers[i].Z}", new Point((int)centers[i].X, (int)centers[i].Y), HersheyFonts.HersheySimplex, 0.5, Scalar.Red);
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
        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            searchMarks = checkBox1.Checked;
        }
    }
}
