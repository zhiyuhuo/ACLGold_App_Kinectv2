//------------------------------------------------------------------------------
// <copyright file="ACLTestingSuite.xaml.cs" company="University of Missouri">
//     Copyright (c) Curators of the University of Missouri.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ACL_Gold_x64.Controllers;
using Microsoft.Kinect;
using System.Threading;
using ACL_Gold_x64.Models;


namespace ACL_Gold_x64
{
    /// <summary>
    /// Interaction logic for ACL_Testing_Suite.xaml
    /// </summary>
    public partial class AclTestingSuite
    {
        public SensorManager Manager;   // Sensor manager abstracts functions for the sensor. Passed just about everywhere.
        private Byte[] _colorPixels;     // Byte array with information about the color image frame from the Kinect.
        private Byte[] _depthPixels;  // Depth Image Pixel array with information about the depth image frame from the Kinect.
        public DrawingGroup DrawingGroup;
        public DrawingContext DrawContext;
        public WriteableBitmap ColorBitmap;
        public WriteableBitmap DepthBitmap;
        public DrawingImage ImageSource;
        public String SubjectId;    // The Test Subject's ID. To be filled out before the "finalize" button is clicked.
        private bool IfKinectRunning;
        private System.Windows.Threading.DispatcherTimer _dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
        private int _lastSecSkeletonFrameNum = 0;
                
        /// <summary>
        /// Constructor method for ACL_Testing_Suite window.
        /// </summary>
        public AclTestingSuite()       
        {//
            this.WindowState = System.Windows.WindowState.Maximized;
            InitializeComponent();
            // disable pause, finalize buttons until start button has been pressed.
            BtnFinalize.IsEnabled = false;
            BtnSuitePause.IsEnabled = false;
            
            //create drawing group
            DrawingGroup = new DrawingGroup();
            ImageSource = new DrawingImage(DrawingGroup);
            //ColorBitmap = new WriteableBitmap(640, 480, 96, 96, PixelFormats.Bgr32, null);
            //DepthBitmap = new WriteableBitmap(640, 480, 96, 96, PixelFormats.Bgr32, null);
            ColorBitmap = new WriteableBitmap(1920, 1080, 96, 96, PixelFormats.Bgr32, null);
            DepthBitmap = new WriteableBitmap(512, 424, 96, 96, PixelFormats.Bgr32, null);

            Manager = new SensorManager( ref ColorBitmap, ref DepthBitmap, ref DrawingGroup, ref KinectImage, ref ImageSource,
                               ref InnerCircle0, ref InnerCircle1, ref InnerCircle2, ref InnerCircle3, ref TxtSuccessfulJumps);

            BtnStartTest.IsEnabled = true;

            _dispatcherTimer.Tick += new EventHandler(DispatcherTimer_Tick);
            _dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            _dispatcherTimer.Start();

        }

        /// <summary>
        /// Click event method for btnSuiteExit.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSuiteExit_Click(object sender, RoutedEventArgs e)
        {
                Manager.StopSensors();
                Manager.ShutdownSensors();

                Environment.Exit(0);
        }

        /// <summary>
        /// Click event method for btnSuitePause.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSuitePause_Click(object sender, RoutedEventArgs e)
        {
            if (this.IfKinectRunning == true)
            {
                Manager.StopSensors();
                this.IfKinectRunning = false;
            }
            else
            {
                textBlock.Text = "Kinect Sensor is Closed!";
            }
        }

        /// <summary>
        /// Click event method for btnStartTest.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnStartTest_Click(object sender, RoutedEventArgs e)
        {
            if (this.IfKinectRunning == false)
            {
                Manager.StartSensors(ref _colorPixels, ref _depthPixels);

                Manager.TestSubjectJumps = new JumpResultsCollection();
                BtnSuitePause.IsEnabled = true;
                BtnFinalize.IsEnabled = true;
                BtnDeleteJumps.IsEnabled = true;
                this.IfKinectRunning = true;
            }
            else 
            {
                textBlock.Text = "Kinect Sensor is Already Started!";
            }
        }

        /// <summary>
        /// Click event method for btnFinalize.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnFinalize_Click(object sender, RoutedEventArgs e)
        {
            if (Manager.TestSubjectJumps.Jumps.Count > 0)
            {
                if (Manager != null)
                {
                    var finalWindow = new FinalizeTests(ref Manager, this);
                    finalWindow.Show();
                }
            }
            else 
            {
                textBlock.Text = "No jump has been collected!";
            }

        }

        /// <summary>
        /// ImageFailed event method for kinectImage
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void KinectImage_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {

        }

        private void BtnDeleteJumps_OnClick(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("This will delete all current jumps. Continue?", "Are you sure?",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            switch (result)
            {
                case MessageBoxResult.Yes:
                    Manager.SuccessfulJumps = 0;
                    Manager.TestSubjectJumps = new JumpResultsCollection();
                    TxtSuccessfulJumps.Text = "0";
                    break;
                case MessageBoxResult.No:
                    break;
            }
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            int fps = Manager._skeletonFrameCount - _lastSecSkeletonFrameNum;
            textBlock2.Text = "FPS: " + fps.ToString();
            _lastSecSkeletonFrameNum = Manager._skeletonFrameCount;
            // code goes here
        }
    }
}
