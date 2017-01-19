//------------------------------------------------------------------------------
// <copyright file="FrameData.cs" company="University of Missouri">
//     Copyright (c) Curators of the University of Missouri.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using Microsoft.Kinect;

/*
 * 
 * FrameData is the lowest object in its hierarchy. There should be one FrameData object for each frame
 * sent by the Kinect, at a rate of roughly 30 frames/second. These objects are stored in the 
 * FrameDataBuffer object, the next object up in the hierarchy.
 * 
 */
namespace ACL_Gold_x64.Models
{
    public class FrameData
    {
        public double TimeStamp { get; set; }

        /// <summary>
        /// Property for the skeleton point array containing each of the 20 joints in the non-adjusted skeleton. Joint
        /// information includes only the x, y, and z positions of each joint.
        /// </summary>
        public CameraSpacePoint[] OrigSkeletonArray { get; set; }

        /// <summary>
        /// Property for the skeleton adjusted for the floor plane.
        /// </summary>
        public CameraSpacePoint[] AdjustedSkeleton { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Vector4[] OrigJointOrientationArray { get; set; }

        /// <summary>
        /// Property for the floor plane
        /// </summary>
        public Tuple<float, float, float, float> FloorPlane { get; set; }
    }
}
