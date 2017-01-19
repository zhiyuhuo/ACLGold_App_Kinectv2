//------------------------------------------------------------------------------
// <copyright file="SkeletonIllustrator.cs" company="University of Missouri">
//     Copyright (c) Curators of the University of Missouri.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Kinect;
using System.Collections.Generic;

namespace ACL_Gold_x64.Controllers
{
    public class SkeletonIllustrator
    {
         //private const Single RenderWidth = 640.0f;
         //private const Single RenderHeight = 480.0f;
        private const Single RenderWidth = 1920.0f;
        private const Single RenderHeight = 1080.0f;
        private const Byte JointThickness = 3;
        private const Byte BodyCenterThickness = 10;        
        
        
        private static readonly Brush CenterPointBrush = Brushes.Blue;
        private static readonly Brush InferredJointBrush = Brushes.Yellow;
        private static readonly Brush TrackedJointBrush = Brushes.LightGreen;


        public static readonly Pen TrackedBonePen = new Pen(Brushes.Green, 8);
        public static readonly Pen TrackedBonePenZoomIn = new Pen(Brushes.Green, 3);
        public static readonly Pen InferredBonePen = new Pen(Brushes.Gray, 1);

        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="skeleton"></param>
        /// <param name="drawContext"></param>
        /// <param name="sensorManager"></param>
        /// <param name="colorBitmap"></param>
        public static void DrawBonesAndJoints(Body skeleton, DrawingContext drawContext, ref SensorManager sensorManager, ref WriteableBitmap colorBitmap)
        {
        
            //Counter for the joints we're cycling through.
            Int32 i;
            
            //Render bones.
            DrawBone(skeleton, ref drawContext, JointType.SpineMid, JointType.SpineBase, ref sensorManager);
            DrawBone(skeleton, ref drawContext, JointType.SpineBase, JointType.HipLeft, ref sensorManager);
            DrawBone(skeleton, ref drawContext, JointType.SpineBase, JointType.HipRight, ref sensorManager);
            DrawBone(skeleton, ref drawContext, JointType.HipLeft, JointType.KneeLeft, ref sensorManager);
            DrawBone(skeleton, ref drawContext, JointType.KneeLeft, JointType.AnkleLeft, ref sensorManager);
            DrawBone(skeleton, ref drawContext, JointType.AnkleLeft, JointType.FootLeft, ref sensorManager);
            DrawBone(skeleton, ref drawContext, JointType.HipRight, JointType.KneeRight, ref sensorManager);
            DrawBone(skeleton, ref drawContext, JointType.KneeRight, JointType.AnkleRight, ref sensorManager);
            DrawBone(skeleton, ref drawContext, JointType.AnkleRight, JointType.FootRight, ref sensorManager);
            
            //Render joints.
            for(i = 12; i < 20; i++)
            {
                var j = skeleton.Joints[(JointType)i];
                Brush drawBrush = null;

                switch (j.TrackingState)
                {
                    case(TrackingState.Tracked):
                        drawBrush = TrackedJointBrush;
                        break;
                    case (TrackingState.Inferred):
                        drawBrush = InferredJointBrush;
                        break;
                }

                if (drawBrush != null)
                    drawContext.DrawEllipse(drawBrush, null, SkeletonPointToScreen(j.Position, ref sensorManager), JointThickness, JointThickness);
                
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="CameraSpacePoint"></param>
        /// <param name="manager"></param>
        /// <returns></returns>
        public static Point SkeletonPointToScreen(CameraSpacePoint cameraSpacePoint, ref SensorManager manager)
        {
            //var depthPoint = manager.Sensor.MapSkeletonPointToColor(CameraSpacePoint, ColorImageFormat.RgbResolution640x480Fps30);
            ColorSpacePoint colorPoint = manager.Sensor.CoordinateMapper.MapCameraPointToColorSpace(cameraSpacePoint);
            return new Point(colorPoint.X, colorPoint.Y);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="skeleton"></param>
        /// <param name="drawContext"></param>
        /// <param name="jType0"></param>
        /// <param name="jType1"></param>
        /// <param name="sensorManager"></param>
        public static void DrawBone(Body skeleton, ref DrawingContext drawContext, JointType jType0, JointType jType1, ref SensorManager sensorManager)
        {
            var joint0 = skeleton.Joints[jType0];
            var joint1 = skeleton.Joints[jType1];

            //If the joints are not being tracked, exit.
            if (joint0.TrackingState == TrackingState.NotTracked || joint1.TrackingState == TrackingState.NotTracked)
                return;

            //Don't draw if both points are inferred.
            if (joint0.TrackingState == TrackingState.Inferred && joint1.TrackingState == TrackingState.Inferred)
                return;

            //We assume all drawn bones are inferred unles BOTH joints are tracked.
            //var drawPen = InferredBonePen;
            if (joint0.TrackingState == TrackingState.Tracked && joint1.TrackingState == TrackingState.Tracked)
            {
                var drawPen = TrackedBonePen;
                drawContext.DrawLine(drawPen, SkeletonPointToScreen(joint0.Position, ref sensorManager), SkeletonPointToScreen(joint1.Position,  ref sensorManager));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="skeleton"></param>
        /// <param name="skeletonFrame"></param>
        /// <param name="drawingGroup"></param>
        /// <param name="colorBitmap"></param>
        /// <param name="sensorManager"></param>
        /// <param name="colorPixels"></param>
        public static void SkeletonNotNull(Body skeleton, BodyFrame skeletonFrame, ref DrawingGroup drawingGroup, ref WriteableBitmap colorBitmap,
            SensorManager sensorManager, ref Byte[] colorPixels, ref WriteableBitmap depthBitmap, ref Byte[] depthPixels)
        {
            //Copy in the color image
            colorBitmap.WritePixels(new Int32Rect(0, 0, colorBitmap.PixelWidth, colorBitmap.PixelHeight), colorPixels, colorBitmap.PixelWidth * sizeof(Int32), 0);

            //Copy in the depth image
            depthBitmap.WritePixels(new Int32Rect(0, 0, depthBitmap.PixelWidth, depthBitmap.PixelHeight), depthPixels, depthBitmap.PixelWidth * sizeof(Int32), 0);

            //Blank image
            using (var dc = drawingGroup.Open())
            {
                dc.DrawImage(colorBitmap, new Rect(new Point(0, 0), new Point(colorBitmap.PixelWidth, colorBitmap.PixelHeight)));

                //Draws bones and joints...
               
                if ( skeleton.IsTracked == true )
                {
                    DrawBonesAndJoints(skeleton, dc, ref sensorManager, ref colorBitmap);
                }
                else
                {
                    //dc.DrawEllipse(CenterPointBrush, null, SkeletonPointToScreen(skeleton., ref sensorManager), BodyCenterThickness, BodyCenterThickness);
                }
                
               
                /*
                if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
                    DrawBonesAndJoints(skeleton, dc, ref sensorManager, ref colorBitmap);
                else if (skeleton.TrackingState == SkeletonTrackingState.PositionOnly)
                    dc.DrawEllipse(CenterPointBrush, null, SkeletonPointToScreen(skeleton.Position, ref sensorManager), BodyCenterThickness, BodyCenterThickness);
                */
            }

            //prevent drawing outside the render area.
            drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0, 0, RenderWidth, RenderHeight));
            
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="drawingGroup"></param>
        /// <param name="colorBitmap"></param>
        /// <param name="colorPixels"></param>
        /// <param name="image"></param>
        /// <param name="imageSource"></param>
        public static void SkeletonNull(ref DrawingGroup drawingGroup, ref WriteableBitmap colorBitmap, ref Byte[] colorPixels, ref Image image, ref DrawingImage imageSource,
            ref Byte[] depthPixels, ref WriteableBitmap depthBitmap)
        {
            //Copy in the color image
            colorBitmap.WritePixels(new Int32Rect(0, 0, colorBitmap.PixelWidth, colorBitmap.PixelHeight), colorPixels, colorBitmap.PixelWidth * sizeof(Int32), 0);

            //Copy in the color image
            depthBitmap.WritePixels(new Int32Rect(0, 0, depthBitmap.PixelWidth, depthBitmap.PixelHeight), depthPixels, depthBitmap.PixelWidth * sizeof(Int32), 0);
            
            //Blank...
            using (var dc = drawingGroup.Open())
            {
                dc.DrawImage(colorBitmap, new Rect(new Point(0, 0), new Point(colorBitmap.PixelWidth, colorBitmap.PixelHeight)));
            }
            
            //prevent drawing outside of the drawing area.
            drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0, 0, RenderWidth, RenderHeight));
            image.Source = imageSource;
        }

        //Store adjusted array, skeleton, ground plane data for transform (rdata), floor clip (4 values), final output values (angle)

        /// <summary>
        /// Draws bones and joints for EndOfTest.xaml.cs
        /// </summary>
        /// <param name="sensorManager">The sensor manager </param>
        /// <param name="spa">The skeleton point array</param>
        /// <param name="drawingContext">Drawing context on the window</param>
        /// <param name="sk">The skeleton</param>
        public static void DrawBonesAndJointsSpa(ref SensorManager sensorManager, CameraSpacePoint[] spa, DrawingContext drawingContext, Body sk)
        {
            var numJoints = Enum.GetNames(typeof(JointType)).Length;
            
            //head
            DrawBone(ref sensorManager, sk, drawingContext, JointType.Head, JointType.Neck);
            DrawBone(ref sensorManager, sk, drawingContext, JointType.Neck, JointType.SpineShoulder);

            //body
            DrawBone(ref sensorManager, sk, drawingContext, JointType.SpineShoulder, JointType.ShoulderLeft);
            DrawBone(ref sensorManager, sk, drawingContext, JointType.SpineShoulder, JointType.ShoulderRight);
            DrawBone(ref sensorManager, sk, drawingContext, JointType.SpineShoulder, JointType.SpineMid);
            DrawBone(ref sensorManager, sk, drawingContext, JointType.SpineMid, JointType.SpineBase);
            DrawBone(ref sensorManager, sk, drawingContext, JointType.SpineBase, JointType.HipLeft);
            DrawBone(ref sensorManager, sk, drawingContext, JointType.SpineBase, JointType.HipRight);

            //uppper arms
            DrawBone(ref sensorManager, sk, drawingContext, JointType.ShoulderLeft, JointType.ElbowLeft);
            DrawBone(ref sensorManager, sk, drawingContext, JointType.ShoulderRight, JointType.ElbowRight);
            DrawBone(ref sensorManager, sk, drawingContext, JointType.ElbowLeft, JointType.WristLeft);
            DrawBone(ref sensorManager, sk, drawingContext, JointType.ElbowRight, JointType.WristRight);
            DrawBone(ref sensorManager, sk, drawingContext, JointType.WristLeft, JointType.HandLeft);
            DrawBone(ref sensorManager, sk, drawingContext, JointType.WristRight, JointType.HandRight);

            //bottom legs
            DrawBone(ref sensorManager, sk, drawingContext, JointType.HipLeft, JointType.KneeLeft);
            DrawBone(ref sensorManager, sk, drawingContext, JointType.KneeLeft, JointType.AnkleLeft);
            DrawBone(ref sensorManager, sk, drawingContext, JointType.AnkleLeft, JointType.FootLeft);
            DrawBone(ref sensorManager, sk, drawingContext, JointType.HipRight, JointType.KneeRight);
            DrawBone(ref sensorManager, sk, drawingContext, JointType.KneeRight, JointType.AnkleRight);
            DrawBone(ref sensorManager, sk, drawingContext, JointType.AnkleRight, JointType.FootRight);
            
            //render joints
            for(var i = 0; i < numJoints; i++)
            {
                DrawJoint(ref sensorManager, sk, (JointType)i, drawingContext);
            }
        }

        /// <summary>
        /// Draws bones and joints for EndOfTest.xaml.cs
        /// </summary>
        /// <param name="sensorManager">The sensor manager </param>
        /// <param name="spa">The skeleton point array</param>
        /// <param name="drawingContext">Drawing context on the window</param>
        /// <param name="sk">The skeleton</param>
        public static void DrawBonesAndJointsSpa(ref SensorManager sensorManager, CameraSpacePoint[] spa, DrawingContext drawingContext, Dictionary<JointType, Joint> sk)
        {
            var numJoints = Enum.GetNames(typeof(JointType)).Length;

            //head
            DrawBone(ref sensorManager, sk, drawingContext, JointType.Head, JointType.Neck);
            DrawBone(ref sensorManager, sk, drawingContext, JointType.Neck, JointType.SpineShoulder);

            //body
            DrawBone(ref sensorManager, sk, drawingContext, JointType.SpineShoulder, JointType.ShoulderLeft);
            DrawBone(ref sensorManager, sk, drawingContext, JointType.SpineShoulder, JointType.ShoulderRight);
            DrawBone(ref sensorManager, sk, drawingContext, JointType.SpineShoulder, JointType.SpineMid);
            DrawBone(ref sensorManager, sk, drawingContext, JointType.SpineMid, JointType.SpineBase);
            DrawBone(ref sensorManager, sk, drawingContext, JointType.SpineBase, JointType.HipLeft);
            DrawBone(ref sensorManager, sk, drawingContext, JointType.SpineBase, JointType.HipRight);

            //uppper arms
            DrawBone(ref sensorManager, sk, drawingContext, JointType.ShoulderLeft, JointType.ElbowLeft);
            DrawBone(ref sensorManager, sk, drawingContext, JointType.ShoulderRight, JointType.ElbowRight);
            DrawBone(ref sensorManager, sk, drawingContext, JointType.ElbowLeft, JointType.WristLeft);
            DrawBone(ref sensorManager, sk, drawingContext, JointType.ElbowRight, JointType.WristRight);
            DrawBone(ref sensorManager, sk, drawingContext, JointType.WristLeft, JointType.HandLeft);
            DrawBone(ref sensorManager, sk, drawingContext, JointType.WristRight, JointType.HandRight);

            //bottom legs
            DrawBone(ref sensorManager, sk, drawingContext, JointType.HipLeft, JointType.KneeLeft);
            DrawBone(ref sensorManager, sk, drawingContext, JointType.KneeLeft, JointType.AnkleLeft);
            DrawBone(ref sensorManager, sk, drawingContext, JointType.AnkleLeft, JointType.FootLeft);
            DrawBone(ref sensorManager, sk, drawingContext, JointType.HipRight, JointType.KneeRight);
            DrawBone(ref sensorManager, sk, drawingContext, JointType.KneeRight, JointType.AnkleRight);
            DrawBone(ref sensorManager, sk, drawingContext, JointType.AnkleRight, JointType.FootRight);

            //render joints
            for (var i = 0; i < numJoints; i++)
            {
                DrawJoint(ref sensorManager, sk, (JointType)i, drawingContext);
            }
        }

        public static void DrawBone(ref SensorManager sensorManager, Body skeleton, DrawingContext drawingContext, JointType joint0, JointType joint1)
        {
            var point0 = sensorManager.Sensor.CoordinateMapper.MapCameraPointToColorSpace(skeleton.Joints[joint0].Position);
            var point1 = sensorManager.Sensor.CoordinateMapper.MapCameraPointToColorSpace(skeleton.Joints[joint1].Position);
            var point0B = new Point(point0.X/6, point0.Y/6 );
            var point1B = new Point(point1.X/6, point1.Y/6 );
            drawingContext.DrawLine(TrackedBonePenZoomIn, point0B, point1B);
        }

        public static void DrawBone(ref SensorManager sensorManager, Dictionary<JointType, Joint> skeleton, DrawingContext drawingContext, JointType joint0, JointType joint1)
        {
            var point0 = sensorManager.Sensor.CoordinateMapper.MapCameraPointToColorSpace(skeleton[joint0].Position);
            var point1 = sensorManager.Sensor.CoordinateMapper.MapCameraPointToColorSpace(skeleton[joint1].Position);
            var point0B = new Point(point0.X / 6, point0.Y / 6);
            var point1B = new Point(point1.X / 6, point1.Y / 6);
            drawingContext.DrawLine(TrackedBonePenZoomIn, point0B, point1B);
        }

        private static void DrawJoint(ref SensorManager sensorManager, Body sk, JointType jt0, DrawingContext drawingContext)
        {
            var depthPoint = sensorManager.Sensor.CoordinateMapper.MapCameraPointToColorSpace(sk.Joints[jt0].Position);
            drawingContext.DrawEllipse(TrackedJointBrush, null, new Point(depthPoint.X / 6, depthPoint.Y / 6), JointThickness, JointThickness);
        }

        private static void DrawJoint(ref SensorManager sensorManager, Dictionary<JointType, Joint> sk, JointType jt0, DrawingContext drawingContext)
        {
            var depthPoint = sensorManager.Sensor.CoordinateMapper.MapCameraPointToColorSpace(sk[jt0].Position);
            drawingContext.DrawEllipse(TrackedJointBrush, null, new Point(depthPoint.X / 6, depthPoint.Y / 6), JointThickness, JointThickness);
        }
    }
}
