// Copyright (c) Gratian Lup. All rights reserved.
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
using System.Diagnostics;
using System.Xml.Serialization;
using System.Reflection;

namespace DebugUtils.Debugger.Filters {
    /// <summary>
    /// Filters messages of type error.
    /// </summary>
    public class ErrorMessageFilter : DebugMessageFilterBase {
        #region Contructor

        public ErrorMessageFilter(int id) {
            FilterId = id;
            Enabled = true;
            Implication = MessageFilterImplication.AND;
        }

        #endregion

        #region Public methods

        public override bool AllowMessage(DebugMessage message) {
            if(message == null) {
                return true;
            }

            if(Enabled && message.Type == DebugMessageType.Error) {
                return true;
            }

            return false;
        }

        #endregion
    }


    /// <summary>
    /// Filters messages of type warning.
    /// </summary>
    public class WarningMessageFilter : DebugMessageFilterBase {
        #region Constructor

        public WarningMessageFilter(int id) {
            FilterId = id;
            Enabled = true;
            Implication = MessageFilterImplication.AND;
        }

        #endregion

        #region Public methods

        public override bool AllowMessage(DebugMessage message) {
            if(message == null) {
                return true;
            }

            if(Enabled && message.Type == DebugMessageType.Warning) {
                return true;
            }

            return false;
        }

        #endregion
    }


    /// <summary>
    /// Filters messages of unknown type.
    /// </summary>
    public class UnknownMessageFilter : DebugMessageFilterBase {
        #region Constructor

        public UnknownMessageFilter(int id) {
            FilterId = id;
            Enabled = true;
            Implication = MessageFilterImplication.AND;
        }

        #endregion

        #region Public methods

        public override bool AllowMessage(DebugMessage message) {
            if(message == null) {
                return true;
            }

            if(Enabled && message.Type == DebugMessageType.Unknown) {
                return true;
            }

            return false;
        }

        #endregion
    }


    /// <summary>
    /// Filters messages by namespace.
    /// </summary>
    public class NamespaceMessageFilter : DebugMessageFilterBase {
        #region Constructor

        public NamespaceMessageFilter(int id) {
            FilterId = id;
            Enabled = true;
            Implication = MessageFilterImplication.AND;
            Namespace = string.Empty;
        }

        public NamespaceMessageFilter(int id, string filterNamespace) {
            if(filterNamespace == null) {
                throw new ArgumentNullException("filterNamespace");
            }

            FilterId = id;
            Enabled = true;
            Implication = MessageFilterImplication.AND;
            Namespace = filterNamespace;
        }

        #endregion

        #region Properties

        private string _namespace;
        public string Namespace {
            get { return _namespace; }
            set { _namespace = value; }
        }

        #endregion

        #region Public methods

        public override bool AllowMessage(DebugMessage message) {
            if(message == null) {
                return true;
            }

            if(Enabled && message.BaseMethod.DeclaringNamespace == _namespace) {
                return true;
            }

            return false;
        }

        #endregion
    }


    /// <summary>
    /// Filters messages by object.
    /// </summary>
    public class ObjectMessageFilter : DebugMessageFilterBase {
        #region Constructor

        public ObjectMessageFilter(int id) {
            FilterId = id;
            Enabled = true;
            Object = string.Empty;
            Implication = MessageFilterImplication.AND;
        }

        public ObjectMessageFilter(int id, string filterObject) {
            if(filterObject == null) {
                throw new ArgumentNullException("filterObject");
            }

            FilterId = id;
            Enabled = true;
            Implication = MessageFilterImplication.AND;
            Object = filterObject;
        }

        #endregion

        #region Properties

        private string _object;
        public string Object {
            get { return _object; }
            set { _object = value; }
        }

        #endregion

        #region Public methods

        public override bool AllowMessage(DebugMessage message) {
            if(message == null) {
                return true;
            }

            if(Enabled && message.BaseMethod.DeclaringObject == _object) {
                return true;
            }
            
            return false;
        }

        #endregion
    }


    /// <summary>
    /// Filters messages by Method.
    /// </summary>
    public class MethodMessageFilter : DebugMessageFilterBase {
        #region Constructor

        public MethodMessageFilter(int id) {
            FilterId = id;
            Enabled = true;
            Implication = MessageFilterImplication.AND;
            Method = string.Empty;
        }

        public MethodMessageFilter(int id, string filterMethod) {
            if(filterMethod == null) {
                throw new ArgumentNullException("filterMethod");
            }

            FilterId = id;
            Enabled = true;
            Implication = MessageFilterImplication.AND;
            Method = filterMethod;
        }

        #endregion

        #region Properties

        private string _method;
        public string Method {
            get { return _method; }
            set { _method = value; }
        }

        #endregion

        #region Public methods

        public override bool AllowMessage(DebugMessage message) {
            if(message == null) {
                return true;
            }

            if(Enabled && message.BaseMethod.DeclaringObject == _method) {
                return true;
            }

            return false;
        }

        #endregion
    }
}
