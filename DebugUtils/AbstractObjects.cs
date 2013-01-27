// Copyright (c) 2006 Gratian Lup. All rights reserved.
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are
// met:
//
// * Redistributions of source code must retain the above copyright
// notice, this list of conditions and the following disclaimer.
// 
// * Redistributions in binary form must reproduce the above
// copyright notice, this list of conditions and the following
// disclaimer in the documentation and/or other materials provided
// with the distribution.
//
// * The name "DebugUtils" must not be used to endorse or promote 
// products derived from this software without prior written permission.
//
// * Products derived from this software may not be called "DebugUtils" nor 
// may "DebugUtils" appear in their names without prior written 
// permission of the author.
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
using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using System.Xml.Serialization;
using System.Reflection;

namespace DebugUtils.Debugger {
    #region Abstract objects

    /// <summary>
    /// It is recommended to derive all debug listeners from this abstract class.
    /// </summary>
    public abstract class DebugListenerBase : IDebugListener {
        #region Fields

        #endregion

        #region Abstract methods

        public abstract bool Open();
        public abstract bool Close();
        public abstract bool DumpMessage(DebugMessage message);

        #endregion

        #region Methods

        public string GetFileName(string path) {
            if(path == null) {
                throw new ArgumentNullException("path");
            }

            DirectoryInfo di = new DirectoryInfo(path);
            return di.Name;
        }


        /// <summary>
        /// Extracts the method name from the method definition.
        /// </summary>
        /// <param name="method">The method definition.</param>
        /// <returns>The method name.</returns>
        /// <example>System.Int32 SomeMethod(System.String)  ->  SomeMethod</example>
        public string ExtractSimplifiedMethod(string method) {
            if(method == null) {
                return String.Empty;
            }

            for(int i = 0; i < method.Length; i++) {
                if(method[i] == ' ') {
                    for(int j = i + 1; j < method.Length; j++) {
                        if(method[j] == '(') {
                            return method.Substring(i + 1, j - i - 1);
                        }
                    }
                }
            }

            return null;
        }


        /// <summary>
        /// Gets the type of the method as a string.
        /// </summary>
        /// <param name="type">The method type.</param>
        /// <returns>The type string.</returns>
        /// <example>public, private, abstract</example>
        public string GetMethodTypeString(MethodType type) {
            switch(type) {
                case MethodType.Abstract: {
                        return "ABSTRACT";
                    }
                case MethodType.Private: {
                        return "PRIVATE";
                    }
                case MethodType.Public: {
                        return "PUBLIC";
                    }
                case MethodType.Static: {
                        return "STATIC";
                    }
                case MethodType.Virtual: {
                        return "VIRTUAL";
                    }
            }

            return string.Empty;
        }

        #endregion

        #region Properties

        private int _handledMessages;
        /// <summary>
        /// Gets or sets the number of handled messages.
        /// </summary>
        public int HandledMessages {
            get { return _handledMessages; }
            set { _handledMessages = value; }
        }

        private bool _useStackInfo;
        /// <summary>
        /// Specifies whether or not to use the stack information from the messages.
        /// </summary>
        public bool UseStackInfo {
            get { return _useStackInfo; }
            set { _useStackInfo = value; }
        }

        private bool _truncateFile;
        /// <summary>
        /// Specifies whether or not to truncate the source file path to the file name.
        /// </summary>
        public bool TruncateFile {
            get { return _truncateFile; }
            set { _truncateFile = value; }
        }

        private bool _simplifyMethod;
        /// <summary>
        /// Specifies whether or not to extract the name of the method.
        /// </summary>
        public bool SimplifyMethod {
            get { return _simplifyMethod; }
            set { _simplifyMethod = value; }
        }

        #region Interface properties

        private int _listnerId;
        public int ListnerId {
            get { return _listnerId; }
            set { _listnerId = value; }
        }

        private bool _isOpen;
        public bool IsOpen {
            get { return _isOpen; }
            set { _isOpen = value; }
        }

        // let the derived type handle the Enabled property
        public abstract bool Enabled { get; set; }

        #endregion

        #endregion
    }


    /// <summary>
    /// It is recommended to derive all debug listeners from this abstract class.
    /// </summary>
    public abstract class DebugMessageFilterBase : IDebugMessageFilter {
        #region Properties

        private int _filterId;
        public int FilterId {
            get { return _filterId; }
            set { _filterId = value; }
        }

        private bool _enabled;
        public bool Enabled {
            get { return _enabled; }
            set { _enabled = value; }
        }

        private MessageFilterImplication _implication;
        public MessageFilterImplication Implication {
            get { return _implication; }
            set { _implication = value; }
        }

        #endregion

        #region Abstract methods

        public abstract bool AllowMessage(DebugMessage message);

        #endregion
    }


    /// <summary>
    /// It is recommended to derive all debug message notifiers from this abstract class.
    /// </summary>
    public abstract class DebugMessageNotifierBase : IDebugMessageNotifier {
        #region Properties

        private bool _enabled;
        public bool Enabled {
            get { return _enabled; }
            set { _enabled = value; }
        }

        private DebugMessage _message;
        public DebugMessage Message {
            get { return _message; }
            set { _message = value; }
        }

        #endregion

        #region Abstract methods

        public abstract bool Launch();

        #endregion
    }


    /// <summary>
    /// It is recommended to derive all crash notifiers from this abstract class.
    /// </summary>
    public abstract class CrashNotifierBase : ICrashNotifier {
        #region Properties

        private string _dumpFilePath;
        public string DumpFilePath {
            get { return _dumpFilePath; }
            set { _dumpFilePath = value; }
        }

        private string _debugMessagesFilePath;
        public string DebugMessagesFilePath {
            get { return _debugMessagesFilePath; }
            set { _debugMessagesFilePath = value; }
        }

        private string _crashDetails;
        public string CrashDetails {
            get { return _crashDetails; }
            set { _crashDetails = value; }
        }

        private Exception _unhandledException;
        public Exception UnhandledException {
            get { return _unhandledException; }
            set { _unhandledException = value; }
        }

        #endregion

        #region Abstract methods

        public abstract bool Launch();

        #endregion
    }

    /// <summary>
    /// It is recommended to derive all object counter notifiers from this abstract class.
    /// </summary>
    public abstract class ObjectCounterNotifierBase : IObjectCounterNotifier {
        #region Properties

        private bool _enabled;
        public bool Enabled {
            get { return _enabled; }
            set { _enabled = value; }
        }

        private Type _counterType;
        public Type CounterType {
            get { return _counterType; }
            set { _counterType = value; }
        }

        private string _counterName;
        public string CounterName {
            get { return _counterName; }
            set { _counterName = value; }
        }

        private int _counterCount;
        public int CounterCount {
            get { return _counterCount; }
            set { _counterCount = value; }
        }

        private int _counterMaximumCount;
        public int CounterMaximumCount {
            get { return _counterMaximumCount; }
            set { _counterMaximumCount = value; }
        }

        private bool? _counterEnabled;
        public bool? CounterEnabled {
            get { return _counterEnabled; }
            set { _counterEnabled = value; }
        }

        private ObjectCounter _counter;
        public ObjectCounter Counter {
            get { return _counter; }
            set { _counter = value; }
        }

        #endregion

        #region Abstract methods

        public abstract bool Launch();

        #endregion
    }

    #endregion
}
