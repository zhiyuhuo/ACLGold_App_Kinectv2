//------------------------------------------------------------------------------
// <copyright file="SkeletonOperations.cs" company="University of Missouri">
//     Copyright (c) Curators of the University of Missouri.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using Microsoft.Kinect;

namespace ACL_Gold_x64.Controllers
{
    class SkeletonOperations
    {
        /// <summary>
        /// Copies skeletons over to a new skeleton.
        /// </summary>
        /// <param name="skeleton1">Original skeleton</param>
        /// <param name="skeleton2">Target skeleton</param>
        public static void CopySkeleton(ref Body skeleton1, Body skeleton2)
        {
            //if(skeleton1 == null)
            //{
            //    skeleton1 = skeleton2;
            //}
            //foreach(JointType j in Enum.GetValues(typeof(JointType)))
            //{
            //    skeleton1.Joints[j] = skeleton2.Joints[j];
            //}
            skeleton1 = skeleton2;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="skeleton1"></param>
        /// <param name="floorClip"></param>
        /// <returns></returns>
        public static CameraSpacePoint[] FloorPlaneAdjustment(Body skeleton1, Tuple<Single, Single, Single, Single> floorClip)
        {
            //FOR ERIK: what does xd stand for? first vector
            var vector1 = new Single[3];
            vector1[0] = floorClip.Item1;
            vector1[1] = floorClip.Item2;
            vector1[2] = floorClip.Item3;
            
            //FOR ERIK: what does x2 stand for? second vector that defines a coordinate system with respect to the ground plane
            var vector2 = new Single[3];
            vector2[0] = 0;
            vector2[1] = vector1[0];
            vector2[2] = vector1[1];

            //FOR ERIK: what does vmag stand for? vertical magnitude
            var verticalMagnitude = Math.Sqrt((vector2[1] * vector2[1]) + (vector2[2] * vector2[2]));
            vector2[0] = Convert.ToSingle(vector2[0] / verticalMagnitude);
            vector2[1] = Convert.ToSingle(vector2[1] / verticalMagnitude);
            vector2[2] = Convert.ToSingle(vector2[2] / verticalMagnitude);

            //FOR ERIK: what does x3 stand for? third vector
            var vector3 = new Single[3];
            vector3[0] = vector1[1] * vector2[2] - vector1[2] * vector2[1];
            vector3[1] = vector1[2] * vector2[0] - vector1[0] * vector2[2];
            vector3[2] = vector1[0] * vector2[1] - vector1[1] * vector2[0];
            if(vector3[0] < 0)
            {
                //Need it to go positive x...
                vector3[0] = -vector3[0];
                vector3[1] = -vector3[1];
                vector3[2] = -vector3[2];
            }

            //FOR ERIK: what does rData stand for? rotation matrix data
            var rotationMatData = new Single[9];
            //Right
            rotationMatData[0] = vector3[0];
            rotationMatData[1] = vector3[1];
            rotationMatData[2] = vector3[2];
            //Up, since Y is up
            rotationMatData[3] = vector1[0];
            rotationMatData[4] = vector1[1];
            rotationMatData[5] = vector1[2];
            //Forward
            rotationMatData[6] = vector2[0];
            rotationMatData[7] = vector2[1];
            rotationMatData[8] = vector2[2];

            //FOR ERIK: what does nj stand for? number of joints
            var jointCount = Enum.GetNames(typeof(JointType)).Length;

            //FOR ERIK: what does ts stand for? temp skeleton point array
            var tempSkelPointArray = new CameraSpacePoint[jointCount];

            for (var jointIndex = 0; jointIndex < jointCount;jointIndex++)
            {
                tempSkelPointArray[jointIndex] = AdjustJoint(skeleton1.Joints[(JointType)jointIndex], rotationMatData, floorClip.Item4);
            }
            //Console.WriteLine("floorclip[4]={0}", floorClip.Item4);
            
            //Console.WriteLine("rdata = {0} {1} {2} {3} {4} {5} {6} {7} {8}", rData[0], rData[1], rData[2], rData[3], rData[4], rData[5], rData[6], rData[7], rData[8]);
            
            return tempSkelPointArray;
        }
        
        //FOR ERIK: what does fY stand for? height of kinect off of the floor
        /// <summary>
        /// 
        /// </summary>
        /// <param name="j"></param>
        /// <param name="rData"></param>
        /// <param name="fY"></param>
        /// <returns></returns>
        public static CameraSpacePoint AdjustJoint(Joint j, Single[] rData, Single fY)
        {
            var x = rData[0] * j.Position.X + rData[1] * j.Position.Y + rData[2] * j.Position.Z;
            var y = rData[3] * j.Position.X + rData[4] * j.Position.Y + rData[5] * j.Position.Z + fY;
            var z = rData[6] * j.Position.X + rData[7] * j.Position.Y + rData[8] * j.Position.Z;

            //FOR ERIK: what does tj stand for? temporary joint or new joint
            var tempJoint = new CameraSpacePoint
            {
                X = x, 
                Y = y, 
                Z = z
            };
            

            return tempJoint;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="kneeJoint"></param>
        /// <param name="ankleJoint"></param>
        /// <returns></returns>
        public static Double KneeValgus(Joint kneeJoint, Joint ankleJoint)
        {
            Double vector1X = ankleJoint.Position.X - kneeJoint.Position.X;
            Double vector1Y = ankleJoint.Position.Y - kneeJoint.Position.Y;
            const Double vector2X = 0;
            var vector2Y = vector1Y;
            var vector1Magn = Math.Sqrt(vector1X * vector1X + vector1Y * vector1Y);
            var vector2Magn = Math.Sqrt(vector2X * vector2X + vector2Y * vector2Y);
            var dotProduct = vector1X * vector2X + vector1Y * vector2Y;
            var cosineA = dotProduct / (vector1Magn * vector2Magn);
            var angle = 180.0 * Math.Acos(cosineA) / 3.14159;
            if (vector1X < 0)
                angle = -angle;
            return angle;
        }

    }
}
