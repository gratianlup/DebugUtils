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
using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using System.Xml.Serialization;
using System.Reflection;

namespace DebugUtils.Debugger {
    #region Debug attribute

    /// <summary>
    /// Allows setting custom debug rules on objects and methods.
    /// </summary>
    /// <example>[DebugOptions(Debug = true, Assert = true)]</example>
    [AttributeUsage(AttributeTargets.All)]
    public class DebugOptions : Attribute {
        internal bool isDebugSet;
        private bool _debug;
        /// <summary>
        /// Specifies whether or not to handle the debugger calls.
        /// </summary>
        public bool Debug {
            get { return _debug; }
            set { _debug = value; isDebugSet = true; }
        }


        internal bool isAssertSet;
        private bool _assert;
        /// <summary>
        /// Specifies whether or not to throw exceptions on failed assertions.
        /// </summary>
        public bool Assert {
            get { return _assert; }
            set { _assert = value; isAssertSet = true; }
        }


        internal bool isStoreSet;
        private bool _store;
        /// <summary>
        /// Specifies whether or not to store generated messages.
        /// </summary>
        public bool Store {
            get { return _store; }
            set { _store = value; isStoreSet = true; }
        }


        internal bool isSaveStackSet;
        private bool _saveStack;
        /// <summary>
        /// Specifies whether or not to save stack information for the messages.
        /// </summary>
        public bool SaveStack {
            get { return _saveStack; }
            set { _saveStack = value; isSaveStackSet = true; }
        }


        internal bool isLogToDebuggerSet;
        private bool logToDebugger;
        /// <summary>
        /// Specifies whether or not to send the messages to the debugger.
        /// </summary>
        public bool LogToDebugger {
            get { return logToDebugger; }
            set { logToDebugger = value; isLogToDebuggerSet = true; }
        }


        internal bool isSendToListnersSet;
        private bool sendToListners;
        /// <summary>
        /// Specifies whether or not to send the messages to the listeners.
        /// </summary>
        public bool SendToListners {
            get { return sendToListners; }
            set { sendToListners = value; isSendToListnersSet = true; }
        }


        internal bool isSendToNotifierSet;
        private bool sendToNotifier;
        /// <summary>
        /// Specifies whether or not to send the messages to the notifier.
        /// </summary>
        public bool SendToNotifier {
            get { return sendToNotifier; }
            set { sendToNotifier = value; isSendToNotifierSet = true; }
        }
    }

    #endregion
}