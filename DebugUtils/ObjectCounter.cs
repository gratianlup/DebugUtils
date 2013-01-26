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
using System.Reflection;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Xml.Serialization;
using System.Collections;

namespace DebugUtils.Debugger {
    /// <summary>
    /// Interface that needs to be used by all object counter notifiers.
    /// </summary>
    public interface IObjectCounterNotifier {
        /// <summary>
        /// Called when the notifier needs to be shown.
        /// </summary>
        /// <returns>
        /// true if the notifier could be launched;
        /// false, otherwise.
        /// </returns>
        bool Launch();

        /// <summary>
        /// Gets or sets the enabled state of the notifier.
        /// </summary>
        /// <remarks>If Enabled is set to false the notifier is no longer launched.</remarks>
        bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the type of the counted object.
        /// </summary>
        Type CounterType { get; set; }

        /// <summary>
        /// Gets or sets the name of the counter.
        /// </summary>
        string CounterName { get; set; }

        /// <summary>
        /// Gets or sets the counter count.
        /// </summary>
        int CounterCount { get; set; }

        /// <summary>
        /// Gets or sets the maximum allowed counter count.
        /// </summary>
        int CounterMaximumCount { get; set; }

        /// <summary>
        /// Gets or sets the counter enabled state.
        /// </summary>
        bool? CounterEnabled { get; set; }

        /// <summary>
        /// Gets or sets the object counter.
        /// </summary>
        ObjectCounter Counter { get; set; }
    }


    /// <summary>
    /// Delegate used to notify when the count of a specified object has
    /// exceeded the maximum allowed value.
    /// </summary>
    /// <param name="type">The type of the object.</param>
    /// <param name="name">The name of the counter.</param>
    /// <param name="count">The count of the object.</param>
    public delegate void ObjectCountExceededDelegate(Type type, string name, int count);


    /// <summary>
    /// Provides functionality for counting objects.
    /// </summary>
    public class ObjectCounter {
        #region Nested types

        /// <summary>
        /// Internal class used to track information about an object counter.
        /// </summary>
        [Serializable]
        internal class ObjectCategory {
            #region Constructor

            public ObjectCategory(string name, Type type, int maxCount) {
                if(type == null) {
                    throw new ArgumentNullException("type");
                }

                _name = name;
                _type = type;
                _maxCount = maxCount;
                _enabled = true;
            }

            public ObjectCategory(string name, Type type) {
                if(type == null) {
                    throw new ArgumentNullException("type");
                }

                _name = name;
                _type = type;
                _enabled = true;
            }

            public ObjectCategory(Type type) {
                if(type == null) {
                    throw new ArgumentNullException("type");
                }

                _name = null;
                _type = type;
                _enabled = true;
            }

            #endregion

            #region Properites

            private string _name;
            public string Name {
                get { return _name; }
                set { _name = value; }
            }

            private Type _type;
            public Type Type {
                get { return _type; }
                set { _type = value; }
            }

            private int _count;
            public int Count {
                get { return _count; }
                set {
                    if(value < 0) {
                        _count = 0;
                    }
                    else {
                        _count = value;
                    }
                }
            }

            private int _maxCount;
            public int MaximumCount {
                get { return _maxCount; }
                set { _maxCount = value; }
            }

            public bool HasName {
                get { return (_name != null && _name.Length > 0); }
            }

            private bool _enabled;
            public bool Enabled {
                get { return _enabled; }
                set { _enabled = value; }
            }

            #endregion

            #region Public methods

            public override int GetHashCode() {
                if(HasName) {
                    return _name.GetHashCode();
                }
                else {
                    return _type.GetHashCode();
                }
            }

            #endregion
        }

        #endregion

        #region Fields

        Hashtable counters;
        private object lockObject;

        /// <summary>
        /// The attached IObjectCounterNotifier object.
        /// </summary>
        public IObjectCounterNotifier ObjectCounterNotifier;

        #endregion

        #region Constructor

        public ObjectCounter() {
            counters = new Hashtable();
            lockObject = new object();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the number of defined object counters.
        /// </summary>
        public int ObjectCounters {
            get { return counters.Count; }
        }

        private bool _autoResetCounter;
        /// <summary>
        /// Specifies whether or not to automatically reset the counter
        /// after the maximum value is reached.
        /// </summary>
        public bool AutoResetCounter {
            get { return _autoResetCounter; }
            set { _autoResetCounter = value; }
        }

        #endregion

        #region Events

        /// <summary>
        /// Event raised when an object exceeds the maximum allowed count.
        /// </summary>
        public ObjectCountExceededDelegate OnObjectCountExceeded;

        #endregion

        #region Private methods

        /// <summary>
        /// Checks if the number of objects exceeds the maximum allowed number.
        /// If the <paramref name="OnObjectCountExceeded"/> event is set, the event is called.
        /// If the <paramref name="ObjectCounterNotifier"/> notifier is set, the notifier is launched.
        /// </summary>
        /// <param name="counter"></param>
        private void CheckObjectCount(ObjectCategory counter) {
            if(counter == null) {
                throw new ArgumentNullException("counter");
            }

            if(counter.MaximumCount > 0 && counter.Count > counter.MaximumCount && counter.Enabled) {
                if(OnObjectCountExceeded != null) {
                    OnObjectCountExceeded(counter.Type, counter.Name, counter.Count);
                }

                // show the notifier
                if(ObjectCounterNotifier != null && ObjectCounterNotifier.Enabled) {
                    ObjectCounterNotifier.CounterType = counter.Type;
                    ObjectCounterNotifier.CounterName = counter.Name;
                    ObjectCounterNotifier.CounterCount = counter.Count;
                    ObjectCounterNotifier.CounterMaximumCount = counter.MaximumCount;
                    ObjectCounterNotifier.Counter = this;

                    if(!ObjectCounterNotifier.Launch()) {
                        Console.WriteLine("Couldn't launch IObjectCounterNotifier {0}", 
                                          ObjectCounterNotifier.GetType().Name);
                    }

                    // check if the counter was disabled
                    if(ObjectCounterNotifier.CounterEnabled.HasValue && 
                       ObjectCounterNotifier.CounterEnabled == false) {
                        counter.Enabled = false;
                    }
                }

                if(_autoResetCounter) {
                    counter.Count = 0;
                }
            }
        }

        /// <summary>
        /// Get all the object categories that match the specified type and counterName.
        /// </summary>
        /// <param name="type">The Type of the counter.</param>
        /// <param name="counterName">The counter name.</param>
        /// <returns>An array of ObjectCategory objects.</returns>
        private ObjectCategory[] GetObjectCategories(Type type, string counterName) {
            ArrayList list = new ArrayList();

            if(type == null) {
                throw new ArgumentNullException("type");
            }

            if(counterName != null && counterName.Length > 0) {
                int hash = counterName.GetHashCode();

                if(counters.ContainsKey(hash)) {
                    return new ObjectCategory[] { (ObjectCategory)counters[hash] };
                }
            }
            else {
                int hash = type.GetHashCode();

                if(counters.ContainsKey(hash)) {
                    return new ObjectCategory[] { (ObjectCategory)counters[hash] };
                }
                else {
                    // make a list
                    foreach(DictionaryEntry de in counters) {
                        list.Add((ObjectCategory)de.Value);
                    }
                }
            }

            return (ObjectCategory[])list.ToArray(typeof(ObjectCategory));
        }

        #endregion

        #region Public methods

        #region Adding

        /// <summary>
        /// Add a object type.
        /// </summary>
        /// <param name="type">The object type. Use typeof() to get the type.</param>
        /// <param name="counterName">The name of the counter this type belongs.</param>
        /// <param name="maxCount">The maximum count of objects of this type allowed.</param>
        public void AddObjectType(Type type, string categoryName, int maxCount) {
            if(type == null) {
                throw new ArgumentNullException("type");
            }

            ObjectCategory category = new ObjectCategory(categoryName, type, maxCount);
            counters.Add(category.GetHashCode(), category);
        }


        /// <summary>
        /// Add a object type.
        /// </summary>
        /// <param name="type">The object type. Use typeof() to get the type.</param>
        /// <param name="counterName">The name of the counter this type belongs.</param>
        public void AddObjectType(Type type, string categoryName) {
            AddObjectType(type, categoryName, 0);
        }


        /// <summary>
        /// Add a object type.
        /// </summary>
        /// <param name="type">The object type. Use typeof() to get the type.</param>
        /// <param name="maxCount">The maximum count of objects of this type allowed.</param>
        public void AddObjectType(Type type, int maxCount) {
            AddObjectType(type, null, maxCount);
        }


        /// <summary>
        /// Add a object type.
        /// </summary>
        /// <param name="type">The object type. Use typeof() to get the type.</param>
        public void AddObjectType(Type type) {
            AddObjectType(type, null, 0);
        }

        #endregion

        #region Increment / decrement

        /// <summary>
        /// Increment the count of the object
        /// </summary>
        /// <param name="type">The object type. Use typeof() to get the type.</param>
        /// <param name="counterName">The name of the counter this type belongs.</param>
        public void IncrementObjectCount(object obj, string counterName) {
            lock(lockObject) {
                if(obj == null) {
                    throw new ArgumentNullException("obj");
                }

                ObjectCategory[] categories = GetObjectCategories(obj.GetType(), counterName);

                foreach(ObjectCategory category in categories) {
                    if(category.Enabled) {
                        category.Count++;
                        CheckObjectCount(category);
                    }
                }
            }
        }


        /// <summary>
        /// Increment the count of the object
        /// </summary>
        /// <param name="type">The object type. Use typeof() to get the type.</param>
        public void IncrementObjectCount(object obj) {
            IncrementObjectCount(obj, null);
        }


        /// <summary>
        /// Decrement the count of the object
        /// </summary>
        /// <param name="type">The object type. Use typeof() to get the type.</param>
        /// <param name="counterName">The name of the counter this type belongs.</param>
        public void DecrementObjectCount(object obj, string counterName) {
            lock(lockObject) {
                if(obj == null) {
                    throw new ArgumentNullException("obj");
                }

                ObjectCategory[] categories = GetObjectCategories(obj.GetType(), counterName);

                foreach(ObjectCategory category in categories) {
                    if(category.Enabled) {
                        category.Count--;
                    }
                }
            }
        }


        /// <summary>
        /// Decrement the count of the object
        /// </summary>
        /// <param name="type">The object type. Use typeof() to get the type.</param>
        public void DecrementObjectCount(object obj) {
            DecrementObjectCount(obj, null);
        }

        #endregion

        #region Object count

        /// <summary>
        /// Get the object count
        /// </summary>
        /// <param name="type">The object type. Use typeof() to get the type.</param>
        /// <param name="counterName">The name of the counter this type belongs.</param>
        /// <returns>NULL if the specified type was not fount.</returns>
        public int? GetObjectCount(Type type, string counterName) {
            if(type == null) {
                throw new ArgumentNullException("type");
            }

            int hash = counterName != null ? 
                       counterName.GetHashCode() : type.GetHashCode();

            if(counters.ContainsKey(hash)) {
                return ((ObjectCategory)counters[hash]).Count;
            }

            return null;
        }


        /// <summary>
        /// Get the object count
        /// </summary>
        /// <param name="type">The object type. Use typeof() to get the type.</param>
        /// <returns>NULL if the specified type was not fount.</returns>
        public int? GetObjectCount(Type type) {
            return GetObjectCount(type, null);
        }


        /// <summary>
        /// Reset the object count
        /// </summary>
        /// <param name="type">The object type. Use typeof() to get the type.</param>
        /// <param name="counterName">The name of the counter this type belongs.</param>
        public void ResetObjectCount(Type type, string counterName) {
            lock(lockObject) {
                if(type == null) {
                    throw new ArgumentNullException("type");
                }

                ObjectCategory[] categories = GetObjectCategories(type, counterName);

                foreach(ObjectCategory category in categories) {
                    category.Count = 0;
                }
            }
        }


        /// <summary>
        /// Get the object count
        /// </summary>
        /// <param name="type">The object type. Use typeof() to get the type.</param>
        public void ResetObjectCount(Type type) {
            ResetObjectCount(type, null);
        }

        #endregion

        #region Object counter state

        /// <summary>
        /// Set the state of the object
        /// </summary>
        /// <param name="type">The object type. Use typeof() to get the type.</param>
        /// <param name="counterName">The name of the counter this type belongs.</param>
        /// <param name="state">The state of the object.</param>
        public void SetObjectCounterState(Type type, string counterName, bool state) {
            lock(lockObject) {
                if(type == null) {
                    throw new ArgumentNullException("type");
                }

                ObjectCategory[] categories = GetObjectCategories(type, counterName);

                foreach(ObjectCategory category in categories) {
                    category.Enabled = state;
                }
            }
        }


        /// <summary>
        /// Set the state of the object
        /// </summary>
        /// <param name="type">The object type. Use typeof() to get the type.</param>
        /// <param name="state">The state of the object.</param>
        public void SetObjectCounterState(Type type, bool state) {
            SetObjectCounterState(type, null, state);
        }


        /// <summary>
        /// Get the state of the object
        /// </summary>
        /// <param name="type">The object type. Use typeof() to get the type.</param>
        /// <param name="counterName">The name of the counter this type belongs.</param>
        /// <returns>
        /// NULL if the specified type was not found;
        /// The state of the counter, otherwise.
        /// </returns>
        public bool? GetObjectCounterState(Type type, string counterName) {
            if(type == null) {
                throw new ArgumentNullException("type");
            }

            int hash = counterName != null ? 
                       counterName.GetHashCode() : type.GetHashCode();

            if(counters.ContainsKey(hash)) {
                return ((ObjectCategory)counters[hash]).Enabled;
            }

            return null;
        }


        /// <summary>
        /// Get the state of the object
        /// </summary>
        /// <param name="type">The object type. Use typeof() to get the type.</param>
        /// <returns>
        /// NULL if the specified type was not found;
        /// The state of the counter, otherwise.
        /// </returns>
        public bool? GetObjectCounterState(Type type) {
            return GetObjectCounterState(type, null);
        }

        #endregion

        #region Maximum count

        /// <summary>
        /// Set the maximum allowed number of objects
        /// </summary>
        /// <param name="type">The object type. Use typeof() to get the type.</param>
        /// <param name="counterName">The name of the counter this type belongs.</param>
        /// <param name="maxCount">The maximum count of objects of this type allowed.</param>
        public void SetObjectMaximumCount(Type type, string categoryName, int maxCount) {
            lock(lockObject) {
                if(type == null) {
                    throw new ArgumentNullException("type");
                }

                int hash = categoryName != null ? 
                           categoryName.GetHashCode() : type.GetHashCode();

                if(counters.ContainsKey(hash)) {
                    ((ObjectCategory)counters[hash]).MaximumCount = maxCount;
                }
            }
        }


        /// <summary>
        /// Set the maximum allowed number of objects
        /// </summary>
        /// <param name="type">The object type. Use typeof() to get the type.</param>
        /// <param name="maxCount">The maximum count of objects of this type allowed.</param>
        public void SetObjectMaximumCount(Type type, int maxCount) {
            SetObjectMaximumCount(type, null, maxCount);
        }


        /// <summary>
        /// Get the maximum allowed number of objects
        /// </summary>
        /// <param name="type">The object type. Use typeof() to get the type.</param>
        /// <param name="counterName">The name of the counter this type belongs.</param>
        public int GetObjectMaximumCount(Type type, string categoryName) {
            if(type == null) {
                throw new ArgumentNullException("type");
            }

            int hash = categoryName != null ? 
                       categoryName.GetHashCode() : type.GetHashCode();

            if(counters.ContainsKey(hash)) {
                return ((ObjectCategory)counters[hash]).MaximumCount;
            }

            return 0;
        }


        /// <summary>
        /// Get the maximum allowed number of objects
        /// </summary>
        /// <param name="type">The object type. Use typeof() to get the type.</param>
        public int GetObjectMaximumCount(Type type) {
            return GetObjectMaximumCount(type, null);
        }

        #endregion

        #region Summary

        /// <summary>
        /// Generate a summary about all object counters.
        /// </summary>
        /// <returns>The summary string.</returns>
        public string GenerateSummary() {
            StringBuilder builder = new StringBuilder();
            int ct = 0;

            builder.Append("Object count summary\r\n\r\n");

            foreach(DictionaryEntry de in counters) {
                ObjectCategory category = (ObjectCategory)de.Value;

                builder.Append(category.Type.FullName);
                builder.Append(": ");
                builder.Append(category.Count.ToString());

                if(category.HasName) {
                    builder.Append(" (");
                    builder.Append(category.Name);
                    builder.Append(")");
                }

                builder.AppendLine();
                ct++;
            }

            builder.Append("\r\nCounted types: ");
            builder.Append(ct.ToString());

            return builder.ToString();
        }


        /// <summary>
        /// Save a summary about all the object counter (text File)
        /// </summary>
        /// <param name="path">The location where to save the summary.</param>
        /// <returns>
        /// true if the summary could be saved;
        /// false, otherwise.
        /// </returns>
        public bool SaveSummary(string path) {
            if(path == null) {
                throw new ArgumentNullException("path");
            }

            StreamWriter writer = null;

            try {
                writer = new StreamWriter(path);

                if(writer.BaseStream.CanWrite) {
                    writer.Write(GenerateSummary());
                    return true;
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

            return false;
        }

        #endregion

        #region Html summary

        /// <summary>
        /// Generate a report in HTML format about the stored object counters
        /// </summary>
        /// <param name="path">The location where to save the report.</param>
        /// <param name="title">The title of the report.</param>
        /// <param name="open">Specifies whether or not to open the report in the default browser.</param>
        /// <returns>
        /// true of the report coud pe generated;
        /// false, otherwise.
        /// </returns>
        public bool GenerateHtmlReport(string path, string title, bool open) {
            if(path == null) {
                throw new ArgumentNullException("path");
            }

            StreamWriter writer = new StreamWriter(path);

            if(!writer.BaseStream.CanWrite) {
                return false;
            }

            // write the html code (old style, don't uses CSS)
            writer.WriteLine("<html>");
            writer.WriteLine("<head>");
            writer.WriteLine("<title>" + (title == null ? "Object counter summary" : title) + "</title>");
            writer.WriteLine("</head>");
            writer.WriteLine("<body bgcolor=\"#FFFFFF\" text=\"black\" link=\"#525674\" vlink=\"purple\" alink=\"red\">");
            writer.WriteLine("<p><font face=\"Arial\" size=\"5\" color=\"#525674\"><b>" + (title == null ? "Object counter summary" : title) +
                             "</b></font><br><font face=\"Arial\" size=\"4\" color=\"#525674\">Summary created on: " +
                             DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString() + "</font><font color=\"#525674\"><br></font></p>");

            if(counters != null && counters.Count > 0) {
                writer.WriteLine("<table border=\"1\" cellspacing=\"0\" width=\"100%\" bordercolordark=\"white\" bordercolorlight=\"black\">");
                writer.WriteLine("<tr bgcolor=\"#525674\">");
                writer.WriteLine("<td align=\"center\" width=\"2%\"><p><font face=\"Arial\" size=\"2\" color=\"white\"><b>ID</b></font></p></td>");
                writer.WriteLine("<td align=\"center\" width=\"24%\"><p><font face=\"Arial\" size=\"2\" color=\"white\"><b>Name</b></font></p></td>");
                writer.WriteLine("<td align=\"right\" width=\"24%\"><p><font face=\"Arial\" size=\"2\" color=\"white\"><b>Type&nbsp</b></font></p></td>");
                writer.WriteLine("<td width=\"78%\"><p><font face=\"Arial\" size=\"2\" color=\"white\"><b>&nbspCount</b></font></p></td>");
                writer.WriteLine("</tr>\n");

                int ct = 0;
                foreach(DictionaryEntry de in counters) {
                    ObjectCategory category = (ObjectCategory)de.Value;

                    writer.WriteLine((ct % 2 == 0) ? "<tr bgcolor=\"FFFFFF\">" : "<tr bgcolor=\"#E9E9F3\">");

                    writer.Write("<td align=\"center\"><p><font face=\"Arial\" size=\"2\" color=\"black\">");
                    writer.Write(((int)(ct + 1)).ToString());
                    writer.WriteLine("</font></p></td>");

                    writer.Write("<td align=\"center\"><p><font face=\"Arial\" size=\"2\" color=\"black\">");
                    writer.Write(category.HasName ? category.Name : "&nbsp");

                    writer.WriteLine("</font></p></td>");

                    writer.Write("<td align=\"right\"><p><font face=\"Arial\" size=\"2\" color=\"black\">");
                    writer.Write(category.Type.FullName + "&nbsp");
                    writer.WriteLine("</font></p></td>");

                    writer.Write("<td><p><font face=\"Arial\" size=\"2\" color=\"black\">");
                    writer.Write("&nbsp" + category.Count.ToString());
                    writer.WriteLine("</font></p></td>");
                    writer.WriteLine("</tr>\n");

                    ct++;
                }

                writer.WriteLine("</table>");
            }

            writer.WriteLine("<p><font face=\"Arial\" size=\"3\" color=\"#525674\">Total counters: " + this.ObjectCounters.ToString() + "<br></font></p>");

            writer.WriteLine("<hr noshade color=\"#525674\">");
            writer.WriteLine("<font face=\"Arial\" color=\"#525674\"><span style=\"font-size:9pt;\">Generated by Debug Library v.1.0 &nbsp&nbsp| &nbsp&nbspCopyright &copy 2007 <a href=\"mailto:lgratian@gmail.com\">Lup Gratian</a></span></font><span style=\"font-size:9pt;\">");
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
        /// Generate a report in HTML format about the stored object counters
        /// </summary>
        /// <remarks>The report will be saved in the temporary directory and opened automatically in the default browser.</remarks>
        /// <returns>
        /// true of the report could be generated;
        /// false, otherwise.
        /// </returns>
        public bool GenerateHtmlReport() {
            // save to a temporary File
            string filePath = Path.GetTempPath() + Path.GetRandomFileName() + ".htm";

            return GenerateHtmlReport(filePath, null, true);
        }

        #endregion

        #endregion
    }
}
