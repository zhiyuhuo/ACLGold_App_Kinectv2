//------------------------------------------------------------------------------
// <copyright file="SensorManager.cs" company="University of Missouri">
//     Copyright (c) Curators of the University of Missouri.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------


using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ACL_Gold_x64.Models;
using Microsoft.Kinect;

namespace ACL_Gold_x64.Controllers
{
    public class SensorManager
    {
        private KinectSensor _sensor;
        private Body[] _skeletonsArray;
        public JumpStage JumpStage;
        public Int32 SuccessfulJumps;
        private Byte[] _colorPixels;
        private Byte[] _depthPixels;
        private WriteableBitmap _colorBitmap;
        private WriteableBitmap _depthBitmap;
        private DrawingGroup _drawingGroup;
        public Image KinectImage;
        public Ellipse State0, State1, State2, State3;
        public DrawingImage ImageSource;
        public TextBlock TxtSuccessfulJumps;
        public Brush EllipseFill;
        public JumpResultsCollection TestSubjectJumps { get; set; }
        private CoordinateMapper coordinateMapper;

        public int _skeletonFrameCount = 0;

        public KinectSensor Sensor
        {
            get { return _sensor; }
            set { _sensor = value; }
        }

        private BodyFrameReader _bodyFrameReader = null;
        private DepthFrameReader _depthFrameReader = null;
        private ColorFrameReader _colorFrameReader = null;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cB"></param>
        /// <param name="dB"></param>
        /// <param name="sensorChooser"></param> this parameter is removed
        /// <param name="drawingGroup"></param>
        /// <param name="kinectImage"></param>
        /// <param name="imageSource"></param>
        /// <param name="s0"></param>
        /// <param name="s1"></param>
        /// <param name="s2"></param>
        /// <param name="s3"></param>
        /// <param name="sj"></param>
        public SensorManager(ref WriteableBitmap cB, ref WriteableBitmap dB, ref DrawingGroup drawingGroup, 
                             ref Image kinectImage, ref DrawingImage imageSource, 
                             ref Ellipse s0, ref Ellipse s1, ref Ellipse s2, ref Ellipse s3, ref TextBlock sj)
        {
            //Sensor = sensorChooser.Kinect;
            Sensor = KinectSensor.GetDefault();
            _colorBitmap = cB;
            _depthBitmap = dB;
            _drawingGroup = drawingGroup;

            KinectImage = kinectImage;
            State0 = s0; State1 = s1; State2 = s2; State3 = s3;
            
            ImageSource = imageSource;
            var tS = this;

            JumpStage = new JumpStage(ref tS);
            TxtSuccessfulJumps = sj;
            TxtSuccessfulJumps.Text = "0";
        }
        /// <summary>
        /// 
        /// </summary>
        public void StopSensors()
        {
            if (_sensor != null)
            {
                Sensor.Close();
                MessageBox.Show(String.Format("SensorID: {0}\nSensor SkeletonStreamIsEnabled: {1}\n SensorColorStreamIsEnabled: {2}", Sensor.UniqueKinectId, Sensor.BodyFrameSource.IsActive, Sensor.ColorFrameSource.IsActive), "Sensor Halting Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                const String noSensor = "No sensor to stop.";
                MessageBox.Show(noSensor, "No Sensor", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public void ShutdownSensors()
        {
            if (Sensor != null)
                Sensor.Close();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cP"></param>
        /// <param name="dP"></param>
        public void StartSensors(ref Byte[] cP, ref Byte[] dP)
        {

            cP = new Byte[Sensor.ColorFrameSource.FrameDescription.LengthInPixels * 4];
            _colorPixels = cP;
            dP = new Byte[Sensor.DepthFrameSource.FrameDescription.Height * Sensor.DepthFrameSource.FrameDescription.Width * 4];  //sRGB
            _depthPixels = dP;

            _bodyFrameReader = Sensor.BodyFrameSource.OpenReader();
            //_depthFrameReader = Sensor.DepthFrameSource.OpenReader();
            _colorFrameReader = Sensor.ColorFrameSource.OpenReader();

            _bodyFrameReader.FrameArrived += SensorSkeletonFrameReady;
            _colorFrameReader.FrameArrived += SensorColorFrameReady;
            //_depthFrameReader.FrameArrived += SensorDepthFrameReady;

            coordinateMapper = this.Sensor.CoordinateMapper;

            Sensor.Open();
            //For debugging
            //MessageBox.Show(String.Format("SensorID: {0}\nSensor SkeletonStreamIsEnabled: {1}\n SensorColorStreamIsEnabled: {2}", Sensor.UniqueKinectId, Sensor.SkeletonStream.IsEnabled, Sensor.ColorStream.IsEnabled), "Sensor Streams Starting Information", MessageBoxButton.OK, MessageBoxImage.Information);      
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SensorSkeletonFrameReady(object sender, BodyFrameArrivedEventArgs e)
        {
            using (var skeletonFrame = e.FrameReference.AcquireFrame())
            {

                if (skeletonFrame != null)
                {
                    if (_skeletonsArray == null)
                    {
                        _skeletonsArray = new Body[skeletonFrame.BodyCount];
                    }

                    skeletonFrame.GetAndRefreshBodyData(_skeletonsArray);

                    //Find first tracked skeleton, if any.
                    var skeleton = _skeletonsArray.Where(s => s.IsTracked).FirstOrDefault();
                    var floorClipV4 = skeletonFrame.FloorClipPlane;

                    if (skeleton != null)
                    {
                        //var floorClipV4 = skeletonFrame.FloorClipPlane;
                        Tuple<Single, Single, Single, Single> floorClip = new Tuple<float, float, float, float>(floorClipV4.X,
                                                                                                              floorClipV4.Y,
                                                                                                              floorClipV4.Z,
                                                                                                              floorClipV4.W);

                        SkeletonIllustrator.SkeletonNotNull(skeleton, skeletonFrame, ref _drawingGroup, ref _colorBitmap,
                            this, ref _colorPixels, ref _depthBitmap, ref _depthPixels);
                        JumpStage.DetermineJumpStage(skeleton, floorClip, _colorBitmap, _depthBitmap);
                        _skeletonFrameCount++;
                        EllipseFill = new SolidColorBrush(Color.FromRgb(221, 64, 45));
                        switch (JumpStage.JumpStatus)
                        {
                            case 0:
                                State0.Fill = EllipseFill;
                                State1.Fill = Brushes.White;
                                State2.Fill = Brushes.White;
                                State3.Fill = Brushes.White;
                                break;
                            case 1:
                                State0.Fill = Brushes.White;
                                State1.Fill = EllipseFill;
                                State2.Fill = Brushes.White;
                                State3.Fill = Brushes.White;
                                break;
                            case 2:
                                State0.Fill = Brushes.White;
                                State1.Fill = Brushes.White;
                                State2.Fill = EllipseFill;
                                State3.Fill = Brushes.White;
                                break;
                            case 3:
                                State0.Fill = Brushes.White;
                                State1.Fill = Brushes.White;
                                State2.Fill = Brushes.White;
                                State3.Fill = EllipseFill;
                                //SuccessfulJumps++;
                                //TxtSuccessfulJumps.Text = Convert.ToString(SuccessfulJumps);
                                if (JumpStage.AdjustedSkeletonList.Count > JumpStage.MIN_COUNT)
                                {
                                    SuccessfulJumps++;
                                    TxtSuccessfulJumps.Text = Convert.ToString(SuccessfulJumps);
                                }
                                break;
                        }
                    }
                    else if (skeleton == null)
                    {
                        SkeletonIllustrator.SkeletonNull(ref _drawingGroup, ref _colorBitmap, ref _colorPixels,
                            ref KinectImage, ref ImageSource, ref _depthPixels, ref _depthBitmap);
                    }

                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SensorColorFrameReady(object sender, ColorFrameArrivedEventArgs e)
        {
            using (ColorFrame colorFrame = e.FrameReference.AcquireFrame())
            {
                if(colorFrame != null)
                {
                    colorFrame.CopyConvertedFrameDataToArray(_colorPixels, ColorImageFormat.Bgra);
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SensorDepthFrameReady(Object sender, DepthFrameArrivedEventArgs e)
        {
            using(var depthFrame = e.FrameReference.AcquireFrame())
            {
                if(depthFrame != null)
                {
                    ushort[] dpa = new ushort[Sensor.DepthFrameSource.FrameDescription.Height * 
                                        Sensor.DepthFrameSource.FrameDescription.Width];
                    depthFrame.CopyFrameDataToArray(dpa);

                    for (int p = 0; p < Sensor.DepthFrameSource.FrameDescription.Height *
                                        Sensor.DepthFrameSource.FrameDescription.Width; p++)
                    {
                        _depthPixels[p*4 + 0] = (Byte)Math.Min(255.0 * dpa[p] / 7500.0, 255.0);
                        _depthPixels[p*4 + 1] = (Byte) Math.Min(255.0*dpa[p] / 7500.0, 255.0);
                        _depthPixels[p*4 + 2] = (Byte)Math.Min(255.0 * dpa[p] / 7500.0, 255.0);
                        _depthPixels[p*4 + 3] = 0;
                    }
                }
            }
        }

        private void DrawStandingPlatform(Vector4 FloorPlane, DrawingContext drawingContext)
        {
            float Dfar = 3.5f;
            float Dnear = 3.25f;
            float Widehalf = 0.5f;
            CameraSpacePoint p1 = new CameraSpacePoint();
            p1.X = -Widehalf;
            p1.Z = Dfar;
            p1.Y = (-FloorPlane.W - (-Widehalf) * FloorPlane.X - (Dfar) * FloorPlane.Z) / FloorPlane.Y;

            CameraSpacePoint p2 = new CameraSpacePoint();
            p2.X = Widehalf;
            p2.Z = Dfar;
            p2.Y = (-FloorPlane.W - (Widehalf) * FloorPlane.X - (Dfar) * FloorPlane.Z) / FloorPlane.Y;

            CameraSpacePoint p3 = new CameraSpacePoint();
            p3.X = Widehalf;
            p3.Z = Dnear;
            p3.Y = (-FloorPlane.W - (Widehalf) * FloorPlane.X - (Dnear) * FloorPlane.Z) / FloorPlane.Y;

            CameraSpacePoint p4 = new CameraSpacePoint();
            p4.X = -Widehalf;
            p4.Z = Dnear;
            p4.Y = (-FloorPlane.W - (-Widehalf) * FloorPlane.X - (Dnear) * FloorPlane.Z) / FloorPlane.Y;

            ColorSpacePoint d1 = this.coordinateMapper.MapCameraPointToColorSpace(p1);
            ColorSpacePoint d2 = this.coordinateMapper.MapCameraPointToColorSpace(p2);
            ColorSpacePoint d3 = this.coordinateMapper.MapCameraPointToColorSpace(p3);
            ColorSpacePoint d4 = this.coordinateMapper.MapCameraPointToColorSpace(p4);

            SolidColorBrush br = Brushes.Blue;

            drawingContext.DrawLine(new Pen(br, 8), new Point(d1.X, d2.Y), new Point(d2.X, d2.Y));
            drawingContext.DrawLine(new Pen(br, 8), new Point(d2.X, d2.Y), new Point(d3.X, d3.Y));
            drawingContext.DrawLine(new Pen(br, 8), new Point(d3.X, d3.Y), new Point(d4.X, d4.Y));
            drawingContext.DrawLine(new Pen(br, 8), new Point(d4.X, d4.Y), new Point(d1.X, d1.Y));
        }
    }
}
