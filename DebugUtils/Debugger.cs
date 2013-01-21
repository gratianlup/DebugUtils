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
    /// Exception thrown when an assertion fails.
    /// </summary>
    public class DebugException : Exception {
        public DebugException() { }

        public DebugException(string message)
            : base(message) {
        }
    }


    /// <summary>
    /// The primary class that handless debugging.
    /// Most of the other classes are exposed as static members withing this class.
    /// All methods and fields are defined as "static".
    /// Provides support for serialization of the stored messages in XML and ZIP 
    /// compressed XML format and generating of HTML reports.
    /// </summary>
    public class Debug {
        /// <summary>
        /// Used for retrieving the setting that apply in the given context.
        /// Custom settings can be specified on a object/Method basis using the DebugOptions attribute.
        /// </summary>
        internal struct DebugSettings {
            public bool Debug;
            public bool Assert;
            public bool Store;
            public bool SaveStack;
            public bool LogToDebugger;
            public bool SendToListners;
            public bool SendToNotifier;
        }

        #region Constants

        private const int DefaultMessageStoryCapacity = 1024;
        private const string DefaultAssertMesssage = "Assertion failed";
        private const string DefaultAssertNotNullMesssage = "Object is null";
        private const string DefaultAssertTypeMesssage = "Object type assertion failed";

        #endregion

        #region Fields

        private static List<DebugMessage> messages;
        private static List<IDebugListener> listeners;
        private static List<IDebugMessageFilter> filters;
        private static Stack<DebugContext> contextStack;
        private static bool _storeMessages;
        private static int _messageStoreCapacity = DefaultMessageStoryCapacity;
        private static bool _breakOnFailedAssertion;
        private static bool _logToDebugger;
        private static bool _saveStackInfo;
        private static bool _debuggerEnabled;
        private static string _debugMessageViewerPath;
        private static string _debugMessageViewerEnvironmentVariable;
        private static Color _color;

        /// <summary>
        /// The message notifier used by the debugger.
        /// </summary>
        public static IDebugMessageNotifier DebugMessageNotifier;

        // used to provide synchronization when using the library from multiple threads
        private static object lockObject = new object();

        #endregion

        #region Properties

        /// <summary>
        /// Returns the number of attached listeners.
        /// </summary>
        public static int ListenerCount {
            get {
                if(listeners == null) {
                    return 0;
                }
                else {
                    return listeners.Count;
                }
            }
        }

        /// <summary>
        /// Returns the number of attached filters.
        /// </summary>
        public static int FilterCount {
            get {
                if(filters == null) {
                    return 0;
                }
                else {
                    return filters.Count;
                }
            }
        }

        /// <summary>
        /// Indicates whether or not the messages should be stored.
        /// </summary>
        public static bool StoreMessages {
            get {
                return _storeMessages;
            }
            set {
                _storeMessages = value;
            }
        }

        /// The the capacity of the message store.
        /// </summary>
        /// <remarks>
        /// If the maximum capacity is exceeded, messages from the beginning are removed.
        /// If no capacity is set, the debugger uses the default capacity (1024).
        /// </remarks>
        public static int MessageStoreCapacity {
            get { return _messageStoreCapacity; }
            set {
                if(value <= 0) {
                    throw new InvalidDataException("Invalid capacity (not allowed <= 0)");
                }

                _messageStoreCapacity = value;
            }
        }
        
        /// <summary>
        /// Indicates whether or not the application should thrown an exception when an assertion fails.
        /// </summary>
        public static bool BreakOnFailedAssertion {
            get { return _breakOnFailedAssertion; }
            set { _breakOnFailedAssertion = value; }
        }
        
        /// <summary>
        /// Indicates whether or not to log the message to the debugger.
        /// </summary>
        public bool LogToDebugger {
            get { return _logToDebugger; }
            set { _logToDebugger = value; }
        }
        
        /// <summary>
        /// Indicates whether or not stack information should be stored for each message.
        /// </summary>
        /// <remarks>True by default.</remarks>
        public static bool SaveStackInfo {
            get { return _saveStackInfo; }
            set { _saveStackInfo = value; }
        }

        
        /// <summary>
        /// Indicates whether or not the debugger is enabled or not.
        /// </summary>
        /// <remarks>True by default.</remarks>
        public static bool Enabled {
            get { return _debuggerEnabled; }
            set { _debuggerEnabled = value; }
        }
        
        /// <summary>
        /// Specifies the path of the message viewer application.
        /// </summary>
        /// <remarks>
        /// The application is launched with a command-line parameter
        /// (ex: C:\viewer.exe C:\messages.xml)
        /// </remarks>
        public static string DebugMessageViewerPath {
            get { return _debugMessageViewerPath; }
            set { _debugMessageViewerPath = value; }
        }
        
        /// <summary>
        /// Specifies the environmental variable used to locate
        /// the viewer application.
        /// </summary>
        /// <remarks>The variable is used only if DebugMessageViewerPath is not set.</remarks>
        public static string DebugMessageViewerEnvironmentVariable {
            get { return _debugMessageViewerEnvironmentVariable; }
            set { _debugMessageViewerEnvironmentVariable = value; }
        }


        /// <summary>
        /// Specifies whether or not a viewer application is set.
        /// </summary>
        public static bool HasMessageViewerAttached {
            get {
                return (!string.IsNullOrEmpty(_debugMessageViewerPath) ||
                       (_debugMessageViewerEnvironmentVariable != null && _debugMessageViewerEnvironmentVariable.Length > 0));
            }
        }

        /// <summary>
        /// The number of messages stored in the debugger store.
        /// </summary>
        public static int StoredMessageCount {
            get {
                if(messages != null) {
                    return messages.Count;
                }

                return 0;
            }
        }

        /// <summary>
        /// Provides access to the list of debug messages.
        /// </summary>
        public static List<DebugMessage> DebugMessages {
            get { return messages; }
            set { messages = value; }
        }

        /// <summary>
        /// The color that should be associated with the message.
        /// </summary>
        public static Color Color {
            get { return _color; }
            set { _color = value; }
        }

        #endregion

        #region Constructor

        static Debug() {
            contextStack = new Stack<DebugContext>();

            // load configuration
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            DebugConfig debugConfig = (DebugConfig)config.Sections["DebugConfig"];

            if(debugConfig != null) {
                _debuggerEnabled = debugConfig.Debug;
                _breakOnFailedAssertion = debugConfig.Assert;
                _storeMessages = debugConfig.Store;
                _logToDebugger = debugConfig.LogToDebugger;
                _saveStackInfo = debugConfig.SaveStackInfo;
            }

            // set default values
            _color = MessageColors.DefaultColor;
        }

        #endregion

        #region Private methods

        #region Debug Message initialization

        /// <summary>
        /// Allocates and initializes a message with its type and message.
        /// </summary>
        /// <param name="type">The type of the message.</param>
        /// <param name="message">The message string.</param>
        /// <returns>A <typeparamref name="DebugMessage"/> object.</returns>
        private static DebugMessage InitDebugMessage(DebugMessageType type, string message) {
            DebugMessage debugMessage = new DebugMessage();

            // assign current time
            debugMessage.Type = type;
            debugMessage.Time = DateTime.Now;
            debugMessage.Color = _color;

            // set context
            if(contextStack.Count > 0) {
                debugMessage.Context = contextStack.Peek();
            }
            else {
                debugMessage.Context = null;
            }

            // set message
            if(message != null) {
                debugMessage.Message = message;
            }

            // set thread info
            debugMessage.ThreadName = Thread.CurrentThread.Name;
            debugMessage.ThreadId = AppDomain.GetCurrentThreadId();
            return debugMessage;
        }

        /// <summary>
        /// Allocates and initializes a message with its type.
        /// </summary>
        /// <param name="type">The type of the message.</param>
        /// <returns>A <typeparamref name="DebugMessage"/> object.</returns>
        private static DebugMessage InitDebugMessage(DebugMessageType type) {
            return InitDebugMessage(type, null);
        }

        /// <summary>
        /// Allocates an empty message.
        /// </summary>
        /// <returns>A <typeparamref name="DebugMessage"/> object.</returns>
        private static DebugMessage InitDebugMessage() {
            DebugMessage debugMessage = new DebugMessage();
            debugMessage.Time = DateTime.Now;
            return debugMessage;
        }

        #endregion

        /// <summary>
        /// Allocates the message store list if it's not allocated yet
        /// </summary>
        private static void AssureMessageListIsAllocated() {
            if(messages == null) {
                lock(lockObject) {
                    messages = new List<DebugMessage>();
                }
            }
        }

        /// <summary>
        /// Tries to add the given message to the list.
        /// </summary>
        /// <param name="message">The message to add.</param>
        private static void AddMessageToStore(DebugMessage message) {
            if(message == null) {
                return;
            }

            AssureMessageListIsAllocated();

            lock(lockObject) {
                if(messages.Count == _messageStoreCapacity && messages.Count != 0) {
                    messages.RemoveAt(0);
                }

                messages.Add(message);
            }
        }

        /// <summary>
        /// Filter the message.
        /// </summary>
        /// <param name="message">The message to filter.</param>
        /// <returns>
        /// true if the message is allowed; false, otherwise.
        /// </returns>
        private static bool ExcludeMessage(DebugMessage message) {
            if(message == null) {
                return true;
            }

            if(filters != null && filters.Count > 0) {
                bool? result = null;

                for(int i = 0; i < filters.Count; i++) {
                    if(filters[i].Enabled == false) {
                        continue;
                    }

                    if(filters[i].Implication == MessageFilterImplication.OR) {
                        if(filters[i].AllowMessage(message)) {
                            return true;
                        }
                    }
                    else // AND implication
                    {
                        // combine the result
                        if(result.HasValue) {
                            result &= filters[i].AllowMessage(message);
                        }
                        else {
                            result = filters[i].AllowMessage(message);
                        }

                        if(result.HasValue && result.Value) {
                            return true;
                        }
                    }
                }

                if(result.HasValue) {
                    return result.Value;
                }
                else {
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Tries to send the given message to all attached listeners.
        /// </summary>
        /// <param name="message">The message to be sent.</param>
        private static void SendMessageToListeners(DebugMessage message) {
            if(message == null) {
                return;
            }

            // check if there is any listener attached
            if(listeners == null || listeners.Count == 0) {
                return;
            }

            if(ExcludeMessage(message)) {
                return;
            }

            // send the message to all listeners
            for(int i = 0; i < listeners.Count; i++) {
                // skip disabled listeners
                if(listeners[i].Enabled == false) {
                    continue;
                }

                if(listeners[i].IsOpen == false) {
                    listeners[i].Open();
                }

                listeners[i].DumpMessage(message);
            }
        }

        /// <summary>
        /// Shows the debug message using the attached debug message notifier.
        /// </summary>
        /// <param name="message">The message to be sent.</param>
        private static void SendMessageToNotifier(DebugMessage message) {
            if(message == null) {
                return;
            }

            if(DebugMessageNotifier != null && DebugMessageNotifier.Enabled) {
                DebugMessageNotifier.Message = message;

                if(DebugMessageNotifier.Launch() == false) {
                    Console.WriteLine("Couldn't launch IDebugMessageNotifier {0}", 
                                      DebugMessageNotifier.GetType().Name);
                }
            }
        }

        /// <summary>
        /// Tries to add to the message all the stack information available at the given moment.
        /// </summary>
        /// <param name="message">The message to which to attach the stack information.</param>
        private static void AddStackInfoToMessage(DebugMessage message) {
            if(message == null) {
                return;
            }

            // check if we need to allocate the stack segments
            if(message.HasStack && message.StackSegments != null) {
                message.StackSegments.Clear();
            }
            else {
                message.StackSegments = new List<StackSegment>();
                message.HasStack = true;
            }

            // obtain stack information
            StackTrace stackInfo = new StackTrace(0, true);

            for(int i = 0; i < stackInfo.FrameCount; i++) {
                StackSegment segment = new StackSegment();
                StackFrame frame = stackInfo.GetFrame(i);

                if(frame.GetMethod().DeclaringType.Namespace == typeof(Debug).Namespace) {
                    continue;
                }

                // copy the relevant data
                segment.File = frame.GetFileName();
                segment.DeclaringObject = frame.GetMethod().DeclaringType.Name;
                segment.DeclaringNamespace = frame.GetMethod().DeclaringType.Namespace;
                segment.Method = frame.GetMethod().ToString();
                AddMethodType(frame, segment);

                segment.Line = frame.GetFileLineNumber();
                message.StackSegments.Add(segment);
            }
        }

        /// <summary>
        /// Add the Method type to the specified StackSegment
        /// </summary>
        /// <param name="frame">The Stack frame.</param>
        /// <param name="segment">The Stack segment.</param>
        private static void AddMethodType(StackFrame frame, StackSegment segment) {
            MethodBase Method = frame.GetMethod();

            if(Method.IsPublic) {
                segment.Type = MethodType.Public;
            }
            else if(Method.IsPrivate) {
                segment.Type = MethodType.Private;
            }
            else if(Method.IsStatic) {
                segment.Type = MethodType.Static;
            }
            else if(Method.IsVirtual) {
                segment.Type = MethodType.Virtual;
            }
            else if(Method.IsAbstract) {
                segment.Type = MethodType.Abstract;
            }
        }

        /// <summary>
        /// Tries to add the stack information for the Method that generated the given message.
        /// </summary>
        /// <param name="message">The message to which to attach the information.</param>
        private static void AddBaseMethodInfoToMessage(DebugMessage message) {
            if(message == null) {
                return;
            }

            // obtain stack information
            StackFrame frame = GetTopMethod();

            if(frame != null) {
                // copy the relevant data
                message.BaseMethod.File = frame.GetFileName();
                message.BaseMethod.DeclaringObject = frame.GetMethod().DeclaringType.Name;
                message.BaseMethod.DeclaringNamespace = frame.GetMethod().DeclaringType.Namespace;
                message.BaseMethod.Method = frame.GetMethod().ToString();
                AddMethodType(frame, message.BaseMethod);

                message.BaseMethod.Line = frame.GetFileLineNumber();
            }
        }


        /// <summary>
        /// Get the Method that called the debugger.
        /// </summary>
        /// <returns>
        /// A <typeparamref name="StackFrame"/> object if the calling method could be found;
        /// null, otherwise.
        /// </returns>
        private static StackFrame GetTopMethod() {
            StackTrace stackInfo = new StackTrace(0, true);

            for(int i = 0; i < stackInfo.FrameCount; i++) {
                StackFrame frame = stackInfo.GetFrame(i);
                MethodBase Method = frame.GetMethod();

                if(Method.ReflectedType.Namespace != typeof(Debug).Namespace) {
                    return frame;
                }
            }

            return null;
        }


        /// <summary>
        /// Get the debugger settings. Custom settings can be specified on a object/method 
        /// basis using the DebugOptions attribute.
        /// </summary>
        /// <returns>
        /// The debug settings attached to the calling method.
        /// </returns>
        private static DebugSettings GetSettings() {
            // find the Method that called the debugger
            StackFrame topFrame;
            MethodBase topMethod = null;
            bool debugAttributeFound = false;
            DebugOptions options = new DebugOptions();
            DebugSettings settings = new DebugSettings();
            topFrame = GetTopMethod();

            if(topFrame != null) {
                topMethod = topFrame.GetMethod();
            }

            // get the attributes
            if(topMethod != null) {
                object[] attributes = topMethod.GetCustomAttributes(typeof(DebugOptions), false);

                foreach(Attribute a in attributes) {
                    // extract the options
                    DebugOptions attributeOptions = (DebugOptions)a;

                    if(attributeOptions.isDebugSet) {
                        options.Debug = attributeOptions.Debug;
                    }

                    if(attributeOptions.isAssertSet) {
                        options.Assert = attributeOptions.Assert;
                    }

                    if(attributeOptions.isStoreSet) {
                        options.Store = attributeOptions.Store;
                    }

                    if(attributeOptions.isSaveStackSet) {
                        settings.SaveStack = attributeOptions.SaveStack;
                    }

                    if(attributeOptions.isLogToDebuggerSet) {
                        settings.LogToDebugger = attributeOptions.LogToDebugger;
                    }

                    if(attributeOptions.isSendToListnersSet) {
                        settings.SendToListners = attributeOptions.SendToListners;
                    }

                    if(attributeOptions.isSendToNotifierSet) {
                        settings.SendToNotifier = attributeOptions.SendToNotifier;
                    }

                    debugAttributeFound = true;
                }

                if(debugAttributeFound == false) {
                    // check if the base class has the debug attribute
                    attributes = topMethod.DeclaringType.GetCustomAttributes(typeof(DebugOptions), false);

                    foreach(Attribute a in attributes) {
                        // extract the options
                        DebugOptions attributeOptions = (DebugOptions)a;

                        if(attributeOptions.isDebugSet) {
                            options.Debug = attributeOptions.Debug;
                        }

                        if(attributeOptions.isAssertSet) {
                            options.Assert = attributeOptions.Assert;
                        }

                        if(attributeOptions.isStoreSet) {
                            options.Store = attributeOptions.Store;
                        }

                        if(attributeOptions.isSaveStackSet) {
                            options.SaveStack = attributeOptions.SaveStack;
                        }

                        if(attributeOptions.isLogToDebuggerSet) {
                            options.LogToDebugger = attributeOptions.LogToDebugger;
                        }

                        if(attributeOptions.isSendToListnersSet) {
                            options.SendToListners = attributeOptions.SendToListners;
                        }

                        if(attributeOptions.isSendToNotifierSet) {
                            options.SendToNotifier = attributeOptions.SendToNotifier;
                        }

                        debugAttributeFound = true;
                    }
                }

                if(debugAttributeFound) {
                    settings.Debug = options.isDebugSet ? options.Debug : _debuggerEnabled;
                    settings.Assert = options.isAssertSet ? options.Assert : _breakOnFailedAssertion;
                    settings.Store = options.isStoreSet ? options.Store : _storeMessages;
                    settings.SaveStack = options.isSaveStackSet ? options.SaveStack : _saveStackInfo;
                    settings.LogToDebugger = options.isLogToDebuggerSet ? options.LogToDebugger : _logToDebugger;
                    settings.SendToListners = options.isSendToListnersSet ? options.SendToListners : true;
                    settings.SendToNotifier = options.isSendToNotifierSet ? options.SendToNotifier : true;
                }
            }

            if(debugAttributeFound == false) {
                settings.Debug = _debuggerEnabled;
                settings.Assert = _breakOnFailedAssertion;
                settings.Store = _storeMessages;
                settings.SaveStack = _saveStackInfo;
                settings.LogToDebugger = _logToDebugger;
                settings.SendToListners = true;
                settings.SendToNotifier = true;
            }

            return settings;
        }

        /// <summary>
        /// Get the format string.
        /// </summary>
        /// <param name="format">The string to look up for.</param>
        /// <returns>The string referenced by the "format" parameter.</returns>
        private static string GetFormatString(string format) {
            if(format == null) {
                return null;
            }

            if(format.Length > 0 && format[0] == '@') {
                return StringTable[format];
            }

            return format;
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Remove all messages from the store.
        /// </summary>
        public static void ClearMessageStore() {
            if(messages != null && messages.Count > 0) {
                messages.Clear();
            }
        }

        #region Listner control

        /// <summary>
        /// Attach a listener to the debugger.
        /// </summary>
        /// <param name="listener">The listener to be added</param>
        /// <returns>
        /// true if the listener could be added; false, otherwise.
        /// </returns>
        public static bool AddListner(IDebugListener listener) {
            if(listener == null) {
                throw new ArgumentNullException("listener");
            }

            // check if the list is allocated
            if(listeners == null) {
                listeners = new List<IDebugListener>();
            }

            // check if the listener is already added
            foreach(IDebugListener l in listeners) {
                if(object.ReferenceEquals(l, listener) || l.ListnerId == listener.ListnerId) {
                    return false;
                }
            }

            listeners.Add(listener);
            return true;
        }

        /// <summary>
        /// Detach a listener from the debugger.
        /// </summary>
        /// <param name="id">The Id of the listener.</param>
        public static void RemoveListner(int id) {
            if(listeners == null || listeners.Count == 0) {
                return;
            }

            // search through all listeners
            for(int i = 0; i < listeners.Count; i++) {
                if(listeners[i].ListnerId == id) {
                    listeners[i].Close();
                    listeners.Remove(listeners[i]);
                    return;
                }
            }
        }

        /// <summary>
        /// Detach all listeners attached to the debugger
        /// </summary>
        public static void RemoveAllListeners() {
            if(listeners == null || listeners.Count == 0) {
                return;
            }

            while(listeners.Count > 0) {
                RemoveListner(listeners[0].ListnerId);
            }
        }

        /// <summary>
        /// Returns a listener by it's Id.
        /// </summary>
        /// <param name="id">The Id of the listener.</param>
        /// <returns>
        /// A <typeparamref name="IDebugListener"/> object if the listener was found;
        /// null, otherwise.
        /// </returns>
        public static IDebugListener GetListnerById(int id) {
            if(listeners == null || listeners.Count == 0) {
                return null;
            }

            for(int i = 0; i < listeners.Count; i++) {
                if(listeners[i].ListnerId == id) {
                    return listeners[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Returns an listener by knowing it's index.
        /// </summary>
        /// <param name="index">The index of the listener.</param>
        /// <returns>
        /// A <typeparamref name="IDebugListener"/> object if the listener was found; null, otherwise.
        /// </returns>
        public static IDebugListener GetListnerByIndex(int index) {
            if(listeners == null || listeners.Count == 0 || index < 0 || index >= listeners.Count) {
                return null;
            }

            return listeners[index];
        }

        #endregion

        #region Filter control

        /// <summary>
        /// Attach a filter to the debugger.
        /// </summary>
        /// <param name="filter">The filter to be attached.</param>
        /// <returns>
        /// true if the filter could be attached; false, otherwise.
        /// </returns>
        public static bool AddFilter(IDebugMessageFilter filter) {
            if(filter == null) {
                return false;
            }

            if(filters == null) {
                filters = new List<IDebugMessageFilter>();
            }

            // check if the filter is already added
            foreach(IDebugMessageFilter f in filters) {
                if(object.ReferenceEquals(f, filter) || f.FilterId == filter.FilterId) {
                    return false;
                }
            }

            filters.Add(filter);
            return true;
        }

        /// <summary>
        /// Detach a filter from the debugger.
        /// </summary>
        /// <param name="filterId">The Id of the filter.</param>
        public static void RemoveFilter(int filterId) {
            if(filters == null || filters.Count == 0) {
                return;
            }

            for(int i = 0; i < filters.Count; i++) {
                if(filters[i].FilterId == filterId) {
                    filters.RemoveAt(i);
                    break;
                }
            }
        }

        /// <summary>
        /// Detach all filters from the debugger.
        /// </summary>
        public static void RemoveAllFilters() {
            if(filters == null || filters.Count == 0) {
                return;
            }

            filters.Clear();
        }

        /// <summary>
        /// Returns a filter by knowing it's Id.
        /// </summary>
        /// <param name="filterId">The Id of the filter.</param>
        /// <returns>
        /// A <typeparamref name="IDebugMessageFilter"/> object if the filter was found; null, otherwise.
        /// </returns>
        public static IDebugMessageFilter GetFilterById(int filterId) {
            if(filters == null || filters.Count == 0) {
                return null;
            }

            for(int i = 0; i < filters.Count; i++) {
                if(filters[i].FilterId == filterId) {
                    return filters[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Return a filter by knowing it's index.
        /// </summary>
        /// <param name="index">The Id of the index.</param>
        /// <returns>
        /// A <typeparamref name="IDebugMessageFilter"/> object if the filter was found;
        /// null, otherwise.
        /// </returns>
        public static IDebugMessageFilter GetFilterByIndex(int index) {
            if(filters == null || filters.Count == 0 || index < 0 || index >= filters.Count) {
                return null;
            }

            return filters[index];
        }

        #endregion

        #region Assertions

        /// <summary>
        /// Performs an assertion.
        /// </summary>
        /// <param name="value">The value to test.</param>
        /// <remarks>The assertion fails if the giben value is false.</remarks>
        [Conditional("DEBUG")]
        public static void Assert(bool value) {
            Assert(value, null);
        }

        /// <summary>
        /// Performs an assertion.
        /// </summary>
        /// <param name="value">The value to test.</param>
        /// <param name="message">The string used to format the message.</param>
        /// <param name="parameters">The message parameters.</param>
        /// <remarks>The assertion fails if the giben value is false.</remarks>
        [Conditional("DEBUG")]
        public static void Assert(bool value, string format, params object[] args) {
            DebugSettings settings = GetSettings();

            if(settings.Debug == false) {
                return;
            }

            if(value == false) {
                string formattedMessage = null;
                format = GetFormatString(format);

                // format the message
                if(format != null) {
                    try {
                        formattedMessage = String.Format(format, args);
                    }
                    catch(Exception e) {
                        Console.WriteLine(e.Message);
                        return;
                    }
                }

                // create the message
                DebugMessage message = InitDebugMessage(DebugMessageType.Error, formattedMessage != null ? 
                                                        formattedMessage : DefaultAssertMesssage);
                AddBaseMethodInfoToMessage(message);

                if(settings.SaveStack) {
                    AddStackInfoToMessage(message);
                }

                if(settings.Store) {
                    AddMessageToStore(message);
                }

                // send the message to all the listeners
                if(settings.SendToListners) {
                    SendMessageToListeners(message);
                }

                if(settings.LogToDebugger) {
                    System.Diagnostics.Debugger.Log(0, "", message.Message);
                }

                if(settings.SendToNotifier) {
                    SendMessageToNotifier(message);
                }

                if(settings.Assert) {
                    throw new DebugException(formattedMessage != null ? formattedMessage : DefaultAssertMesssage);
                }
            }
        }


        /// <summary>
        /// Performs a type assertion.
        /// </summary>
        /// <param name="obj">The object to check.</param>
        /// <param name="type">The type to check for.</param>
        /// <param name="format">The string used to format the message.</param>
        /// <param name="args">The message parameters.</param>
        [Conditional("DEBUG")]
        public static void AssertType(object obj, Type type, string format, params object[] args) {
            if(obj == null || type == null) {
                return;
            }

            DebugSettings settings = GetSettings();

            if(settings.Debug == false) {
                return;
            }

            // check if object is of given type
            if(type.IsInstanceOfType(obj) == false) {
                string formattedMessage = null;
                format = GetFormatString(format);

                // format the message
                if(format != null) {
                    try {
                        formattedMessage = String.Format(format, args);
                    }
                    catch(Exception e) {
                        Console.WriteLine(e.Message);
                        return;
                    }
                }

                DebugMessage message = InitDebugMessage(DebugMessageType.Error, formattedMessage != null ? 
                                                        formattedMessage : DefaultAssertTypeMesssage);
                AddBaseMethodInfoToMessage(message);

                if(settings.SaveStack) {
                    AddStackInfoToMessage(message);
                }

                if(settings.Store) {
                    AddMessageToStore(message);
                }

                // send the message to all the listeners
                if(settings.SendToListners) {
                    SendMessageToListeners(message);
                }

                if(settings.LogToDebugger) {
                    System.Diagnostics.Debugger.Log(0, "", message.Message);
                }

                if(settings.SendToNotifier) {
                    SendMessageToNotifier(message);
                }

                if(settings.Assert) {
                    throw new DebugException(formattedMessage != null ? 
                                             formattedMessage : DefaultAssertTypeMesssage);
                }
            }
        }

        /// <summary>
        /// Performs a type assertion.
        /// </summary>
        /// <param name="obj">The object to check.</param>
        /// <param name="type">The type to check for.</param>
        [Conditional("DEBUG")]
        public static void AssertType(object obj, Type type) {
            AssertType(obj, type, null);
        }

        /// <summary>
        /// Performs a null assertion.
        /// </summary>
        /// <param name="obj">The object to check.</param>
        [Conditional("DEBUG")]
        public static void AssertNotNull(object obj) {
            AssertNotNull(obj, null);
        }

        /// <summary>
        /// Performs a null assertion.
        /// </summary>
        /// <param name="obj">The object to check.</param>
        /// <param name="format">The string used to format the message.</param>
        /// <param name="args">The message parameters.</param>
        [Conditional("DEBUG")]
        public static void AssertNotNull(object obj, string format, params object[] args) {
            // check if object is of given type
            if(obj == null) {
                string formattedMessage = null;
                format = GetFormatString(format);

                // format the message
                if(format != null) {
                    try {
                        formattedMessage = String.Format(format, args);
                    }
                    catch(Exception e) {
                        Console.WriteLine(e.Message);
                        return;
                    }
                }

                DebugSettings settings = GetSettings();
                
                if(settings.Debug == false) {
                    return;
                }

                DebugMessage message = InitDebugMessage(DebugMessageType.Error, formattedMessage != null ? 
                                                        formattedMessage : DefaultAssertNotNullMesssage);
                AddBaseMethodInfoToMessage(message);

                if(settings.SaveStack) {
                    AddStackInfoToMessage(message);
                }

                if(settings.Store) {
                    AddMessageToStore(message);
                }

                // send the message to all the listeners
                if(settings.SendToListners) {
                    SendMessageToListeners(message);
                }

                if(settings.LogToDebugger) {
                    System.Diagnostics.Debugger.Log(0, "", message.Message);
                }

                if(settings.SendToNotifier) {
                    SendMessageToNotifier(message);
                }

                if(settings.Assert) {
                    throw new DebugException(formattedMessage != null ? formattedMessage :
                                             DefaultAssertNotNullMesssage);
                }
            }
        }

        #endregion

        #region Action reporting

        /// <summary>
        /// Reports the given error to the debugger.
        /// </summary>
        /// <param name="format">The string used to format the message.</param>
        /// <param name="args">The message parameters.</param>
        public static void ReportError(string format, params object[] args) {
            if(format == null) {
                return;
            }

            DebugSettings settings = GetSettings();

            if(settings.Debug == false) {
                return;
            }

            string formattedMessage = null;
            format = GetFormatString(format);

            try {
                formattedMessage = String.Format(format, args);
            }
            catch(Exception e) {
                Console.WriteLine(e.Message);
                return;
            }

            DebugMessage message = InitDebugMessage(DebugMessageType.Error, formattedMessage);
            AddBaseMethodInfoToMessage(message);

            if(settings.SaveStack) {
                AddStackInfoToMessage(message);
            }

            if(settings.Store) {
                AddMessageToStore(message);
            }

            // send the message to all the listeners
            if(settings.SendToListners) {
                SendMessageToListeners(message);
            }

            if(settings.LogToDebugger) {
                System.Diagnostics.Debugger.Log(0, "", message.Message);
            }

            if(settings.SendToNotifier) {
                SendMessageToNotifier(message);
            }
        }

        /// <summary>
        /// Reports the given warning to the debugger.
        /// </summary>
        /// <param name="format">The string used to format the message.</param>
        /// <param name="args">The message parameters.</param>
        public static void ReportWarning(string format, params object[] args) {
            if(format == null) {
                return;
            }

            DebugSettings settings = GetSettings();

            if(settings.Debug == false) {
                return;
            }

            string formattedMessage = null;
            format = GetFormatString(format);

            try {
                formattedMessage = String.Format(format, args);
            }
            catch(Exception e) {
                Console.WriteLine(e.Message);
                return;
            }

            DebugMessage message = InitDebugMessage(DebugMessageType.Warning, formattedMessage);
            AddBaseMethodInfoToMessage(message);

            if(settings.SaveStack) {
                AddStackInfoToMessage(message);
            }

            if(settings.Store) {
                AddMessageToStore(message);
            }

            // send the message to all the listeners
            if(settings.SendToListners) {
                SendMessageToListeners(message);
            }

            if(settings.LogToDebugger) {
                System.Diagnostics.Debugger.Log(0, "", message.Message);
            }

            if(settings.SendToNotifier) {
                SendMessageToNotifier(message);
            }
        }

        /// <summary>
        /// Reports the given message to the debugger.
        /// </summary>
        /// <param name="format">The string used to format the message.</param>
        /// <param name="args">The message parameters.</param>
        public static void Report(string format, params object[] args) {
            if(format == null) {
                return;
            }

            DebugSettings settings = GetSettings();

            if(settings.Debug == false) {
                return;
            }

            string formattedMessage = null;
            format = GetFormatString(format);

            try {
                formattedMessage = String.Format(format, args);
            }
            catch(Exception e) {
                Console.WriteLine(e.Message);
                return;
            }

            DebugMessage message = InitDebugMessage(DebugMessageType.Unknown, formattedMessage);
            AddBaseMethodInfoToMessage(message);

            if(settings.SaveStack) {
                AddStackInfoToMessage(message);
            }

            if(settings.Store) {
                AddMessageToStore(message);
            }

            // send the message to all the listeners
            if(settings.SendToListners) {
                SendMessageToListeners(message);
            }

            if(settings.LogToDebugger) {
                System.Diagnostics.Debugger.Log(0, "", message.Message);
            }

            if(settings.SendToNotifier) {
                SendMessageToNotifier(message);
            }
        }

        #endregion

        #region Data reporting

        /// <summary>
        /// Reports the given data and message to the debugger.
        /// </summary>
        /// <param name="dataType">The type of the data.</param>
        /// <param name="data">The data. It must be a serializable object.</param>
        /// <param name="args">The message parameters.</param>
        public static void ReportData(DataType dataType, object data, string format, params object[] args) {
            if(format == null) {
                return;
            }

            DebugSettings settings = GetSettings();
            
            if(settings.Debug == false) {
                return;
            }

            string formattedMessage = null;
            format = GetFormatString(format);

            try {
                formattedMessage = String.Format(format, args);
            }
            catch(Exception e) {
                Console.WriteLine(e.Message);
                return;
            }

            DebugMessage message = InitDebugMessage(DebugMessageType.Error, formattedMessage);
            AddBaseMethodInfoToMessage(message);

            if(settings.SaveStack) {
                AddStackInfoToMessage(message);
            }

            // add data
            message.DataType = dataType;
            message.Data = data;

            if(settings.Store) {
                AddMessageToStore(message);
            }

            // send the message to all the listeners
            if(settings.SendToListners) {
                SendMessageToListeners(message);
            }

            if(settings.LogToDebugger) {
                System.Diagnostics.Debugger.Log(0, "", message.Message);
            }

            if(settings.SendToNotifier) {
                SendMessageToNotifier(message);
            }
        }

        /// <summary>
        /// Reports the given data and message to the debugger.
        /// </summary>
        /// <param name="data">The string data.</param>
        /// <param name="args">The message parameters.</param>
        public static void ReportData(string data, string format, params object[] args) {
            ReportData(DataType.Text, data, format, args);
        }

        /// <summary>
        /// Reports the given data and message to the debugger.
        /// </summary>
        /// <param name="data">The binary data.</param>
        /// <param name="args">The message parameters.</param>
        public static void ReportData(byte[] data, string format, params object[] args) {
            ReportData(DataType.Binary, data, format, args);
        }

        /// <summary>
        /// Reports the given data and message to the debugger.
        /// </summary>
        /// <param name="data">The Stream data.</param>
        /// <param name="args">The message parameters.</param>
        public static void ReportData(Stream data, string format, params object[] args) {
            ReportData(DataType.Stream, data, format, args);
        }

        #endregion

        #region Serialization

        /// <summary>
        /// Serializes all stored messages in XML format.
        /// </summary>
        /// <param name="path">The path where to store the serialized message.</param>
        /// <returns>
        /// true if the messages were successfully serialized.
        /// false if the message couldn't be serialized.
        /// </returns>
        public static bool SerializeMessages(string path) {
            if(messages == null || path == null) {
                return false;
            }

            StreamWriter writer = null;

            try {
                XmlSerializer serializer = new XmlSerializer(typeof(List<DebugMessage>));
                writer = new StreamWriter(path);

                if(writer.BaseStream.CanWrite) {
                    serializer.Serialize(writer, messages);
                }
                else {
                    return false;
                }
            }
            catch(Exception e) {
                Console.WriteLine(e.Message);
                return false;
            }
            finally {
                if(writer != null && writer.BaseStream.CanWrite) {
                    writer.Close();
                }
            }

            return true;
        }

        /// <summary>
        /// Deserializes all stored messages from XML format.
        /// </summary>
        /// <param name="path">The path from where to deserialize the message.</param>
        /// <returns>
        /// true if the messages were successfully deserialized.
        /// false if the message couldn't be deserialized.
        /// </returns>
        public static bool DeserializeMessages(string path) {
            if(path == null) {
                return false;
            }

            StreamReader reader = null;

            try {
                XmlSerializer serializer = new XmlSerializer(typeof(List<DebugMessage>));
                reader = new StreamReader(path);

                if(reader.BaseStream.CanRead) {
                    messages = (List<DebugMessage>)serializer.Deserialize(reader);
                }
                else {
                    return false;
                }
            }
            catch(Exception e) {
                Console.WriteLine(e.Message);
                return false;
            }
            finally {
                if(reader != null) {
                    reader.Close();
                }
            }

            return true;
        }

        /// <summary>
        /// Serializes all stored messages in XML format.
        /// </summary>
        /// <param name="path">The memory stream where to store the serialized message.</param>
        /// <returns>
        /// true if the message was successfully serialized.
        /// false if the message couldn't be serialized.
        /// </returns>
        public static bool SerializeMessages(MemoryStream memoryStream) {
            if(messages == null || memoryStream == null) {
                return false;
            }

            try {
                XmlSerializer serializer = new XmlSerializer(typeof(List<DebugMessage>));

                if(memoryStream.CanWrite) {
                    serializer.Serialize(memoryStream, messages);
                }
                else {
                    return false;
                }
            }
            catch(Exception e) {
                Console.WriteLine(e.Message);
                return false;
            }

            return true;
        }


        /// <summary>
        /// Serializes all stored messages in XML and compresses them in ZIP format
        /// </summary>
        /// <param name="path">The path where to store the serialized messages.</param>
        /// <returns>
        /// true if the message was successfully serialized.
        /// false if the message couldn't be serialized.
        /// </returns>
        public static bool SerializeMessagesCompressed(string path) {
            if(messages == null || path == null) {
                return false;
            }

            try {
                XmlSerializer serializer = new XmlSerializer(typeof(List<DebugMessage>));
                MemoryStream memoryMessages = new MemoryStream();
                MemoryStream memoryStream = new MemoryStream();

                // serialize the messages in memory
                serializer.Serialize(memoryMessages, messages);

                byte[] buffer = new byte[memoryMessages.Length];
                memoryMessages.Position = 0;
                memoryMessages.Read(buffer, 0, (int)memoryMessages.Length);

                // compress the stream from memory
                GZipStream zipStream = new GZipStream(memoryStream, CompressionMode.Compress, true);
                zipStream.Write(buffer, 0, (int)memoryMessages.Length);
                zipStream.Close();

                FileStream fileStream = new FileStream(path, FileMode.OpenOrCreate);
                fileStream.Position = 0;

                // write the compressed stream to the File
                if(fileStream.CanWrite) {
                    buffer = new byte[memoryStream.Length];
                    memoryStream.Position = 0;
                    memoryStream.Read(buffer, 0, (int)memoryStream.Length);

                    fileStream.Write(buffer, 0, (int)memoryStream.Length);
                    fileStream.Close();
                }
                else {
                    return false;
                }
            }
            catch(Exception e) {
                Console.WriteLine(e.Message);
                return false;
            }

            return true;
        }

        #endregion

        #region Message viewer

        /// <summary>
        /// Launch the attached debug message viewer
        /// </summary>
        /// <param name="path">The File to open.</param>
        /// <returns>
        /// true if the viewer could be launched; false, otherwise.
        /// </returns>
        public static bool LaunchDebugMessageViewer(string path) {
            if(path == null) {
                throw new ArgumentNullException("path");
            }

            if(File.Exists(path) == false) {
                return false;
            }

            // figure out the viewer path
            // if the given path is not valid, try to load it from the environment variable.
            string viewerPath = _debugMessageViewerPath;

            if(viewerPath == null && _debugMessageViewerEnvironmentVariable != null) {
                viewerPath = Environment.GetEnvironmentVariable(_debugMessageViewerEnvironmentVariable);
            }

            if(viewerPath == null || File.Exists(viewerPath) == false) {
                return false;
            }

            // launch the viewer
            try {
                Process.Start(viewerPath, path);
                return true;
            }
            catch(Exception e) {
                Console.WriteLine(e.Message);
                return false;
            }
        }


        /// <summary>
        /// Launch the attached debug message viewer
        /// </summary>
        /// <remarks>A temporary XML File will be generated containing the serialized messages.</remarks>
        /// <returns>
        /// true if the viewer could be launched;
        /// false, otherwise.
        /// </returns>
        public static bool LaunchDebugMessageViewer() {
            // save to a temporary File
            string filePath = Path.GetTempPath() + Path.GetRandomFileName() + ".xml";

            if(SerializeMessages(filePath)) {
                return LaunchDebugMessageViewer(filePath);
            }

            return false;
        }

        #endregion

        #region HTML report

        /// <summary>
        /// Generate a report in HTML format about the stored debug messages
        /// </summary>
        /// <param name="path">The location where to save the report.</param>
        /// <param name="title">The title of the report.</param>
        /// <param name="open">Specifies whether or not to open the report in the default browser.</param>
        /// <returns>
        /// true if the report could be generated;
        /// false, otherwise.
        /// </returns>
        public static bool GenerateHtmlReport(string path, string title, bool open) {
            if(path == null) {
                throw new ArgumentNullException("path");
            }

            StreamWriter writer = new StreamWriter(path);

            if(writer.BaseStream.CanWrite == false) {
                return false;
            }

            // used to count the messages based on their type
            int[] typeCount = new int[3];
            typeCount[0] = typeCount[1] = typeCount[2] = 0;

            writer.WriteLine("<html>");
            writer.WriteLine("<head>");
            writer.WriteLine("<title>" + (title == null ? "Debug report" : title) + "</title>");
            writer.WriteLine("</head>");
            writer.WriteLine("<body bgcolor=\"#FFFFFF\" text=\"black\" link=\"blue\" vlink=\"purple\" alink=\"red\">");
            writer.WriteLine("<p><font face=\"Arial\" size=\"5\" color=\"#525674\"><b>" + (title == null ? "Debug report" : title) +
                             "</b></font><br><font face=\"Arial\" size=\"4\" color=\"#525674\">Report created on: " +
                             DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString() + "</font><font color=\"#525674\"><br></font></p>");

            if(messages != null && messages.Count > 0) {
                // begin a table
                writer.WriteLine("<table border=\"1\" cellspacing=\"0\" width=\"100%\" bordercolordark=\"white\" bordercolorlight=\"black\">");
                writer.WriteLine("<tr bgcolor=\"#525674\">");

                // write the columns
                writer.WriteLine("<td align=\"center\" width=\"2%\"><p><font face=\"Arial\" size=\"2\" color=\"white\"><b>ID</b></font></p></td>");
                writer.WriteLine("<td align=\"center\" width=\"6%\"><p><font face=\"Arial\" size=\"2\" color=\"white\"><b>Time</b></font></p></td>");
                writer.WriteLine("<td width=\"11%\"><p><font face=\"Arial\" size=\"2\" color=\"white\"><b>File</b></font></p></td>");
                writer.WriteLine("<td align=\"center\" width=\"4%\"><p><font face=\"Arial\" size=\"2\" color=\"white\"><b>Line</b></font></p></td>");
                writer.WriteLine("<td width=\"78%\"><p><font face=\"Arial\" size=\"2\" color=\"white\"><b>Message</b></font></p></td>");
                writer.WriteLine("</tr>\n");

                // write the messages
                int ct = 0;
                foreach(DebugMessage m in messages) {
                    writer.WriteLine((ct % 2 == 0) ? "<tr bgcolor=\"FFFFFF\">" : "<tr bgcolor=\"#E9E9F3\">");

                    // set the background color according to the type of the message
                    // error   - bright red
                    // warning - bright yellow
                    // unknown - bright blue
                    switch(m.Type) {
                        case DebugMessageType.Error: {
                                writer.Write("<td align=\"center\" width=\"57\" bgcolor=\"#FFCCCC\"><p><font face=\"Arial\" size=\"2\" color=\"black\">");
                                typeCount[0]++;
                                break;
                            }
                        case DebugMessageType.Warning: {
                                writer.Write("<td align=\"center\" width=\"57\" bgcolor=\"#FFFFCC\"><p><font face=\"Arial\" size=\"2\" color=\"black\">");
                                typeCount[1]++;
                                break;
                            }
                        case DebugMessageType.Unknown: {
                                writer.Write("<td align=\"center\" width=\"57\" bgcolor=\"#C7E0F9\"><p><font face=\"Arial\" size=\"2\" color=\"black\">");
                                typeCount[2]++;
                                break;
                            }
                    }

                    // message number
                    writer.Write(((int)(ct + 1)).ToString());
                    writer.WriteLine("</font></p></td>");

                    // time
                    writer.Write("<td align=\"center\" width=\"100\"><p><font face=\"Arial\" size=\"2\" color=\"black\">");
                    writer.Write(m.Time.ToLongTimeString() + ":" + m.Time.Millisecond.ToString());
                    writer.WriteLine("</font></p></td>");

                    // source File
                    writer.Write("<td width=\"168\"><p><font face=\"Arial\" size=\"2\" color=\"black\">");
                    writer.Write(Path.GetFileName(m.BaseMethod.File));
                    writer.WriteLine("</font></p></td>");

                    // source Line
                    writer.Write("<td align=\"center\" width=\"70\"><p><font face=\"Arial\" size=\"2\" color=\"black\">");
                    writer.Write(m.BaseMethod.Line.ToString());
                    writer.WriteLine("</font></p></td>");

                    // message
                    writer.Write("<td><p><font face=\"Arial\" size=\"2\" color=\"black\">");
                    writer.Write(m.Message);
                    writer.WriteLine("</font></p></td>");
                    writer.WriteLine("</tr>\n");

                    ct++;
                }

                // close the table
                writer.WriteLine("</table>");
            }

            // write a summary
            writer.WriteLine("<p><font face=\"Arial\" size=\"3\" color=\"#525674\">Total Messages: " + StoredMessageCount.ToString() + "<br></font>");
            writer.WriteLine("<font face=\"Arial\" size=\"3\" color=\"#525674\"><br>Errors: " + typeCount[0].ToString() + "<br></font>");
            writer.WriteLine("<font face=\"Arial\" size=\"3\" color=\"#525674\">Warnings: " + typeCount[1].ToString() + "<br></font>");
            writer.WriteLine("<font face=\"Arial\" size=\"3\" color=\"#525674\">Unknown: " + typeCount[2].ToString() + "<br></font></p>");

            writer.WriteLine("<hr noshade color=\"#525674\">");
            writer.WriteLine("<font face=\"Arial\" color=\"#525674\"><span style=\"font-size:9pt;\">Generated by Debug Library v.1.0 &nbsp &nbsp| &nbsp &nbsp Copyright &copy 2007 <a href=\"mailto:lgratian@gmail.com\">Lup Gratian</a></span></font><span style=\"font-size:9pt;\">");
            writer.WriteLine("</body>");
            writer.WriteLine("</html>");
            writer.Close();

            if(open) {
                try {
                    Process.Start(path);
                }
                catch(Exception e) {
                    Console.WriteLine("Failed to open report. {0}", e.Message);
                }
            }

            return true;
        }

        /// <summary>
        /// Generate a report in HTML format about the stored debug messages
        /// </summary>
        /// <remarks>The report will be saved in the temporary directory and opened automatically in the default browser.</remarks>
        /// <returns>
        /// true if the report could be generated;
        /// false, otherwise.
        /// </returns>
        public static bool GenerateHtmlReport() {
            // save to a temporary File
            string filePath = Path.GetTempPath() + Path.GetRandomFileName() + ".htm";

            return GenerateHtmlReport(filePath, null, true);
        }

        #endregion

        #region Message context

        /// <summary>
        /// Enter a new message context.
        /// </summary>
        /// <param name="name">The name of the new context.</param>
        public static void EnterContext(string name) {
            if(name == null) {
                throw new ArgumentNullException("name");
            }

            // push the context on the stack
            DebugContext context = new DebugContext(name);
            context.Depth = contextStack.Count + 1;
            contextStack.Push(context);
        }

        /// <summary>
        /// Enter a new message context and associate data with it.
        /// </summary>
        /// <param name="context">The context to be set.</param>
        public static void EnterContext(DebugContext context) {
            if(context == null) {
                throw new ArgumentNullException("context");
            }

            // push the context on the stack
            context.Depth = contextStack.Count + 1;
            contextStack.Push(context);
        }

        /// <summary>
        /// Return to the previous context.
        /// </summary>
        public static void ExitContext() {
            if(contextStack.Count > 0) {
                contextStack.Pop();
            }
        }

        /// <summary>
        /// Reset the color to the default one.
        /// </summary>
        public static void ResetColor() {
            _color = MessageColors.DefaultColor;
        }

        #endregion

        #endregion

        #region Unhandled exception manager

        /// <summary>
        /// Provides support for unhandled exceptions and writing dump reports.
        /// </summary>
        public static ExceptionManager ExceptionManager = new ExceptionManager();

        #endregion

        #region Performance manager

        /// <summary>
        /// Provides support for monitoring the execution time.
        /// </summary>
        public static PerformanceManager PerformanceManager = new PerformanceManager();

        #endregion

        #region String table

        public static StringTable StringTable = new StringTable();

        #endregion

        #region Object counter

        /// <summary>
        /// Provides support for monitoring the count of created objects.
        /// </summary>
        public static ObjectCounter ObjectCounter = new ObjectCounter();

        #endregion

        #region WinSAT

        /// <summary>
        /// Provides support for retrieving the WinSAT (Windows Experience Index) scores under Windows Vista.
        /// </summary>
        public static WinSAT WinSAT = new WinSAT();

        #endregion

        #region Helpers

        /// <summary>
        /// Verifies if an exception is a critical one.
        /// </summary>
        /// <param name="ex">The exception to verify.</param>
        public static bool IsCriticalException(Exception ex) {
            return ((ex is AccessViolationException) || (ex is StackOverflowException) || (ex is OutOfMemoryException) || (ex is ThreadAbortException));
        }

        #endregion
    }
}
