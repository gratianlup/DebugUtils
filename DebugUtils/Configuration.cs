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
using System.Threading;
using System.Drawing;
using System.Configuration;

namespace DebugUtils.Debugger {
    /// <summary>
    /// Class used to initialize the debuger from a configuration file.
    /// </summary>
    public class DebugConfig : ConfigurationSection {
        #region Properties

        [ConfigurationProperty("Debug", DefaultValue = "true")]
        public bool Debug {
            get { return (bool)this["Debug"]; }
            set { this["Debug"] = value; }
        }


        [ConfigurationProperty("Assert", DefaultValue = "false")]
        public bool Assert {
            get { return (bool)this["Assert"]; }
            set { this["Assert"] = value; }
        }


        [ConfigurationProperty("Store", DefaultValue = "false")]
        public bool Store {
            get { return (bool)this["Store"]; }
            set { this["Store"] = value; }
        }


        [ConfigurationProperty("SaveStackInfo", DefaultValue = "true")]
        public bool SaveStackInfo {
            get { return (bool)this["SaveStackInfo"]; }
            set { this["SaveStackInfo"] = value; }
        }


        [ConfigurationProperty("LogToDebugger", DefaultValue = "false")]
        public bool LogToDebugger {
            get { return (bool)this["LogToDebugger"]; }
            set { this["LogToDebugger"] = value; }
        }

        #endregion
    }
}