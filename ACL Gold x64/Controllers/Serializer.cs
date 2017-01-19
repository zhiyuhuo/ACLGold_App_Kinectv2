//------------------------------------------------------------------------------
// <copyright file="JumpResults.cs" company="University of Missouri">
//     Copyright (c) Curators of the University of Missouri.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Web;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Converters;
using ACL_Gold_x64.Models;
using System.Windows.Forms;
using System.IO;
using System.Windows.Media.Imaging;
//using System.Threading;

namespace ACL_Gold_x64.Controllers
{
    public class Serializer
    {
        private readonly JumpResultsCollection _jumpCollection;
        private readonly String _completeFilePath;
        private readonly String _logFilePath;

        public bool IsFinished { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="toSerialize"></param>
        public Serializer(JumpResultsCollection toSerialize)
        {

            _jumpCollection = toSerialize;
            var fileName = Convert.ToString( toSerialize.TestSubjectId);

            string path = @"C:\Users\Public\TestFolder";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            _completeFilePath = @"C:\Users\Public\TestFolder\" + fileName + ".json";
            _logFilePath = @"C:\Users\Public\TestFolder\log" + fileName + ".txt";
        }

        /// <summary>
        /// 
        /// </summary>
        public void Serialize()
        {
            const String message = "BOOM! Serialized.\n";
            var start = new DateTime();
            start = DateTime.UtcNow;
            ITraceWriter traceWriter = new MemoryTraceWriter();
            
            var jsonString = JsonConvert.SerializeObject(_jumpCollection, 
                Formatting.Indented, 
                new JsonSerializerSettings { TraceWriter = traceWriter, Converters = { new JavaScriptDateTimeConverter() } });
            
            File.WriteAllText(_completeFilePath, jsonString);
            //Console.WriteLine(traceWriter);
            File.WriteAllText(_logFilePath, traceWriter.ToString());
            System.Media.SystemSounds.Beep.Play();
            var end = new DateTime();
            end = DateTime.UtcNow;
            var timePassed = new TimeSpan();
            timePassed = end.Subtract(start);
            MessageBox.Show(message + Convert.ToString(timePassed));
            IsFinished = true;
        }
    }
}
