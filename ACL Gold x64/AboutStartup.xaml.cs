//------------------------------------------------------------------------------
// <copyright file="AboutStartup.xaml.cs" company="University of Missouri">
//     Copyright (c) Curators of the University of Missouri.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System.Windows;


namespace ACL_Gold_x64
{
    /// <summary>
    /// Interaction logic for AboutStartup.xaml
    /// </summary>
    public partial class AboutStartup
    {
        /// <summary>
        /// 
        /// </summary>
        public AboutStartup()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnAboutExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
