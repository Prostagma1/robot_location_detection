using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Threading;
using System.Windows.Forms;
using Size = OpenCvSharp.Size;

namespace robot_location_detection
{
    public partial class Form1 : Form
    {
        bool runVideo;
        VideoCapture capture;
        Mat matInput;
        Thread cameraThread;
        readonly Size sizeObject = new Size(640, 480);
        string pathToFile;
        public Form1()
        {
            InitializeComponent();
        }
        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            panel2.Enabled = true;
        }
        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            panel2.Enabled = false;
        }
        private void DisposeVideo()
        {
            pictureBox1.Image = null;
            if (cameraThread != null && cameraThread.IsAlive) cameraThread.Abort();
            matInput?.Dispose();
            capture?.Dispose();
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


                Invoke(new Action(() =>
                {
                    pictureBox1.Image = BitmapConverter.ToBitmap(matInput);
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }));
            }
        }
    }
}
