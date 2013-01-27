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
using System.Drawing;

namespace DebugUtils.Debugger {
    #region Debug Message structures

    /// <summary>
    /// The possible types a message can be.
    /// </summary>
    [Flags]
    public enum DebugMessageType {
        Error = 0x01,
        Warning = 0x02,
        Unknown = 0x04,
    }


    /// <summary>
    /// The possible types a Method can be.
    /// </summary>
    public enum MethodType {
        Private,
        Public,
        Abstract,
        Virtual,
        Static
    }


    /// <summary>
    /// The possible types the data of a context can be.
    /// </summary>
    public enum DataType {
        None,
        Unknown,
        Text,
        Binary,
        Stream,
        Html,
        HtmlStream,
        Xml,
        XmlStream,
        DataSet,
        DataTable
    }


    /// <summary>
    /// Provides a set of colors that should be used for messages.
    /// </summary>
    public static class MessageColors {
        #region Fields

        public static readonly Color Transparent = Color.Transparent;
        public static readonly Color Yellow = Color.FromArgb(255, 255, 154);
        public static readonly Color YellowGreen = Color.FromArgb(235, 255, 154);
        public static readonly Color Green = Color.FromArgb(178, 255, 154);
        public static readonly Color Cyan = Color.FromArgb(154, 228, 255);
        public static readonly Color Blue = Color.FromArgb(134, 190, 255);
        public static readonly Color Violet = Color.FromArgb(162, 154, 255);
        public static readonly Color Purple = Color.FromArgb(213, 154, 255);
        public static readonly Color Pink = Color.FromArgb(255, 154, 217);
        public static readonly Color Red = Color.FromArgb(255, 164, 164);
        public static readonly Color Orange = Color.FromArgb(255, 192, 154);
        public static readonly Color OrangeYellow = Color.FromArgb(255, 235, 154);

        public static readonly int AvailableColors = 12;
        private static Random random;

        #endregion

        #region Properties

        /// <summary>
        /// Get the default color.
        /// </summary>
        public static Color DefaultColor {
            get { return Transparent; }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Get a color based on an number % AvailableColors.
        /// </summary>
        public static Color GetByNumber(int index) {
            index %= AvailableColors;

            if(index == 0) { return Transparent; }
            else if(index == 1) { return Yellow; }
            else if(index == 2) { return YellowGreen; }
            else if(index == 3) { return Green; }
            else if(index == 4) { return Cyan; }
            else if(index == 5) { return Blue; }
            else if(index == 6) { return Violet; }
            else if(index == 7) { return Purple; }
            else if(index == 8) { return Pink; }
            else if(index == 9) { return Red; }
            else if(index == 10) { return Orange; }
            else if(index == 11) { return OrangeYellow; }

            // not found
            return Transparent;
        }


        /// <summary>
        /// Get a random color.
        /// </summary>
        /// <returns></returns>
        public static Color GetRandom() {
            if(random == null) {
                random = new Random(Environment.TickCount);
            }

            return GetByNumber(random.Next(0, AvailableColors - 1));
        }

        #endregion
    }


    /// <summary>
    /// Stores information about an Method on the stack.
    /// </summary>
    [Serializable]
    public class StackSegment {
        private string _file;
        /// <summary>
        /// The source file where the Method call made.
        /// </summary>
        public string File {
            get { return _file; }
            set { _file = value; }
        }

        private string _declaringObject;
        /// <summary>
        /// The object in which the Method call is made.
        /// </summary>
        public string DeclaringObject {
            get { return _declaringObject; }
            set { _declaringObject = value; }
        }

        private string _declaringNamespace;
        /// <summary>
        /// The namespace where the Method call is made.
        /// </summary>
        public string DeclaringNamespace {
            get { return _declaringNamespace; }
            set { _declaringNamespace = value; }
        }

        private string _method;
        /// <summary>
        /// The name of the called Method.
        /// </summary>
        public string Method {
            get { return _method; }
            set { _method = value; }
        }

        private int _line;
        /// <summary>
        /// The Line at which the Method is called.
        /// </summary>
        public int Line {
            get { return _line; }
            set { _line = value; }
        }

        private MethodType _type;
        /// <summary>
        /// The type of the called Method.
        /// </summary>
        /// <remarks>See <see cref="MethodType"/> for details.</remarks>
        public MethodType Type {
            get { return _type; }
            set { _type = value; }
        }
    }


    /// <summary>
    /// Stores information about the context in which the messages are logged.
    /// </summary>
    public class DebugContext {
        #region Properties

        private string _name;
        /// <summary>
        /// The name of the context.
        /// </summary>
        public string Name {
            get { return _name; }
            set { _name = value; }
        }

        private int _depth;
        /// <summary>
        /// The depth at which the context sits.
        /// </summary>
        public int Depth {
            get { return _depth; }
            set { _depth = value; }
        }

        #endregion

        #region Constructor

        public DebugContext() {
        }

        public DebugContext(string name) {
            _name = name;
        }

        #endregion
    }


    /// <summary>
    /// The primary structure used to store message information.
    /// </summary>
    /// <remarks>
    /// Contains the message, information about the type of the
    /// message, the time at which the message was generated and information
    /// about the stack segments.
    /// </remarks>
    [Serializable]
    public class DebugMessage {
        private DebugMessageType _type;
        /// <summary>
        /// The type of the message.
        /// </summary>
        /// <remarks>See <see cref="DebugMessageType"/> for details.</remarks>
        public DebugMessageType Type {
            get { return _type; }
            set { _type = value; }
        }


        private DebugContext _context;
        /// <summary>
        /// The context in which the message was created.
        /// </summary>
        public DebugContext Context {
            get { return _context; }
            set { _context = value; }
        }


        private Color _color;
        /// <summary>
        /// The color asociated with this message.
        /// </summary>
        public Color Color {
            get { return _color; }
            set { _color = value; }
        }


        private string _message;
        /// <summary>
        /// The message string.
        /// </summary>
        public string Message {
            get { return _message; }
            set { _message = value; }
        }


        private DateTime _time;
        /// <summary>
        /// The time at which the message was generated.
        /// </summary>
        public DateTime Time {
            get { return _time; }
            set { _time = value; }
        }


        private StackSegment _baseMethod;
        /// <summary>
        /// Information (the stack segment) for the method
        /// that generated this message.
        /// </summary>
        public StackSegment BaseMethod {
            get { return _baseMethod; }
            set { _baseMethod = value; }
        }


        private bool _hasStack;
        /// <summary>
        /// Specifies if the message has information
        /// about the entire method stack.
        /// </summary>
        public bool HasStack {
            get { return _hasStack; }
            set { _hasStack = value; }
        }


        private List<StackSegment> _stackSegments;
        /// <summary>
        /// The list of all stack segments. Present only if HasStack is true.
        /// </summary>
        public List<StackSegment> StackSegments {
            get { return _stackSegments; }
            set { _stackSegments = value; }
        }


        private string _threadName;
        /// <summary>
        /// The name of the thread on which the message was generated.
        /// </summary>
        public string ThreadName {
            get { return _threadName; }
            set { _threadName = value; }
        }


        private int _threadId;
        /// <summary>
        /// The Id of the thread on which the message was generated.
        /// </summary>
        public int ThreadId {
            get { return _threadId; }
            set { _threadId = value; }
        }


        private DataType _dataType;
        /// <summary>
        /// The type of the data associated with the message.
        /// </summary>
        public DataType DataType {
            get { return _dataType; }
            set { _dataType = value; }
        }


        private object _data;
        /// <summary>
        /// The data associated with the message.
        /// </summary>
        public object Data {
            get { return _data; }
            set { _data = value; }
        }

        #region Constructor

        public DebugMessage() {
            _baseMethod = new StackSegment();
        }

        public DebugMessage(string message)
            : this() {
            _message = message;
            _time = DateTime.Now;
        }

        public DebugMessage(DebugMessageType messageType, DateTime time, string message)
            : this() {
            _type = messageType;
            _time = time;
            _message = message;
        }

        #endregion
    }

    #endregion

    #region Interfaces

    /// <summary>
    /// Interface that needs to be used by all listeners.
    /// </summary>
    public interface IDebugListener {
        /// <summary>
        /// Method called by the debugger the first time it uses the listener.
        /// </summary>
        /// <returns>
        /// true if the listener could be opened; 
        /// otherwise, false.
        /// </returns>
        bool Open();

        /// <summary>
        /// Method called by the debugger when the listener is no longer needed.
        /// </summary>
        /// <returns>
        /// true if the listener could be closed; 
        /// otherwise, false.
        /// </returns>
        bool Close();

        /// <summary>
        /// Method called by the debugger if a message needs to be handled
        /// by the listener.
        /// </summary>
        /// <param name="message">A <typeparamref name="DebugMessage"/> object that represents the message to be handled.</param>
        /// <returns>
        /// true if the message could be handled; 
        /// otherwise, false.
        /// </returns>
        bool DumpMessage(DebugMessage message);

        /// <summary>
        /// Gets or sets the Id of the listener.
        /// </summary>
        int ListnerId { get; set; }

        /// <summary>
        /// Gets the open state of the listener.
        /// </summary>
        bool IsOpen { get; }

        /// <summary>
        /// Gets or sets the enabled state of the listener.
        /// </summary>
        /// <remarks>
        /// If Enabled is set to false the debugger no longer calls
        /// the DumpMessage method to notify the listener about new messages.
        /// </remarks>
        bool Enabled { get; set; }
    }


    /// <summary>
    /// Specifies the way the result of the filter is treated.
    /// </summary>
    public enum MessageFilterImplication {
        AND, OR
    }


    /// <summary>
    /// Interface that needs to be used by all message filters.
    /// </summary>
    /// <remarks>
    /// Filters can be used to exclude some messages from being
    /// sent to the message listeners. For example, a filter can be
    /// set that allows only errors to be sent, not warnings and
    /// unknown message types.
    /// </remarks>
    public interface IDebugMessageFilter {
        /// <summary>
        /// Gets or sets the Id of the filter;
        /// </summary>
        int FilterId { get; set; }

        /// <summary>
        /// Gets or sets the enabled state of the filter.
        /// </summary>
        /// <remarks>
        /// If Enabled is set to false the debugger no longer calls
        /// the filter when filtering messages.
        /// </remarks>
        bool Enabled { get; set; }

        /// <summary>
        /// The implication of the filter in relationship with other ones.
        /// </summary>
        MessageFilterImplication Implication { get; set; }

        /// <summary>
        /// Called by the debugger when a message needs to be filtered.
        /// </summary>
        /// <param name="message">A <typeparamref name="DebugMessage"/> object that represents the message to be filter.</param>
        /// <returns>
        /// true if the message is allowed by the filter;
        /// otherwise, false.
        /// </returns>
        bool AllowMessage(DebugMessage message); // return true if the message should be blocked
    }



    /// <summary>
    /// Interface that needs to be used by all debug message notifiers.
    /// </summary>
    /// <remarks>
    /// Message notifiers can provide an interactive way
    /// for handling messages (WinForms dialogs, Task Dialogs under Windows Vista).
    /// </remarks>
    public interface IDebugMessageNotifier {
        /// <summary>
        /// Called when the notifier needs to be launched.
        /// </summary>
        /// <returns>
        /// true if the notifier could be launched;
        /// otherwise, false.
        /// </returns>
        bool Launch();

        /// <summary>
        /// Gets or sets the enabled state of the notifier.
        /// </summary>
        /// <remarks>
        /// If Enabled is set to false the debugger no longer launches the notifier.
        /// </remarks>
        bool Enabled { get; set; }
        DebugMessage Message { get; set; }
    }

    #endregion
}
