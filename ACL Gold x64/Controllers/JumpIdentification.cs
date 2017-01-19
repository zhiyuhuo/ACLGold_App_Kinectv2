//------------------------------------------------------------------------------
// <copyright file="JumpIdentification.cs" company="University of Missouri">
//     Copyright (c) Curators of the University of Missouri.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Windows.Media.Imaging;

using Microsoft.Kinect;

namespace ACL_Gold_x64.Controllers
{
    class JumpIdentification
    {
        public LinkedList<CameraSpacePoint[]> SkeletonList;
        public LinkedList<WriteableBitmap> ImageList; 
        public LinkedList<Body> OriginalSkeletonList;
        public CameraSpacePoint[] PreviousSkeleton;
        public Int32 MachineStatus;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="machineStatus"></param>
        /// <param name="previousSkeleton"></param>
        public JumpIdentification(Int32 machineStatus = -1, CameraSpacePoint[] previousSkeleton = null)
        {
            PreviousSkeleton = previousSkeleton;
            MachineStatus = machineStatus;
            SkeletonList = new LinkedList<CameraSpacePoint[]>();
            ImageList = new LinkedList<WriteableBitmap>();
            OriginalSkeletonList = new LinkedList<Body>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="inputArray"></param>
        /// <param name="iterations"></param>
        /// <param name="kappa"></param>
        /// <param name="lambda"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public static Single[] Anisodiff(Single[] inputArray, Int32 iterations, Double kappa, Double lambda, Byte option)
        {
            var outputArray = inputArray;
            var diff1 = new Single[inputArray.Length + 2];
            var deltaN= new Single[inputArray.Length];
            var deltaS= new Single[inputArray.Length];
            Single condN = 0; 
            Single condS = 0;
            for (var i = 1; i <= iterations; i++)
            {
                diff1[0] = 0;
                for(var j = 1; j <= inputArray.Length; j++)
                {
                    diff1[j] = outputArray[j - 1];
                }

                diff1[inputArray.Length + 1] = 0;
                for (var k = 0; k < inputArray.Length; k++)
                {
                    switch (option)
                    {
                        case 1:
                            deltaN[k] = diff1[k] - outputArray[k];
                            deltaS[k] = diff1[k + 2] - outputArray[k];
                            condN = (Single)Math.Exp(-(Math.Pow((deltaN[k] / kappa), 2)));
                            condS = (Single)Math.Exp(-(Math.Pow((deltaS[k] / kappa), 2)));
                            break;
                        case 2:
                            deltaN[k] = diff1[k] - outputArray[k];
                            deltaS[k] = diff1[k + 2] - outputArray[k];
                            condN = Convert.ToSingle(1/(1+(Math.Pow((deltaN[k] / kappa), 2))));
                            condS = Convert.ToSingle(1/(1+(Math.Pow((deltaS[k] / kappa), 2))));
                            break;
                    }

                    outputArray[k] += (Single)lambda * (condN * deltaN[k] + condS * deltaS[k]);
                }
            }
            return outputArray;
        }
    }
}
