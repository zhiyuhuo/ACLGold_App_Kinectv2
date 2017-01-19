//------------------------------------------------------------------------------
// <copyright file="FinalizeTests.xaml.cs" company="University of Missouri">
//     Copyright (c) Curators of the University of Missouri.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

using System.Windows;


using ACL_Gold_x64.Models;
using ACL_Gold_x64.Controllers;
//using System.Threading;

namespace ACL_Gold_x64
{
    /// <summary>
    /// Interaction logic for FinalizeTests.xaml
    /// </summary>
    public partial class FinalizeTests
    {
        private Int32 _positionInCollection;
        private readonly SensorManager _manager;
        private JumpResults _results;
        private readonly List<Single> _toConvert;
        private readonly List<String> _toDisplay;
        private readonly JumpResultsCollection _toSerializer;
        private readonly AclTestingSuite _suite;

        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sensorManager"></param>
        /// <param name="testSuite"></param>
        public FinalizeTests(ref SensorManager sensorManager, AclTestingSuite testSuite)
        {
            this.WindowState = WindowState.Maximized;
            InitializeComponent();
            //Instantiate object variables needed 
            _results = new JumpResults();
            _toConvert = new List<Single>();
            _toDisplay = new List<String>();
            _toSerializer = new JumpResultsCollection();
            //Set object variables to values
            _manager = sensorManager;    //set up manager so we can use the sensorManager object's info in this class
            _suite = testSuite;
            _results = _manager.TestSubjectJumps.Jumps[_positionInCollection];    //start at the first jump
            //Add objects to the toConvert list by calling AddToList
            AddToList();
            //Convert the toConvert list to toDisplay with ConvertData
            ConvertData();
            //Change display data with DisplayData.
            DisplayData();
            //clear out toDisplay and toConvert
            _toDisplay.Clear();
            _toConvert.Clear();
            InitContactImage.Source = _results.InitContactImage;
            PeakFlexionImage.Source = _results.PeakFlexionImage;
            //Change the counter on top of window.
            TxtJumpTestCount.Text = String.Format("Jump test {0}", _positionInCollection + 1);

            //Import the object for the jumpResultsCollection. If the user saves the object to a serialized .json file, this will
            //be what is sent. 
            _toSerializer = _manager.TestSubjectJumps;
            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            _positionInCollection = Math.Min(_positionInCollection + 1, _manager.TestSubjectJumps.Jumps.Count() - 1);
                
            _results = _manager.TestSubjectJumps.Jumps[_positionInCollection];
            AddToList();
            ConvertData();
            DisplayData();
            _toDisplay.Clear();
            _toConvert.Clear();
            BtnPrevious.IsEnabled = true;
            InitContactImage.Source = _results.InitContactImage;
            PeakFlexionImage.Source = _results.PeakFlexionImage;
            TxtJumpTestCount.Text = String.Format("Jump test {0}", _positionInCollection + 1);
            if(_positionInCollection == _manager.TestSubjectJumps.Jumps.Count() -1)
            {
                BtnNext.IsEnabled = false;
            }
            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            _positionInCollection = Math.Max(_positionInCollection - 1, 0);
            _results = _manager.TestSubjectJumps.Jumps[_positionInCollection];
            AddToList();
            ConvertData();
            DisplayData();
            _toDisplay.Clear();
            _toConvert.Clear();
            BtnNext.IsEnabled = true;
            InitContactImage.Source = _results.InitContactImage;
            PeakFlexionImage.Source = _results.PeakFlexionImage;
            TxtJumpTestCount.Text = String.Format("Jump test {0}", _positionInCollection + 1);
            if(_positionInCollection == 0)
            {
                BtnPrevious.IsEnabled = false;
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {          
            if(String.IsNullOrEmpty(TxtSubjectId.Text))
            {
                MessageBox.Show("A text subject ID number must be entered.", "PROBLEM", MessageBoxButton.OK);
            }
            else
            {
                _toSerializer.TestSubjectId = TxtSubjectId.Text;
                var jumpCount = _toSerializer.Jumps.Count;
                for (var count = 0; count < jumpCount; count++)
                {
                    _toSerializer.Jumps[count].SubjectId = TxtSubjectId.Text;
                }
                var serializer = new Serializer(_toSerializer);
                serializer.Serialize();
                _suite.TxtSuccessfulJumps.Text = "0";
                _manager.SuccessfulJumps = 0;
                _manager.TestSubjectJumps = new JumpResultsCollection();
                Close();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();

        }

        /// <summary>
        /// 
        /// </summary>
        private void AddToList()
        {
            _toConvert.Clear();
            _toConvert.Add(_results.InitKasr);
            _toConvert.Add(_results.InitLeftValg);
            _toConvert.Add(_results.InitRightValg);
            _toConvert.Add(_results.PeakKasr);
            _toConvert.Add(_results.PeakLeftValg);
            _toConvert.Add(_results.PeakRightValg);
        }
                        
        /// <summary>
        /// Takes in a collection of the single values used in the JumpResults object, converts them to strings.
        /// </summary>
        /// <remarks>1: InitKasr 2: InitLeftValg 3: InitRightValg 4: PeakKasr 5: PeakLeftValg 6: PeakRightValg </remarks>
        private void ConvertData()
        {           
            var listSize = _toConvert.Count;
            int count;

            // we flip the sign to be medically correct. Valgus is when the knees are bent inward and should be positive.
            // Varus is when the knees are bent outward. Negative valgus is seen as varus.
            _toConvert[1] = _toConvert[1]*-1;
            _toConvert[2] = _toConvert[2]*-1;
            _toConvert[4] = _toConvert[4]*-1;
            _toConvert[5] = _toConvert[5]*-1;

            //now, we truncate the values to 1 decimal place for the angles, 2 for the ratio
            _toConvert[0] = Convert.ToSingle((Math.Truncate(_toConvert[0]*100)/100)); //to 2
            _toConvert[3] = Convert.ToSingle((Math.Truncate(_toConvert[3]*100)/100));

            _toConvert[1] = Convert.ToSingle((Math.Truncate(_toConvert[1]*10)/10));
            _toConvert[2] = Convert.ToSingle((Math.Truncate(_toConvert[2]*10)/10));
            _toConvert[4] = Convert.ToSingle((Math.Truncate(_toConvert[4]*10)/10));
            _toConvert[5] = Convert.ToSingle((Math.Truncate(_toConvert[5]*10)/10));

            for (count = 0; count < listSize; count++ )
            {
                _toDisplay.Add(Convert.ToString(_toConvert[count]));
            }
        }

        /// <summary>
        /// Sets contents of the various fields in the interface.
        /// </summary>
        private void DisplayData()
        {
            TxtInitKasr.Text = _toDisplay[0];
            TxtInitLeftValg.Text = _toDisplay[1];
            TxtInitRightValg.Text = _toDisplay[2];
            TxtPeakKasr.Text = _toDisplay[3];
            TxtPeakLeftValg.Text = _toDisplay[4];
            TxtPeakRightValg.Text = _toDisplay[5];
            
        }

        private void BtnDeleteJump_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("This will delete jump " + (_positionInCollection + 1) + ". Continue?", "Are you sure?",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            switch (result)
            {
                case MessageBoxResult.Yes:
                    _toSerializer.Jumps.RemoveAt(_positionInCollection);

                    if (_toSerializer.Jumps.Count == 0)
                    {
                        MessageBox.Show("There are no remaining jumps.");
                         _suite.TxtSuccessfulJumps.Text = _manager.TestSubjectJumps.Jumps.Count.ToString();
                        Close();

                    }
                    else
                    {
                        _positionInCollection = Math.Max(_positionInCollection - 1, 0);
                        _results = _manager.TestSubjectJumps.Jumps[_positionInCollection];
                        AddToList();
                        ConvertData();
                        DisplayData();
                        _toDisplay.Clear();
                        _toConvert.Clear();


                        TxtJumpTestCount.Text = String.Format("Jump test {0}", _positionInCollection + 1);

                        BtnNext.IsEnabled = true;
                        BtnPrevious.IsEnabled = true;
                        if (_positionInCollection == 0)
                        {
                            BtnPrevious.IsEnabled = false;
                        }
                        if (_positionInCollection == _manager.TestSubjectJumps.Jumps.Count() - 1)
                        {
                            BtnNext.IsEnabled = false;
                        }
                        _suite.TxtSuccessfulJumps.Text = _manager.TestSubjectJumps.Jumps.Count.ToString();
                        
                    }
                    break;

            }
        }
    }
}
