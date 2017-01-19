//------------------------------------------------------------------------------
// <copyright file="JumpStage.cs" company="University of Missouri">
//     Copyright (c) Curators of the University of Missouri.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Kinect;

using ACL_Gold_x64.Models;

namespace ACL_Gold_x64.Controllers
{
    public class JumpStage
    {
        public LinkedList<CameraSpacePoint[]> AdjustedSkeletonList;
        public LinkedList<Tuple<Single, Single, Single, Single>> FloorPlaneList;   
        public LinkedList<WriteableBitmap> dp_list; 
        public LinkedList<WriteableBitmap> ImageList;    // list for the images- CHANGE TO LIST FOR DEPTH IMAGE
        public LinkedList<Body> OriginalSkeletonList;   // list for the original skeletons
        public CameraSpacePoint[] PrevSkeletonPoints; // skeleton point array for the previous skeleton
        public Int32 JumpStatus;    // value for the state machine
        public SensorManager SensorManager; // sensorManager object passed in.
        public const Double ConvValue = 100 / 2.54; //rename either inches to meters or meters to inches
        public const Double ConvValue2 = 2.54 / 100;    //same here
        public const Double PiDivide = Math.PI/2;
        private const String Error = "Error";
        public const int MIN_COUNT = 20;
        // new adding, because the joints in body class are read-only
        public LinkedList<Dictionary<JointType, Joint>> OriginalBodyJointsList = new LinkedList<Dictionary<JointType, Joint>>();   // list for the original skeletons

        /// <summary>
        /// Constructor for the JumpStage class.
        /// </summary>
        /// <param name="sM">Sensor manager object with requisite information contained therein.</param>
        /// <param name="jumpStatus">State machine variable. Set to -1 by default</param>
        /// <param name="prevSkeletonPoints">Body Point Array.</param>
        public JumpStage(ref SensorManager sM, Int32 jumpStatus = -1, CameraSpacePoint[] prevSkeletonPoints = null)
        {
            PrevSkeletonPoints = prevSkeletonPoints;
            JumpStatus = jumpStatus;
            SensorManager = sM;
            AdjustedSkeletonList = new LinkedList<CameraSpacePoint[]>(); //skeleton list
            ImageList = new LinkedList<WriteableBitmap>(); //image list
            OriginalSkeletonList = new LinkedList<Body>(); //original skeleton list
            dp_list = new LinkedList<WriteableBitmap>(); //depth pixel list
            FloorPlaneList = new LinkedList<Tuple<Single, Single, Single, Single>>(); //floor plane list
        }

        /// <summary>
        /// Machine state 0 = no one is in frame, or not in position to jump. default state. Only goes to state 1.
        /// Machine state 1 = some one is standing on the platform. A jump *may* take place. Can revert back to state 0.
        ///                     can only be entered from state 0.
        /// Machine state 2 = on the ground, in front of the platform. Can only enter from state 1. Must be within a certain "box". 
        ///                     2 feet wide, 2 1/2 deep.
        /// Machine state 3 = now in front of platform, feet on ground, at least 2 1/2 feet in front of platform. Jump has "ended". 
        ///  
        /// The buffer looks at the segment of data in Machine State 3. 
        /// </summary>                   
        /// <param name="skeleton">Body moving about in 3D space.</param>
        /// <param name="floorClip">Floor clip tuple</param>
        /// <param name="image">WriteableBitmap</param>
        /// <param name="dp">DepthImagePixel array</param>
        public void DetermineJumpStage(Body skeleton, Tuple<Single, Single, Single, Single> floorClip, WriteableBitmap image, WriteableBitmap dp)
        {

            var adjustedSkeleton = SkeletonOperations.FloorPlaneAdjustment(skeleton, floorClip);  //Current adjusted joint data

            if (PrevSkeletonPoints == null)
            {
                PrevSkeletonPoints = adjustedSkeleton;
                JumpStatus = 0;
                return;
            }

            Double value = Math.Min(adjustedSkeleton[(Int32)JointType.AnkleLeft].Y, adjustedSkeleton[(Int32)JointType.AnkleRight].Y);
            
            var trackedJoints = 0;    // counter for number of tracked joints

            /*
             * Each frame is checked to see how many joints are tracked on the frame's skeleton.
             */ 
            foreach (KeyValuePair<JointType, Joint> j in skeleton.Joints)
            {
                if (j.Value.TrackingState == TrackingState.Tracked)
                {
                    trackedJoints++;    // ... and increment the counter.
                }
            }

            // if there are 17 or more joints tracked AND the person is more than 3.7 meters away... 
            if (adjustedSkeleton[(Int32)JointType.SpineBase].Z > 3.7 && trackedJoints >= 17)
            {
                JumpStatus = 0;  // Reset if person is too far away
            }

            // if the jumpStatus is already 0, the distance is less than 3.6 but more than 2.5 meters away 
            else if (JumpStatus == 0 && adjustedSkeleton[(Int32)JointType.SpineBase].Z < 3.6 && adjustedSkeleton[(Int32)JointType.SpineBase].Z > 2.5)
            {
                if (value > (10 * ConvValue2))   // Is person on platform ?
                {
                    JumpStatus = 1;
                    PrevSkeletonPoints = adjustedSkeleton;
                }
                else
                    JumpStatus = 0;
            }
            else if (JumpStatus == 1)
            {
                // find difference between current hipCenter x coordinate and the previous one.
                var centerJointXDifferential =
                    Math.Abs(adjustedSkeleton[(Int32)JointType.SpineBase].X - PrevSkeletonPoints[(Int32)JointType.SpineBase].X) * ConvValue;
                // do the same for the hipCenter z coordinate
                var centerJointZDifferential =
                    (adjustedSkeleton[(Int32)JointType.SpineBase].Z - PrevSkeletonPoints[(Int32)JointType.SpineBase].Z) * ConvValue;
                // find the max height that either the left or right foot has been at in the frame.
                var footHeight = 
                    (Math.Max(adjustedSkeleton[(Int32)JointType.FootRight].Y, adjustedSkeleton[(Int32)JointType.FootLeft].Y)) * ConvValue;

                //go to zero, check x differential
                if (centerJointXDifferential > 14 || centerJointZDifferential > 12 || centerJointZDifferential < -30)
                {
                    JumpStatus = 0;
                }

                //stay at one, check x differential
                else if (footHeight > 4)
                {
                    JumpStatus = 1;
                }

                //go to two
                else
                {
                    JumpStatus = 2;
                    PrevSkeletonPoints = adjustedSkeleton;
                }
            }
            else if (JumpStatus == 2)
            {
                var centerJointXDifferential = Math.Abs(adjustedSkeleton[(Int32)JointType.SpineBase].X - PrevSkeletonPoints[(Int32)JointType.SpineBase].X) * ConvValue;
                var centerJointZDifferential = (adjustedSkeleton[(Int32)JointType.SpineBase].Z - PrevSkeletonPoints[(Int32)JointType.SpineBase].Z) * ConvValue;

                //go to zero, check x differential
                if (centerJointXDifferential > 14 || centerJointZDifferential > 12 || centerJointZDifferential < -48)
                {
                    JumpStatus = 0;
                }

                //go to 3 if person has moved forward at least 20 inches
                else if (centerJointZDifferential < -20)
                {
                    JumpStatus = 3;
                }

                //stay in 2
                else
                    JumpStatus = 2;

            }
            else if (JumpStatus == 3)
            {
                JumpStatus = 0;

                //Console.Write("Jump identified, extracting measures...");

                if (AdjustedSkeletonList.Count > MIN_COUNT)
                {
                    //---------------------------------------
                    // Convert list to array, make combined
                    // signal...
                    //---------------------------------------
                    var frameCount = 0;
                    var numJoints = Enum.GetNames(typeof(JointType)).Length;
                    var skelPointArrayCollection = new Single[numJoints][][];
                    for (var j = 0; j < numJoints; j++)
                    {
                        skelPointArrayCollection[j] = new Single[3][];
                        for (var d = 0; d < 3; d++)
                        {
                            skelPointArrayCollection[j][d] = new Single[AdjustedSkeletonList.Count];
                            frameCount = 0;
                            foreach (var adjustedSkeletonPoints in AdjustedSkeletonList)
                            {
                                switch (d)
                                {
                                    case 0:
                                        skelPointArrayCollection[j][d][frameCount] = adjustedSkeletonPoints[j].X;
                                        break;
                                    case 1:
                                        skelPointArrayCollection[j][d][frameCount] = adjustedSkeletonPoints[j].Y;
                                        break;
                                    case 2:
                                        skelPointArrayCollection[j][d][frameCount] = adjustedSkeletonPoints[j].Z;
                                        break;
                                }

                                frameCount++;
                            }
                        }
                    }

                    var combinedSignal = new Single[AdjustedSkeletonList.Count];
                    for (var j = 0; j < AdjustedSkeletonList.Count; j++)
                    {
                        combinedSignal[j] = (skelPointArrayCollection[(Int32)JointType.AnkleLeft][1][j] + skelPointArrayCollection[(Int32)JointType.AnkleRight][1][j] +
                                            skelPointArrayCollection[(Int32)JointType.FootLeft][1][j] + skelPointArrayCollection[(Int32)JointType.FootRight][1][j]) / 4;
                    }

                    //----------------------------------------------
                    // Filter the joint locations...
                    //----------------------------------------------
                    for (var j = 0; j < numJoints; j++)
                    {
                        for (var d = 0; d < 3; d++)
                        {
                            MedianFilter1Dimensional(ref skelPointArrayCollection[j][d], 3);
                            GaussianFilter1Dimensional(ref skelPointArrayCollection[j][d]);   //Predefined filter...
                        }
                    }
                    for (var j = 0; j < 10; j++)
                    {
                        combinedSignal = JumpIdentification.Anisodiff(combinedSignal, 2, 0.1, 0.225, 1);
                    }

                    //-----------------------------------------------
                    // Take derivative
                    //-----------------------------------------------
                    var combinedSignalDerivative = new Single[AdjustedSkeletonList.Count];
                    combinedSignalDerivative[0] = 0.0f;
                    var indexOfMinimum = 0;
                    for (var j = 1; j < AdjustedSkeletonList.Count; j++)
                    {
                        combinedSignalDerivative[j] = combinedSignal[j] - combinedSignal[j - 1];
                        if (combinedSignal[j] < combinedSignal[indexOfMinimum])
                            indexOfMinimum = j;  //Minimum...
                    }

                    //-----------------------------------------------
                    // Identify point of initial contact
                    //   - Have both ankles and feet vote on
                    //     point of initial contact
                    //-----------------------------------------------
                    var initialContactFrame = -1;
                    bool ifNormalDerivative = false;
                    for (var j = 0; j < AdjustedSkeletonList.Count; j++)
                    {
                        if (combinedSignalDerivative[j] > -0.019 && combinedSignal[j] < combinedSignal[indexOfMinimum] + 0.05)
                        {
                            ifNormalDerivative = true;
                            initialContactFrame = j - 1;
                            break;
                        }
                    }

                    if (ifNormalDerivative == false)
                    {
                        const String failure = "Not Difference in joint angles between init frame and peak frame!";
                        MessageBox.Show(failure, Error, MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    //---------------------------------------
                    // Identify point of peak flexion
                    //---------------------------------------
                    
                    var counter = initialContactFrame;
                    const String jumpSeg = "JumpSeg: ";
                    const String space = " ";
                    var previousY = 1000.0f;
                    Single currentY = (skelPointArrayCollection[(Int32)JointType.SpineBase][1][counter] +
                        skelPointArrayCollection[(Int32)JointType.HipLeft][1][counter] +
                        skelPointArrayCollection[(Int32)JointType.HipRight][1][counter]) / 3.0f;
                    counter += 1;
                    while (counter < frameCount && currentY < previousY)
                    {
                        previousY = currentY;
                        currentY = (skelPointArrayCollection[(Int32)JointType.SpineBase][1][counter] +
                            skelPointArrayCollection[(Int32)JointType.HipLeft][1][counter] +
                            skelPointArrayCollection[(Int32)JointType.HipRight][1][counter]) / 3.0f;
                        counter = counter + 1;
                    }

                    var peakFlexionFrame = counter - 2;  // Backup 1 frame for actual min, and 1 more for alg...

                    Console.WriteLine(jumpSeg + frameCount + space + initialContactFrame + space + peakFlexionFrame);

                    //------------------------------------------------
                    // Do we have a plausable identification ?
                    //------------------------------------------------
                    if (initialContactFrame > 0 && initialContactFrame < frameCount - 2 && peakFlexionFrame < frameCount - 2)
                    {

                        //-------------------------------------------------
                        // Compute displacement vector for jump 
                        //-------------------------------------------------
                        var initDisplacementVectorFrame = Math.Max(initialContactFrame - 25, 4);
                        var initDisplacementVectorPoint = new CameraSpacePoint
                        {
                            X = (skelPointArrayCollection[(Int32)JointType.SpineBase][0][initDisplacementVectorFrame] + skelPointArrayCollection[(Int32)JointType.HipLeft][0][initDisplacementVectorFrame] +
                                skelPointArrayCollection[(Int32) JointType.HipRight][0][initDisplacementVectorFrame])/3.0f,
                            Y = (skelPointArrayCollection[(Int32)JointType.SpineBase][1][initDisplacementVectorFrame] + skelPointArrayCollection[(Int32)JointType.HipLeft][1][initDisplacementVectorFrame] + skelPointArrayCollection[(Int32)JointType.
                                HipRight][1][initDisplacementVectorFrame]) / 3.0f,
                            Z = (skelPointArrayCollection[(Int32)JointType.SpineBase][2][initDisplacementVectorFrame] + skelPointArrayCollection[(Int32)JointType.HipLeft][2][initDisplacementVectorFrame] + skelPointArrayCollection[(Int32)JointType.
                                HipRight][2][initDisplacementVectorFrame]) / 3.0f
                        };                              
                        var endDisplacementVectorPoint = new CameraSpacePoint();                  
                        initDisplacementVectorFrame = AdjustedSkeletonList.Count - 4;
                        endDisplacementVectorPoint.X = (skelPointArrayCollection[(Int32)JointType.SpineBase][0][initDisplacementVectorFrame] + skelPointArrayCollection[(Int32)JointType.HipLeft][0][initDisplacementVectorFrame] + skelPointArrayCollection[(Int32)JointType.HipRight][0][initDisplacementVectorFrame]) / 3.0f;
                        endDisplacementVectorPoint.Y = (skelPointArrayCollection[(Int32)JointType.SpineBase][1][initDisplacementVectorFrame] + skelPointArrayCollection[(Int32)JointType.HipLeft][1][initDisplacementVectorFrame] + skelPointArrayCollection[(Int32)JointType.HipRight][1][initDisplacementVectorFrame]) / 3.0f;
                        endDisplacementVectorPoint.Z = (skelPointArrayCollection[(Int32)JointType.SpineBase][2][initDisplacementVectorFrame] + skelPointArrayCollection[(Int32)JointType.HipLeft][2][initDisplacementVectorFrame] + skelPointArrayCollection[(Int32)JointType.HipRight][2][initDisplacementVectorFrame]) / 3.0f;

                        var jumpDisplacementVector = SubtractSkelPoint(endDisplacementVectorPoint, initDisplacementVectorPoint);


                        //-------------------------------------------------
                        // Measure features at points of interest...
                        // Make our window with skeletons and info...
                        //-------------------------------------------------
                        var eot = new EndOfTest(ref SensorManager);

                        var frame = 0;
                        Double initialKneeAnkleSepRatio = 0, 
                            peakKneeAnkleSepRatio = 0, 
                            rightValgusInitCont = 0, 
                            rightValgusPeakFlex = 0, 
                            leftValgusInitCont = 0, 
                            leftValgusPeakFlex = 0;
                        Int32 initContCounter = 0, peakFlexCounter = 0;
                        foreach (CameraSpacePoint[] adjustedSkeletonPoint in AdjustedSkeletonList)
                        {
                            LinkedList<WriteableBitmap>.Enumerator imageListEnum = ImageList.GetEnumerator();
                            LinkedList<Body>.Enumerator origSkelListEnum = OriginalSkeletonList.GetEnumerator();
                            LinkedList<Dictionary<JointType, Joint>>.Enumerator origBodyJointsListEnum = OriginalBodyJointsList.GetEnumerator();
                            for (Int32 j = 0; j < frame; j++)
                            {
                                imageListEnum.MoveNext();
                                origSkelListEnum.MoveNext();
                                origBodyJointsListEnum.MoveNext();
                            }

                            var kneeMeasures = GetKneeMeasures(skelPointArrayCollection, frame, jumpDisplacementVector, endDisplacementVectorPoint);
                            //get the average of three frames, the frame before, the current frame, and the frame after.
                            if (frame >= initialContactFrame-1 && frame <= initialContactFrame+1)
                            {
                                initialKneeAnkleSepRatio += kneeMeasures[2];
                                rightValgusInitCont += kneeMeasures[1];
                                leftValgusInitCont += kneeMeasures[0];
                                initContCounter++;

                                if( frame == initialContactFrame )
                                {   
                                    //eot.DrawSkeleton(adjustedSkeletonPoint, 0, imageListEnum.Current, origSkelListEnum.Current);
                                    eot.DrawSkeleton(adjustedSkeletonPoint, 0, imageListEnum.Current, origBodyJointsListEnum.Current);
                                }
                                    
                            }
                            if ( frame >= peakFlexionFrame - 1 && frame <= peakFlexionFrame + 1)
                            {
                                peakKneeAnkleSepRatio += kneeMeasures[2];
                                rightValgusPeakFlex += kneeMeasures[1];
                                leftValgusPeakFlex += kneeMeasures[0];
                                peakFlexCounter++;

                                if (frame == peakFlexionFrame)
                                {
                                    //eot.DrawSkeleton(adjustedSkeletonPoint, 1, imageListEnum.Current, origSkelListEnum.Current);
                                    eot.DrawSkeleton(adjustedSkeletonPoint, 1, imageListEnum.Current, origBodyJointsListEnum.Current);
                                }
                            }
                            frame++;
                        }

                        initialKneeAnkleSepRatio /= initContCounter;
                        rightValgusInitCont /= initContCounter;
                        leftValgusInitCont /= initContCounter;

                        peakKneeAnkleSepRatio /= peakFlexCounter;
                        rightValgusPeakFlex /= peakFlexCounter;
                        leftValgusPeakFlex /= peakFlexCounter;

                        eot.AddNumbers(peakKneeAnkleSepRatio, rightValgusPeakFlex, leftValgusPeakFlex, 1);
                        eot.AddNumbers(initialKneeAnkleSepRatio, rightValgusInitCont, leftValgusInitCont, 0);
                        
                        //---------------------------
                        // Finish console output
                        //---------------------------
                        eot.Show();
                        
                        var jump = new JumpResults();   // the jump that all of the frames will go into.
                        foreach (var skeletonPointArray in AdjustedSkeletonList)
                        {
                            LinkedList<Body>.Enumerator origSkelListEnum = 
                                OriginalSkeletonList.GetEnumerator();    // make the original skeleton list searchable
                            LinkedList<Tuple<Single, Single, Single, Single>>.Enumerator floorPlaneListEnum = 
                                FloorPlaneList.GetEnumerator();    // make the floor plane list searchable

                            
                            jump.InitContactImage = eot.DrawingImage1;
                            jump.PeakFlexionImage = eot.DrawingImage2;
                            for (Int32 j = 0; j < frame; j++)
                            {
                                origSkelListEnum.MoveNext();
                                floorPlaneListEnum.MoveNext();
                            }
                            
                            var fD = new FrameData();
                            //fD.OrigSkeleton = oske.Current;
                            fD.OrigSkeletonArray = new CameraSpacePoint[25];
                            fD.OrigJointOrientationArray = new Vector4[25];
                            //********************************************
                            //Where we put the stuff in the OrigSkeletonPoints array
                            //for each joint, put it in the equivalent index in the OrigSkeletonArray
                            if (origSkelListEnum.Current != null)
                            {
                                fD.OrigSkeletonArray[0] = origSkelListEnum.Current.Joints[(JointType) 0].Position; // hipCenter
                                fD.OrigSkeletonArray[1] = origSkelListEnum.Current.Joints[(JointType) 1].Position; // spine
                                fD.OrigSkeletonArray[2] = origSkelListEnum.Current.Joints[(JointType) 2].Position; // shoulderCenter
                                fD.OrigSkeletonArray[3] = origSkelListEnum.Current.Joints[(JointType) 3].Position; // head
                                fD.OrigSkeletonArray[4] = origSkelListEnum.Current.Joints[(JointType) 4].Position; // shoulderLeft
                                fD.OrigSkeletonArray[5] = origSkelListEnum.Current.Joints[(JointType) 5].Position; // elbowLeft
                                fD.OrigSkeletonArray[6] = origSkelListEnum.Current.Joints[(JointType) 6].Position; // wristLeft
                                fD.OrigSkeletonArray[7] = origSkelListEnum.Current.Joints[(JointType) 7].Position; // handLeft
                                fD.OrigSkeletonArray[8] = origSkelListEnum.Current.Joints[(JointType) 8].Position; // shoulderRight
                                fD.OrigSkeletonArray[9] = origSkelListEnum.Current.Joints[(JointType) 9].Position; // elbowRight
                                fD.OrigSkeletonArray[10] = origSkelListEnum.Current.Joints[(JointType) 10].Position; // wristRight
                                fD.OrigSkeletonArray[11] = origSkelListEnum.Current.Joints[(JointType) 11].Position; // handRight
                                fD.OrigSkeletonArray[12] = origSkelListEnum.Current.Joints[(JointType) 12].Position; // hipLeft
                                fD.OrigSkeletonArray[13] = origSkelListEnum.Current.Joints[(JointType) 13].Position; // kneeLeft
                                fD.OrigSkeletonArray[14] = origSkelListEnum.Current.Joints[(JointType) 14].Position; // ankleLeft
                                fD.OrigSkeletonArray[15] = origSkelListEnum.Current.Joints[(JointType) 15].Position; // footLeft
                                fD.OrigSkeletonArray[16] = origSkelListEnum.Current.Joints[(JointType) 16].Position; // hipRight
                                fD.OrigSkeletonArray[17] = origSkelListEnum.Current.Joints[(JointType) 17].Position; // kneeRight
                                fD.OrigSkeletonArray[18] = origSkelListEnum.Current.Joints[(JointType) 18].Position; // ankleRight
                                fD.OrigSkeletonArray[19] = origSkelListEnum.Current.Joints[(JointType) 19].Position; // footRight

                                fD.OrigSkeletonArray[20] = origSkelListEnum.Current.Joints[(JointType) 20].Position; 
                                fD.OrigSkeletonArray[21] = origSkelListEnum.Current.Joints[(JointType) 21].Position; 
                                fD.OrigSkeletonArray[22] = origSkelListEnum.Current.Joints[(JointType) 22].Position; 
                                fD.OrigSkeletonArray[23] = origSkelListEnum.Current.Joints[(JointType) 23].Position; 
                                fD.OrigSkeletonArray[24] = origSkelListEnum.Current.Joints[(JointType) 24].Position; 
                                /*
                                for (var count = 0; count < oske.Current.Joints.Count; count++)
                                {
                                    fD.OrigSkeletonArray[count].X = Mathf.Round fD.OrigSkeletonArray[count].X
                                }*/
                            }

                            if (origSkelListEnum.Current != null)
                            {
                                fD.OrigJointOrientationArray[0] = origSkelListEnum.Current.JointOrientations[(JointType)0].Orientation; // hipCenter
                                fD.OrigJointOrientationArray[1] = origSkelListEnum.Current.JointOrientations[(JointType)1].Orientation; // spine
                                fD.OrigJointOrientationArray[2] = origSkelListEnum.Current.JointOrientations[(JointType)2].Orientation; // shoulderCenter
                                fD.OrigJointOrientationArray[3] = origSkelListEnum.Current.JointOrientations[(JointType)3].Orientation; // head
                                fD.OrigJointOrientationArray[4] = origSkelListEnum.Current.JointOrientations[(JointType)4].Orientation; // shoulderLeft
                                fD.OrigJointOrientationArray[5] = origSkelListEnum.Current.JointOrientations[(JointType)5].Orientation; // elbowLeft
                                fD.OrigJointOrientationArray[6] = origSkelListEnum.Current.JointOrientations[(JointType)6].Orientation; // wristLeft
                                fD.OrigJointOrientationArray[7] = origSkelListEnum.Current.JointOrientations[(JointType)7].Orientation; // handLeft
                                fD.OrigJointOrientationArray[8] = origSkelListEnum.Current.JointOrientations[(JointType)8].Orientation; // shoulderRight
                                fD.OrigJointOrientationArray[9] = origSkelListEnum.Current.JointOrientations[(JointType)9].Orientation; // elbowRight
                                fD.OrigJointOrientationArray[10] = origSkelListEnum.Current.JointOrientations[(JointType)10].Orientation; // wristRight
                                fD.OrigJointOrientationArray[11] = origSkelListEnum.Current.JointOrientations[(JointType)11].Orientation; // handRight
                                fD.OrigJointOrientationArray[12] = origSkelListEnum.Current.JointOrientations[(JointType)12].Orientation; // hipLeft
                                fD.OrigJointOrientationArray[13] = origSkelListEnum.Current.JointOrientations[(JointType)13].Orientation; // kneeLeft
                                fD.OrigJointOrientationArray[14] = origSkelListEnum.Current.JointOrientations[(JointType)14].Orientation; // ankleLeft
                                fD.OrigJointOrientationArray[15] = origSkelListEnum.Current.JointOrientations[(JointType)15].Orientation; // footLeft
                                fD.OrigJointOrientationArray[16] = origSkelListEnum.Current.JointOrientations[(JointType)16].Orientation; // hipRight
                                fD.OrigJointOrientationArray[17] = origSkelListEnum.Current.JointOrientations[(JointType)17].Orientation; // kneeRight
                                fD.OrigJointOrientationArray[18] = origSkelListEnum.Current.JointOrientations[(JointType)18].Orientation; // ankleRight
                                fD.OrigJointOrientationArray[19] = origSkelListEnum.Current.JointOrientations[(JointType)19].Orientation; // footRight
                                fD.OrigJointOrientationArray[20] = origSkelListEnum.Current.JointOrientations[(JointType)20].Orientation;
                                fD.OrigJointOrientationArray[21] = origSkelListEnum.Current.JointOrientations[(JointType)21].Orientation;
                                fD.OrigJointOrientationArray[22] = origSkelListEnum.Current.JointOrientations[(JointType)22].Orientation;
                                fD.OrigJointOrientationArray[23] = origSkelListEnum.Current.JointOrientations[(JointType)23].Orientation;
                                fD.OrigJointOrientationArray[24] = origSkelListEnum.Current.JointOrientations[(JointType)24].Orientation;
                                /*
                                for (var count = 0; count < oske.Current.Joints.Count; count++)
                                {
                                    fD.OrigSkeletonArray[count].X = Mathf.Round fD.OrigSkeletonArray[count].X
                                }*/
                            }

                            fD.TimeStamp = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;   // time stamp, seconds since UNIX epoch
                            

                            //********************************************
                            fD.AdjustedSkeleton = skeletonPointArray;
                            fD.FloorPlane = floorPlaneListEnum.Current;
                            //fD.DepthImage = dpe.Current;
                            
                            jump.FrameDataList.Add(fD);
                            
                        }

                        jump.InitKasr = Convert.ToSingle(initialKneeAnkleSepRatio);
                        jump.InitLeftValg = Convert.ToSingle(leftValgusInitCont);
                        jump.InitRightValg = Convert.ToSingle(rightValgusInitCont);
                        jump.InitFrame = Convert.ToUInt32(initialContactFrame);
                        

                        
                        jump.PeakKasr = Convert.ToSingle(peakKneeAnkleSepRatio);
                        jump.PeakLeftValg = Convert.ToSingle(leftValgusPeakFlex);
                        jump.PeakRightValg = Convert.ToSingle(rightValgusPeakFlex);
                        jump.PeakFrame = Convert.ToUInt32(peakFlexionFrame);  // Make pF and iC UInts?
                        
                        SensorManager.TestSubjectJumps.Jumps.Add(jump);
                    }
                    else
                    {
                        const String failure = "... Failed to correctly identify points of interest!";
                        MessageBox.Show(failure, Error, MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    String failure = "... not enough data! only got " + AdjustedSkeletonList.Count.ToString() + " frames";
                    MessageBox.Show(failure, Error, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            if (JumpStatus == 0)
            {
                AdjustedSkeletonList.Clear();
                ImageList.Clear();
                OriginalSkeletonList.Clear();
                FloorPlaneList.Clear();
                dp_list.Clear();
                OriginalBodyJointsList.Clear();
            }
            else
            {
                if (JumpStatus == 1 && AdjustedSkeletonList.Count > 180)
                {
                    AdjustedSkeletonList.RemoveFirst();
                    ImageList.RemoveFirst();
                    OriginalSkeletonList.RemoveFirst();
                    dp_list.RemoveFirst();
                    FloorPlaneList.RemoveFirst();
                }

                AdjustedSkeletonList.AddLast(adjustedSkeleton);
                ImageList.AddLast(image.Clone());
                // send to finalize window 
                dp_list.AddLast(dp.Clone());
                FloorPlaneList.AddLast(floorClip);
                ///var tempSkeleton = new Body(); //expired sentence
                Body tempSkeleton = null;
                SkeletonOperations.CopySkeleton(ref tempSkeleton, skeleton);
                OriginalSkeletonList.AddLast(tempSkeleton);

                //record body joints
                Dictionary<JointType, Joint> jointsToSave = new Dictionary<JointType, Joint>();
                foreach (JointType j in Enum.GetValues(typeof(JointType)))
                {
                    jointsToSave.Add(j, skeleton.Joints[j]);
                }
                OriginalBodyJointsList.AddLast(jointsToSave);
            }
        }

        //----------------------------------
        //Median filter - in place
        //----------------------------------
        /// <summary>
        /// 
        /// </summary>
        /// <param name="inputArray"></param>
        /// <param name="halfWindowLength"></param>
        private static void MedianFilter1Dimensional(ref Single[] inputArray, Int32 halfWindowLength)
        {

            if (inputArray.Length <= 3)
                return;

            var filterSize = (halfWindowLength * 2) + 1;
            var bufferList = new Single[filterSize];
            var tempArray = new Single[inputArray.Length + filterSize - 1];
            Array.Copy(inputArray, 0, tempArray, 0, halfWindowLength);         //Quick and easy (bad) boundary handling..
            Array.Copy(inputArray, 0, tempArray, halfWindowLength, inputArray.Length);
            Array.Copy(inputArray, inputArray.Length - halfWindowLength, tempArray, inputArray.Length + halfWindowLength, halfWindowLength);

            for (var j = 0; j < inputArray.Length; j++)
            {
                Array.Copy(tempArray, j, bufferList, 0, filterSize);
                Array.Sort(bufferList, 0, filterSize);
                inputArray[j] = bufferList[halfWindowLength];
            }
        }

        //----------------------------------
        // Fixed gaussian - in place
        //----------------------------------
        /// <summary>
        /// 
        /// </summary>
        /// <param name="inputArray"></param>
        private static void GaussianFilter1Dimensional(ref Single[] inputArray)
        {
            if (inputArray.Length <= 3)
                return;

            // Gaussian filter with window size of seven, standard deviation of 1 or 2. These are coefficients 
            Single[] gaussFilterCoefficients = { 0.017988f, 0.089096f, 0.232692f, 0.320448f, 0.232692f, 0.089096f, 0.0179880f };
            var tempArray = new Single[inputArray.Length + 6];
            Array.Copy(inputArray, 0, tempArray, 0, 3);           //Quick and easy (bad) boundary handling..
            Array.Copy(inputArray, 0, tempArray, 3, inputArray.Length);
            Array.Copy(inputArray, inputArray.Length - 3, tempArray, inputArray.Length + 3, 3);

            for (var j = 0; j < inputArray.Length; j++)
            {
                inputArray[j] = 0.0f;
                for (var d = 0; d < 7; d++)
                {
                    inputArray[j] += gaussFilterCoefficients[d] * tempArray[j + d];
                }
            }
        }

        //------------------------------------------------
        // Compute angles/measures on frontal plane
        //   
        //     output has 7 values:
        //       [0]   left valgus  (degrees)
        //       [1]   right valgus (degrees)
        //       [2]   knee/ankle sep ratio
        //       [3]   knee sep (mm)
        //       [4]   ankle sep (mm)
        //       [5]   left knee X pos (mm)
        //       [6]   right knee X pos (mm)
        //------------------------------------------------
        /// <summary>
        /// Computes angles and measures on a frontal plane.
        /// </summary>
        /// <param name="skelPointArrayCollection">Body point array</param>
        /// <param name="displacementVector"></param>
        /// <param name="pointOnFrontalPlane"></param>
        /// <param name="frameCount"></param>
        /// <returns>Returns an array with:
        /// [0] left valgus (degrees)
        /// [1] right valgus (degrees)
        /// [2] knee/ankle separation ratio
        /// [3] knee separation (millimeters)
        /// [4] ankle separation (millimeters)
        /// [5] left knee X position (millimeters)
        /// [6] right knee X position (millimeters)
        /// </returns>
        private Double[] GetKneeMeasures(float[][][] skelPointArrayCollection, Int32 frameCount, CameraSpacePoint displacementVector, CameraSpacePoint pointOnFrontalPlane)
        {

            var kneeMeasures = new Double[7];

            //--------------------------------------------------
            //  Compute unit normal vector to frontal plane
            //      - Ignore the vertical axis (Y in this case)
            //--------------------------------------------------
            var frontalPlaneNormalVector = new CameraSpacePoint();
            var vectorMagnitude = Math.Sqrt((displacementVector.X * displacementVector.X) + 
                (displacementVector.Z * displacementVector.Z));
            frontalPlaneNormalVector.X = Convert.ToSingle((displacementVector.X / vectorMagnitude));
            frontalPlaneNormalVector.Y = 0;
            frontalPlaneNormalVector.Z = (float)(displacementVector.Z / vectorMagnitude);

            //System.Diagnostics.Debug.WriteLine("nv_fp:  " + nv_fp.X + " " + nv_fp.Y + " " + nv_fp.Z);

            //--------------------------------------------
            //  Rotate 90 degrees to get "left" direction
            //--------------------------------------------
            var leftNormalVector = new CameraSpacePoint();
            leftNormalVector.X = (float)(Math.Cos(PiDivide) * frontalPlaneNormalVector.X - Math.Sin(PiDivide) * 
                frontalPlaneNormalVector.Z);
            leftNormalVector.Y = 0;
            leftNormalVector.Z = (float)(Math.Sin(PiDivide) * frontalPlaneNormalVector.X + Math.Cos(PiDivide) * 
                frontalPlaneNormalVector.Z);

            //System.Diagnostics.Debug.WriteLine("nv_left:  " + nv_left.X + " " + nv_left.Y + " " + nv_left.Z);

            //-----------------------------------------
            // Project joints onto frontal plane
            //     p_q = q - dot(q - fp, nv_fp)*nv_fp;
            //-----------------------------------------
            var skeletonFrontalPlaneProjection = new CameraSpacePoint[Enum.GetNames(typeof(JointType)).Length];
            for (var j = 0; j < Enum.GetNames(typeof(JointType)).Length; j++)
            {
                var skelPoint = new CameraSpacePoint{
                X = skelPointArrayCollection[j][0][frameCount],
                Y = skelPointArrayCollection[j][1][frameCount],
                Z = skelPointArrayCollection[j][2][frameCount]
                };

                var dotProduct = DotProductSkelPoint(SubtractSkelPoint(skelPoint, pointOnFrontalPlane), frontalPlaneNormalVector);
                skeletonFrontalPlaneProjection[j] = SubtractSkelPoint(skelPoint, MultiplySkelPoint(frontalPlaneNormalVector, dotProduct));

                //System.Diagnostics.Debug.WriteLine(skp.X + " " + skp.Y + " " + skp.Z);
                //System.Diagnostics.Debug.WriteLine(q_sk[j].X + " " + q_sk[j].Y + " " + q_sk[j].Z);
            }

            //------------------------------------------------
            // Left Valgus
            //------------------------------------------------
            vectorMagnitude = VectorNormSkelPoint(SubtractSkelPoint(skeletonFrontalPlaneProjection[(int)JointType.AnkleLeft], 
                skeletonFrontalPlaneProjection[(int)JointType.KneeLeft]));
            var vector1 = DivideSkelPoint(SubtractSkelPoint(skeletonFrontalPlaneProjection[(int)JointType.AnkleLeft], 
                skeletonFrontalPlaneProjection[(int)JointType.KneeLeft]), vectorMagnitude);

            var vector2 = new CameraSpacePoint
            {
                //Unit vector straight down...
                X = 0, 
                Y = -1, 
                Z = 0 
            };
            kneeMeasures[0] = Math.Acos(DotProductSkelPoint(vector1, vector2)) * 180.0 / Math.PI;

            // In (-) or Out (+)  ??
            var leftAnkleXProjection = DotProductSkelPoint(skeletonFrontalPlaneProjection[(int)JointType.AnkleLeft], leftNormalVector);
            var leftKneeXProjection = DotProductSkelPoint(skeletonFrontalPlaneProjection[(int)JointType.KneeLeft], leftNormalVector);
            if (leftAnkleXProjection < leftKneeXProjection)
            {
                //Knee bent in (-)
                kneeMeasures[0] = -kneeMeasures[0];
            }

            //------------------------------------------------
            // Right Valgus
            //------------------------------------------------
            vectorMagnitude = VectorNormSkelPoint(SubtractSkelPoint(skeletonFrontalPlaneProjection[(int)JointType.AnkleRight], skeletonFrontalPlaneProjection[(int)JointType.KneeRight]));
            vector1 = DivideSkelPoint(SubtractSkelPoint(skeletonFrontalPlaneProjection[(int)JointType.AnkleRight], skeletonFrontalPlaneProjection[(int)JointType.KneeRight]), vectorMagnitude);

            vector2 = new CameraSpacePoint
            {
                //Unit vector straight down...
                X = 0, 
                Y = -1, 
                Z = 0 
            };
            kneeMeasures[1] = Math.Acos(DotProductSkelPoint(vector1, vector2)) * 180.0 / Math.PI; //In degrees....

            // In (-) or Out (+)  ??
            var rightAnkleXProjection = DotProductSkelPoint(skeletonFrontalPlaneProjection[(int)JointType.AnkleRight], leftNormalVector);
            var rightKneeXProjection = DotProductSkelPoint(skeletonFrontalPlaneProjection[(int)JointType.KneeRight], leftNormalVector);
            if (rightAnkleXProjection > rightKneeXProjection)
            {
                //Knee bent in (-)
                kneeMeasures[1] = -kneeMeasures[1];
            }

            //----------------------------------------------
            // Knee / ankle seperation ratio
            //----------------------------------------------
            Double kneeSep = Math.Abs(leftKneeXProjection - rightKneeXProjection);
            Double ankleSep = Math.Abs(leftAnkleXProjection - rightAnkleXProjection);
            kneeMeasures[2] = kneeSep / ankleSep;
            kneeMeasures[3] = kneeSep;
            kneeMeasures[4] = ankleSep;

            //---------------------------------------------
            // Left/right knee X position on frontal plane
            //---------------------------------------------
            kneeMeasures[5] = leftKneeXProjection;
            kneeMeasures[6] = rightKneeXProjection;

            return kneeMeasures;
        }


        #region Erik's Helpers
        //------------------------------------------------
        // Helpers
        //------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <returns></returns>
        static Double DotProductSkelPoint(CameraSpacePoint point1, CameraSpacePoint point2)
        {
            return (point1.X * point2.X + point1.Y * point2.Y + point1.Z * point2.Z);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <returns></returns>
        static CameraSpacePoint SubtractSkelPoint(CameraSpacePoint point1, CameraSpacePoint point2)
        {
            var np = new CameraSpacePoint
            {
                X = point1.X - point2.X,
                Y = point1.Y - point2.Y,
                Z = point1.Z - point2.Z
            };
            return np;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        static CameraSpacePoint DivideSkelPoint(CameraSpacePoint point1, Double val)
        {
            var np = new CameraSpacePoint
            {
                X = Convert.ToSingle(point1.X/val),
                Y = Convert.ToSingle(point1.Y/val),
                Z = Convert.ToSingle(point1.Z/val)
            };
            return np;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="scalarVal"></param>
        /// <returns></returns>
        static CameraSpacePoint MultiplySkelPoint(CameraSpacePoint point1, Double scalarVal)
        {
            var np = new CameraSpacePoint
            {
                X = (Single)(point1.X * scalarVal),
                Y = (Single)(point1.Y * scalarVal),
                Z = (Single)(point1.Z * scalarVal)
            };
            return np;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="point1"></param>
        /// <returns></returns>
        Double VectorNormSkelPoint(CameraSpacePoint point1)
        {
            return Math.Sqrt(point1.X * point1.X + point1.Y * point1.Y + point1.Z * point1.Z);
        }
        #endregion
    }

}
