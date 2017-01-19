//------------------------------------------------------------------------------
// <copyright file="EndOfTest.xaml.cs" company="University of Missouri">
//     Copyright (c) Curators of the University of Missouri.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.IO;
using System.Windows;

using System.Windows.Media;
using System.Windows.Media.Imaging;
using ACL_Gold_x64.Controllers;
using Microsoft.Kinect;
using System.Collections.Generic;


namespace ACL_Gold_x64
{
    /// <summary>
    /// Interaction logic for EndOfTest.xaml
    /// </summary>
    public partial class EndOfTest
    {
        private readonly DrawingGroup _drawingGroup1;
        public DrawingImage DrawingImage1;
        private readonly DrawingGroup _drawingGroup2;
        public DrawingImage DrawingImage2;
        public SensorManager SensorManager;

        /// <summary>
        /// Constructor method for EndOfTest window
        /// </summary>
        /// <param name="sM">SensorManager from the ACL Testing Suite window.</param>
        public EndOfTest(ref SensorManager sM)
        {
            InitializeComponent();
            
            SensorManager = sM;
            _drawingGroup1 = new DrawingGroup();
            _drawingGroup2 = new DrawingGroup();
            DrawingImage1 = new DrawingImage(_drawingGroup1);
            DrawingImage2 = new DrawingImage(_drawingGroup2);
        }

        /// <summary>
        /// Click event method for btnEndOfTestExit
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnEndOfTestExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Places numbers in the user interface
        /// </summary>
        /// <param name="kneeAnkleSr">Knee-Ankle Separation Ratio</param>
        /// <param name="rightValgus">Right Valgus</param>
        /// <param name="leftValgus">Left Valgus</param>
        /// <param name="iP">Which set of frames are being referenced (initial contact or peak flexion)</param>
        public void AddNumbers(Double kneeAnkleSr, Double rightValgus, Double leftValgus, Int32 iP)
        {
            var shortKasr = Math.Truncate(kneeAnkleSr * 100) / 100;
            /*---- Look at this block! The data is flipped there! Doctor Gray:        ----*/ 
            /*---- Is that what you want!!! Zhiyu Huo found it at 12/19/2014 11:50am  ----*/
            var shortRv = (Math.Truncate(rightValgus * 100) / 100) * -1; //These two are multiplied times -1 to flip them into correct medical nomenclature.
            var shortLv = (Math.Truncate(leftValgus * 100) / 100) * -1; // Valgus is where the knee is tilted inwards. Varus is where the knee is outwards. Negative valgus is considered varus here.
            /* -------------------------------------------------------------------------- */
            if(iP == 0)
            {
                TxtInitContactKasr.Text = Convert.ToString(shortKasr);
                TxtInitContactLv.Text = Convert.ToString(shortLv);
                TxtInitContactRv.Text = Convert.ToString(shortRv);
            }
            else
            {
                TxtPeakFlexKasr.Text = Convert.ToString(shortKasr);
                TxtPeakFlexRv.Text = Convert.ToString(shortRv);
                TxtPeakFlexLv.Text = Convert.ToString(shortLv);
            }
        }

        /// <summary>
        /// Draws the entire skeleton.
        /// </summary>
        /// <param name="skelPointArray"></param>
        /// <param name="iP">Which set of frames are being referenced (initial contact or peak flexion)</param>
        /// <param name="colorBitmap">Writeable bitmap to which the skeleton is drawn.</param>
        /// <param name="skeleton"></param>
        public void DrawSkeleton(CameraSpacePoint[] skelPointArray, int iP, WriteableBitmap colorBitmap, Body skeleton)
        {
            if(iP == 0)
            {
                using(var dc = _drawingGroup1.Open())
                {
                    dc.DrawImage(colorBitmap, new Rect(new Point(0, 0), new Point(320, 180)));
                    SkeletonIllustrator.DrawBonesAndJointsSpa(ref SensorManager, skelPointArray, dc, skeleton);
                    _drawingGroup1.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, 320, 180));
                    InitContactImage.Source = DrawingImage1;

                    //Create way to save to file to jumpresult object for use in EndOfTest window
                    
                    
                }
            }
            else
            {
                using(var dc = _drawingGroup2.Open())
                {
                    dc.DrawImage(colorBitmap, new Rect(new Point(0, 0), new Point(320, 180)));
                    SkeletonIllustrator.DrawBonesAndJointsSpa( ref SensorManager, skelPointArray, dc, skeleton);
                    
                    _drawingGroup2.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, 320, 180));
                    PeakFlexImage.Source = DrawingImage2;

                    //Same here
                }
            }
        }

        /// <summary>
        /// Draws the entire skeleton.
        /// </summary>
        /// <param name="skelPointArray"></param>
        /// <param name="iP">Which set of frames are being referenced (initial contact or peak flexion)</param>
        /// <param name="colorBitmap">Writeable bitmap to which the skeleton is drawn.</param>
        /// <param name="skeleton"></param>
        public void DrawSkeleton(CameraSpacePoint[] skelPointArray, int iP, WriteableBitmap colorBitmap, Dictionary<JointType, Joint> body)
        {
            if (iP == 0)
            {
                using (var dc = _drawingGroup1.Open())
                {
                    dc.DrawImage(colorBitmap, new Rect(new Point(0, 0), new Point(320, 180)));
                    SkeletonIllustrator.DrawBonesAndJointsSpa(ref SensorManager, skelPointArray, dc, body);
                    _drawingGroup1.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, 320, 180));
                    InitContactImage.Source = DrawingImage1;

                    //Create way to save to file to jumpresult object for use in EndOfTest window


                }
            }
            else
            {
                using (var dc = _drawingGroup2.Open())
                {
                    dc.DrawImage(colorBitmap, new Rect(new Point(0, 0), new Point(320, 180)));
                    SkeletonIllustrator.DrawBonesAndJointsSpa(ref SensorManager, skelPointArray, dc, body);

                    _drawingGroup2.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, 320, 180));
                    PeakFlexImage.Source = DrawingImage2;

                    //Same here
                }
            }
        }
    }
}
