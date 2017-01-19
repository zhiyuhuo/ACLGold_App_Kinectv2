//------------------------------------------------------------------------------
// <copyright file="JumpResultsCollection.cs" company="University of Missouri">
//     Copyright (c) Curators of the University of Missouri.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace ACL_Gold_x64.Models
{
    /// <summary>
    /// JumpResultsCollection is the top object in its hierarchy. It contains the list of JumpResults objects.
    /// There shouldn't be too many JumpResults objects in the Jumps list. You should only have as many objects as you do
    /// successful jumps for the subject. There is one JumpResultsCollection per test subject. This will be the object
    /// sent off to the CSV file. 
    /// </summary>
    public class JumpResultsCollection
    {

        public String TestSubjectId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public JumpResultsCollection()
        {
            Jumps = new List<JumpResults>();
        }

        /// <summary>
        /// 
        /// </summary>
        public List<JumpResults> Jumps { get; set; }
    }
}
