//------------------------------------------------------------------------------
// <copyright file="JumpResults.cs" company="University of Missouri">
//     Copyright (c) Curators of the University of Missouri.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
//using System.Drawing;
using System.Windows.Controls;
using System.Windows.Media;


namespace ACL_Gold_x64.Models
{
    /// <summary>
    /// JumpResults is the second-highest object in its hierarchy. It contains one FrameDataBuffer object, as well as 
    /// the data from the frames considered most important by the nature of our work (initial contact and peak flexion).
    /// There should be one JumpResults object generated for each jump successfully completed.
    /// </summary>
    public class JumpResults
    {
        /// <summary>
        /// Constructor method for JumpResults class.
        /// </summary>
        public JumpResults()
        {
            FrameDataList = new List<FrameData>();
        }


        /// <summary>
        /// The ID number of the subject that performed this particular jump.
        /// </summary>
        public String SubjectId { get; set; }

        /// <summary>
        /// The frame where initial contact occurs.
        /// </summary>
        public UInt32 InitFrame { get; set; }

        /// <summary>
        /// Property for knee-ankle separation ratio at the frame where initial contact occurs.
        /// </summary>
        public Single InitKasr { get; set; }

        /// <summary>
        /// Property for left knee valgus at frame where initial contact occurs.
        /// </summary>
        public Single InitLeftValg { get; set; }

        /// <summary>
        /// Property for right knee valgus at frame where initial contact occurs.
        /// </summary>
        public Single InitRightValg { get; set; }

        /// <summary>
        /// The frame where peak flexion occurs.
        /// </summary>
        public UInt32 PeakFrame { get; set; }

        /// <summary>
        /// Property for knee-ankle separation ratio at frame where peak flexion occurs.
        /// </summary>
        public Single PeakKasr { get; set; }

        /// <summary>
        /// Property for left knee valgus at frame where peak flexion occurs.
        /// </summary>
        public Single PeakLeftValg { get; set; }

        /// <summary>
        /// Property for right knee valgus at frame where peak flexion occurs.
        /// </summary>
        public Single PeakRightValg { get; set; }

        /// <summary>
        /// Property for list of data frames sent by Kinect. Stored in the database as a blob.
        /// </summary>
        public List<FrameData> FrameDataList { get; set; }

        /// <summary>
        /// The image for initial contact. To be removed before serialization.
        /// </summary>
        public ImageSource InitContactImage { get; set; }

        /// <summary>
        /// The image for peak flexion. To be removed before serialization.
        /// </summary>
        public ImageSource PeakFlexionImage { get; set; }

    }
}
