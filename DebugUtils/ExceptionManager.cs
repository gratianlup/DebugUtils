// Copyright (c) Gratian Lup. All rights reserved.
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are
// met:
//
//     * Redistributions of source code must retain the above copyright
//       notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above
//       copyright notice, this list of conditions and the following
//       disclaimer in the documentation and/or other materials provided
//       with the distribution.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
// OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
// SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
// LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
// THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace DebugUtils.Debugger {
    /// <summary>
    /// Interface that needs to be used by all CrashNotifier objects.
    /// </summary>
    public interface ICrashNotifier {
        bool Launch();
        string DumpFilePath { get; set; }
        string DebugMessagesFilePath { get; set; }
        string CrashDetails { get; set; }
        Exception UnhandledException { get; set; }
    }


    /// <summary>
    /// Used to pass custom data to the exception manager when when writing the dump report.
    /// </summary>
    public class CustomDataEventArgs : EventArgs {
        public string data;
    }

    /// <summary>
    /// Delegate used to handle the OnWriteDump event.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void CustomDataRetrievalDelegate(object sender, CustomDataEventArgs e);


    /// <summary>
    /// Provides functionality for managing unhandled exceptions.
    /// </summary>
    public class ExceptionManager {
        #region Constants

        private const string DefaultDumpHeader = "Application crash report";
        private const string DefaultCrashReportMessage = "Application crash";

        #endregion

        #region Fields

        private AppDomain activeDomain;
        private bool enabled;

        /// <summary>
        /// The crash notifier to be launched when an unhandled exception occurs.
        /// </summary>
        public ICrashNotifier CrashNotifier;
        private string crashDetails;

        #endregion

        #region Properties

        private bool _dump;
        /// <summary>
        /// Indicates whether or not to create a crash dump report.
        /// </summary>
        public bool Dump {
            get { return _dump; }
            set { _dump = value; }
        }


        private string _dumpPath;
        /// <summary>
        /// Indicates where to save the crash dump report.
        /// </summary>
        public string DumpPath {
            get { return _dumpPath; }
            set { _dumpPath = value; }
        }


        private string _dumpHeader;
        /// <summary>
        /// Indicates a description header of the crash dump report.
        /// </summary>
        public string DumpHeader {
            get { return _dumpHeader; }
            set { _dumpHeader = value; }
        }

        private bool _reportCrash;
        /// <summary>
        /// Indicates whether or not to report the application crash to the debugger.
        /// </summary>
        public bool ReportCrash {
            get { return _reportCrash; }
            set { _reportCrash = value; }
        }

        private string _crashReportMessage;
        /// <summary>
        /// The message to be sent to the debugger when reporting the application crash.
        /// </summary>
        public string CrashReportMessage {
            get { return _crashReportMessage; }
            set { _crashReportMessage = value; }
        }

        private bool _saveDebugMessages;
        /// <summary>
        /// Indicates whether or not to save the debug messages when the application crashes.
        /// </summary>
        public bool SaveDebugMessages {
            get { return _saveDebugMessages; }
            set { _saveDebugMessages = value; }
        }

        private string _debugMessagesPath;
        /// <summary>
        /// The path where to save the debug messages.
        /// </summary>
        public string DebugMessagesPath {
            get { return _debugMessagesPath; }
            set { _debugMessagesPath = value; }
        }

        private bool _saveMessagesCompressed;
        /// <summary>
        /// Indicates whether or not to save the debug messages in compressed format (ZIP).
        /// </summary>
        public bool SaveMessagesCompressed {
            get { return _saveMessagesCompressed; }
            set { _saveMessagesCompressed = value; }
        }

        private bool _appendWinSATScores;
        /// <summary>
        /// Indicates whether or not to append the WinSAT (Windows Experience Index) scores to the dump report.
        /// </summary>
        public bool AppendWinSATScores {
            get { return _appendWinSATScores; }
            set { _appendWinSATScores = value; }
        }

        #endregion

        #region Events

        /// <summary>
        /// Event raised when an unhandled exception occurs.
        /// </summary>
        public event UnhandledExceptionEventHandler OnUnhandledException;


        /// <summary>
        /// Event raised when the dump report is being written.
        /// It can be used to write custom data to the report.
        /// </summary>
        public event CustomDataRetrievalDelegate OnWriteDump;

        #endregion

        #region Private methods

        private void WriteDump(Exception e) {
            string path = _dumpPath;

            if(path == null) {
                // save the dump in the temp directory
                path = Environment.GetEnvironmentVariable("TEMP");
                path += "\\" + Assembly.GetExecutingAssembly().GetName().Name + ".txt";
            }

            StringBuilder builder = new StringBuilder();
            builder.Append(AppDomain.CurrentDomain.FriendlyName);
            builder.AppendLine();

            // append dump File header
            if(_dumpHeader != null) {
                builder.Append(_dumpHeader);
            }
            else {
                builder.Append(DefaultDumpHeader);
            }

            builder.AppendLine();
            builder.AppendLine();

            // append date and time
            builder.Append("Date: ");
            builder.Append(DateTime.Now.ToLongDateString());
            builder.Append(", ");
            builder.Append(DateTime.Now.ToLongTimeString());
            builder.AppendLine();
            builder.AppendLine();

            // application info
            builder.Append("Application version: " + Assembly.GetExecutingAssembly().GetName().Version.ToString());
            builder.AppendLine();
            builder.Append("Application path: " + Environment.CommandLine);
            builder.AppendLine();
            builder.Append("Application working set: " + Environment.WorkingSet.ToString());
            builder.AppendLine();
            builder.AppendLine();

            // host info
            builder.Append("Operating system version: " + Environment.OSVersion.ToString());
            builder.AppendLine();
            builder.Append("CLR version: " + Environment.Version.ToString());

            if(_appendWinSATScores && WinSAT.IsAvailable) {
                WinSAT winSAT = new WinSAT();

                if(winSAT.LoadWinSAT()) {
                    builder.AppendLine();
                    builder.AppendLine();
                    builder.Append("WinSAT scores");
                    builder.AppendLine();
                    builder.Append("System: " + winSAT.ComputerScore.ToString());
                    builder.AppendLine();
                    builder.Append("CPU: " + winSAT.CpuScore.ToString());
                    builder.AppendLine();
                    builder.Append("Memory: " + winSAT.MemoryScore.ToString());
                    builder.AppendLine();
                    builder.Append("Graphics: " + winSAT.GraphicsScore.ToString());
                    builder.AppendLine();
                    builder.Append("Games graphics: " + winSAT.GamingGraphicsScore.ToString());
                    builder.AppendLine();
                    builder.Append("Disk: " + winSAT.DiskScore.ToString());
                }
            }

            builder.AppendLine();
            builder.AppendLine();

            // append exception info
            if(e != null) {
                builder.Append("Exception: " + e.Message);
                builder.AppendLine();
                builder.Append("TargerSite: " + e.TargetSite.Name);

                // append inner exceptions
                Exception inner = e.InnerException;
                int innerCount = 1;

                while(inner != null) {
                    builder.Append('\t', innerCount);
                    builder.Append("Exception: " + inner.Message);
                    builder.AppendLine();
                    builder.Append("TargerSite: " + inner.TargetSite.Name);

                    innerCount++;
                }
            }

            crashDetails = builder.ToString();
            builder.AppendLine();
            builder.Append("Stack trace:\r\n" + e.StackTrace);

            if(OnWriteDump != null) {
                CustomDataEventArgs args = new CustomDataEventArgs();

                OnWriteDump(this, args);

                // append data
                if(args.data != null && args.data.Length > 0) {
                    builder.AppendLine();
                    builder.AppendLine();
                    builder.Append(args.data);
                }
            }

            // loaded assembly info

            builder.Append("\r\n\r\nLoaded assemblies:\r\n\r\n");
            activeDomain = AppDomain.CurrentDomain;
            
            foreach(Assembly a in activeDomain.GetAssemblies()) {
                builder.Append("Name: " + a.GetName().Name);
                builder.AppendLine();

                try {
                    if(a.Location != null && a.Location.Length != 0 && File.Exists(a.Location)) {
                        builder.Append(FileVersionInfo.GetVersionInfo(a.Location).ToString());
                        builder.AppendLine();
                    }
                }
                catch(Exception ne) {
                    Console.WriteLine(ne.Message);
                }
            }

            // write to File
            StreamWriter writer = new StreamWriter(path);
            _dumpPath = path;

            if(writer.BaseStream.CanWrite) {
                writer.Write(builder.ToString());
                writer.Close();
            }
        }


        /// <summary>
        /// Save the debug messages in XML format
        /// </summary>
        private void SerializeDebugMessages() {
            string path = _debugMessagesPath;

            if(path == null) {
                // save the dump in the temp directory
                path = Environment.GetEnvironmentVariable("TEMP");

                // append assembly name
                path += "\\" + Assembly.GetExecutingAssembly().GetName().Name + ".log";
            }

            if(_saveMessagesCompressed) {
                Debug.SerializeMessagesCompressed(path);
            }
            else {
                Debug.SerializeMessages(path);
            }

            _debugMessagesPath = path;
        }


        private void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs args) {
            try {
                // notify about the exception
                if(OnUnhandledException != null) {
                    OnUnhandledException(this, args);
                }

                if(_dump) {
                    WriteDump(args.ExceptionObject as Exception);
                }

                // report the crash to the debugger
                if(_reportCrash) {
                    if(_crashReportMessage != null) {
                        Debug.ReportError(_crashReportMessage);
                    }
                    else {
                        Debug.ReportError(DefaultCrashReportMessage);
                    }
                }

                // save the debug messages
                if(_saveDebugMessages) {
                    SerializeDebugMessages();
                }

                // show crash notifier
                if(CrashNotifier != null) {
                    CrashNotifier.DumpFilePath = _dumpPath;
                    CrashNotifier.DebugMessagesFilePath = _saveDebugMessages == false ? null : _debugMessagesPath;
                    CrashNotifier.CrashDetails = crashDetails;
                    CrashNotifier.UnhandledException = (Exception)args.ExceptionObject;

                    if(CrashNotifier.Launch() == false) {
                        Console.WriteLine("Couldn't launch ICrashNotifier {0}", CrashNotifier.GetType().Name);
                    }
                }
            }
            catch { }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Attach the exception manager to the UnhandledException event of the given AppDomain.
        /// </summary>
        /// <param name="domain">The AppDomain to which to attach the exception manager.</param>
        public void AttachExceptionManager(AppDomain domain) {
            if(domain == null) {
                return;
            }

            // disable watching the previous domain
            DetachExceptionManager();

            // attach the handler to the event
            domain.UnhandledException += UnhandledExceptionHandler;
            enabled = true;
        }


        /// <summary>
        /// Attach the exception manager to the UnhandledException event of the default AppDomain.
        /// </summary>
        public void AttachExceptionManager() {
            AttachExceptionManager(AppDomain.CurrentDomain);
        }


        /// <summary>
        /// Detach the exception manager from the attached AppDomain.
        /// </summary>
        public void DetachExceptionManager() {
            if(enabled && activeDomain != null) {

                activeDomain.UnhandledException -= UnhandledExceptionHandler;
            }
        }

        #endregion
    }
}
