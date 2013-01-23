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
using System.Diagnostics;
using System.Xml.Serialization;
using System.Collections.Specialized;
using System.Threading;

namespace DebugUtils.Debugger {
    /// <summary>
    /// Interface that needs to be used by all iteration notifier objects.
    /// </summary>
    public interface IPerformanceIterationNotifier {
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
        /// The event the notifier should handle.
        /// </summary>
        PerformanceEvent Event { get; set; }

        /// <summary>
        /// The iteration the notifier should handle.
        /// </summary>
        PerformanceIteration Iteration { get; set; }
    }


    /// <summary>
    /// Provides information about an iteration.
    /// </summary>
    [Serializable]
    [DebuggerDisplay("Duration = {Duration}")]
    public class PerformanceIteration {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private long _startTime;
        /// <summary>
        /// Gets or sets the time at which the iteration started.
        /// </summary>
        public long StartTime {
            get { return _startTime; }
            set { _startTime = value; }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private long _endTime;
        /// <summary>
        /// Gets or sets the time at which the iteration ended.
        /// </summary>
        public long EndTime {
            get { return _endTime; }
            set { _endTime = value; }
        }

        /// <summary>
        /// Gets the duration of the iteration.
        /// </summary>
        public TimeSpan Duration {
            get { return TimeSpan.FromMilliseconds(_endTime - _startTime); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _number;
        /// <summary>
        /// Gets or sets the number of the iteration.
        /// </summary>
        public int Number {
            get { return _number; }
            set { _number = value; }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _hitCount;
        /// <summary>
        /// Gets or sets the number of hits.
        /// </summary>
        public int HitCount {
            get { return _hitCount; }
            set { _hitCount = value; }
        }

        /// <summary>
        /// Gets the number of hits per second.
        /// </summary>
        /// <returns>
        /// The number of hits per seconds, if HitCount > 0;
        /// NaN, otherwise.
        /// </returns>
        public double HitsPerSecond {
            get {
                if(_hitCount <= 0) {
                    return double.NaN;
                }
                else {
                    return ((double)_hitCount * 1000.0) / (double)Duration.TotalMilliseconds;
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private long _startWorkingSet;
        /// <summary>
        /// Gets or sets the amount of available memory when the iteration was started.
        /// </summary>
        public long StartWorkingSet {
            get { return _startWorkingSet; }
            set { _startWorkingSet = value; }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private long _endWorkingSet;
        /// <summary>
        /// Gets or sets the amount of available memory when the iteration was stopped.
        /// </summary>
        public long EndWorkingSet {
            get { return _endWorkingSet; }
            set { _endWorkingSet = value; }
        }

        /// <summary>
        /// Gets the amount of memory used during the iteration.
        /// </summary>
        public long DeltaWorkingSet {
            get {
                return _endWorkingSet - _startWorkingSet;
            }
        }
    }


    /// <summary>
    /// Delegate used by performance iterations with a timer attached
    /// </summary>
    /// <param name="perfEvent">The event that generated the call.</param>
    /// <param name="iteration">The active iteration when the call was made.</param>
    public delegate void TimeEllapsedDelegate(PerformanceEvent perfEvent, PerformanceIteration iteration);


    /// <summary>
    /// Provides information about a performance event.
    /// </summary>
    [Serializable]
    public class PerformanceEvent {
        #region Fields

        [NonSerialized]
        private object lockObject = new object();

        [NonSerialized]
        private Stopwatch watch;
        private List<PerformanceIteration> iterations;
        private PerformanceIteration activeIteration;
        [NonSerialized]
        private Timer timer;
        private event TimeEllapsedDelegate timerCompletedEvent;
        private bool timing;
        [NonSerialized]
        private ManualResetEvent waitEvent;

        #endregion

        #region Properties

        private string _name;
        /// <summary>
        /// Gets or sets the name of the event.
        /// </summary>
        public string Name {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// Specifies whether or not the event is started.
        /// </summary>
        public bool IsStarted {
            get { return watch.IsRunning; }
        }

        private TimeSpan _maxTime;
        /// <summary>
        /// Gets or sets the the maximum allowed time a event can take.
        /// </summary>
        public TimeSpan MaximumTime {
            get { return _maxTime; }
            set { _maxTime = value; }
        }

        private bool _reportTimeExceedingToDebugger;
        /// <summary>
        /// Specifies whether or not the debugger should be informed when
        /// an event takes more than the maximum allowed time.
        /// </summary>
        public bool ReportTimeExceedingToDebugger {
            get { return _reportTimeExceedingToDebugger; }
            set { _reportTimeExceedingToDebugger = value; }
        }

        /// <summary>
        /// Gets the number of iterations.
        /// </summary>
        public int IterationCount {
            get { return iterations.Count; }
        }

        /// <summary>
        /// Gets the iterations.
        /// </summary>
        public List<PerformanceIteration> Iterations {
            get { return iterations; }
        }

        private PerformanceManager _manager;
        [XmlIgnore]
        public PerformanceManager Manager {
            get { return _manager; }
            set { _manager = value; }
        }

        /// <summary>
        /// The average of all iteration durations
        /// </summary>
        /// <returns>
        /// The average duration if IterationCount > 0;
        /// null, otherwise.
        /// </returns>
        public TimeSpan? AverageDuration {
            get {
                lock(lockObject) {

                    TimeSpan total = TimeSpan.FromMilliseconds(0);

                    foreach(PerformanceIteration pi in iterations) {
                        total += pi.Duration;
                    }

                    if(iterations.Count != 0) {
                        return TimeSpan.FromMilliseconds(total.TotalMilliseconds / iterations.Count);
                    }
                    else {
                        return null;
                    }
                }
            }
        }

        #endregion

        #region Constructor

        private void InitPerformanceEvent() {
            watch = new Stopwatch();
            iterations = new List<PerformanceIteration>();
            waitEvent = new ManualResetEvent(false);
            _maxTime = TimeSpan.MaxValue;
        }


        public PerformanceEvent() {
            InitPerformanceEvent();
        }

        /// <param name="name">The name of the event.</param>
        public PerformanceEvent(string name) {
            if(name == null) {
                throw new ArgumentNullException("name");
            }

            _name = name;
            InitPerformanceEvent();
        }

        #endregion

        #region Events

        /// <summary>
        /// Event called when the maximum allowed execution time is exceeded.
        /// </summary>
        public event TimeEllapsedDelegate OnMaximumTimeExceeded;

        #endregion

        #region Private methods

        private void TimerCallback(object stateInfo) {
            if(timing == false) {
                return;
            }

            Stop();
            timing = false;

            if(timerCompletedEvent != null) {
                timerCompletedEvent(this, activeIteration);
            }
            else {
                TimeEllapsedDelegate temp = OnMaximumTimeExceeded;

                if(temp != null) {
                    temp(this, activeIteration);
                }
            }

            if(_manager != null) {
                if(_manager.IterationNotifier != null) {
                    _manager.IterationNotifier.Event = this;
                    _manager.IterationNotifier.Iteration = activeIteration;

                    if(_manager.IterationNotifier.Launch() == false) {
                        Console.WriteLine("Couldn't launch IIterationNotifier {0}", _manager.IterationNotifier.GetType().Name);
                    }
                }
            }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Start an iteration.
        /// </summary>
        public void Start() {
            lock(lockObject) {

                // stop the active iteration
                if(activeIteration != null && watch.IsRunning) {
                    Stop();
                }

                activeIteration = new PerformanceIteration();
                activeIteration.HitCount = 0;
                activeIteration.StartWorkingSet = GC.GetTotalMemory(false);
                watch.Reset();
                watch.Start();
                waitEvent.Reset();
            }
        }


        /// <summary>
        /// Stop the currently running iteration.
        /// </summary>
        public void Stop() {
            lock(lockObject) {
                if(activeIteration != null && watch.IsRunning) {
                    watch.Stop();
                    activeIteration.StartTime = (long)(new TimeSpan(DateTime.Now.Ticks).TotalMilliseconds) - watch.ElapsedMilliseconds;
                    activeIteration.EndTime = activeIteration.StartTime + watch.ElapsedMilliseconds;
                    activeIteration.EndWorkingSet = GC.GetTotalMemory(false);

                    // add the iteration to the list
                    activeIteration.Number = iterations.Count;
                    iterations.Add(activeIteration);

                    if(activeIteration.Duration > _maxTime) {
                        // call event
                        if(OnMaximumTimeExceeded != null) {
                            OnMaximumTimeExceeded(this, null);
                        }

                        // report to debugger
                        if(_reportTimeExceedingToDebugger) {
                            Debug.ReportWarning("Operation {0} (duration = {1}) exceeded maximum time of {2}",
                                                _name, activeIteration.Duration.TotalMilliseconds.ToString(),
                                                _maxTime.TotalMilliseconds.ToString());
                        }
                    }

                    waitEvent.Set();

                    if(Manager != null && _manager.IterationNotifier != null && _manager.IterationNotifier.Enabled) {
                        _manager.IterationNotifier.Event = this;
                        _manager.IterationNotifier.Iteration = activeIteration;
                        _manager.IterationNotifier.Launch();
                    }
                }
            }
        }


        /// <summary>
        /// Increase the hit count of the running iteration.
        /// </summary>
        public void Hit() {
            if(activeIteration != null && watch.IsRunning) {
                activeIteration.HitCount++;
            }
        }


        /// <summary>
        /// Start an iteration with an exact duration.
        /// </summary>
        /// <param name="duration">The duration to monitor the hit count.</param>
        /// <param name="onTimeEllapsed">The method to call when the specified time elapses.</param>
        public void StartTimed(TimeSpan duration, TimeEllapsedDelegate onTimeEllapsed) {
            lock(lockObject) {
                try {
                    timerCompletedEvent = onTimeEllapsed;
                    timing = true;
                    Start();
                    timer = new Timer(TimerCallback, null, duration, TimeSpan.FromSeconds(0));
                }
                catch(Exception e) {
                    Console.WriteLine(e.Message);
                }
            }
        }


        /// <summary>
        /// Start an iteration with an exact duration.
        /// </summary>
        /// <param name="duration">The duration to monitor the hit count.</param>
        public void StartTimed(TimeSpan duration) {
            StartTimed(duration, null);
        }


        /// <summary>
        /// Stop monitoring the active performance iteration.
        /// </summary>
        public void StopTimer() {
            lock(lockObject) {
                if(timing) {
                    timer.Change(-1, -1);
                    timer.Dispose();
                    timer = null;
                    TimerCallback(null);
                }
            }
        }


        /// <summary>
        /// Wait until the event is stopped
        /// </summary>
        public void Wait() {
            waitEvent.WaitOne();
        }

        #endregion
    }


    /// <summary>
    /// Provides functionality for monitoring the performance of the application.
    /// </summary>
    public class PerformanceManager {
        #region Fields

        private Dictionary<string, PerformanceEvent> _events;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the number of the _events.
        /// </summary>
        public int EventCount {
            get { return _events.Count; }
        }

        private string _performanceViewerPath;
        /// <summary>
        /// Gets or sets the path of the attached viewer path.
        /// </summary>
        public string PerformanceViewerPath {
            get { return _performanceViewerPath; }
            set { _performanceViewerPath = value; }
        }

        private string _performanceViewerEnvironmentVariable;
        /// <summary>
        /// Gets or sets the environment variable used to locate the viewer application.
        /// </summary>
        /// <remarks>The vatiable is used only if <paramref name="PerformanceViewerPath"/> is not set.</remarks>
        public string PerformanceViewerEnvironmentVariable {
            get { return _performanceViewerEnvironmentVariable; }
            set { _performanceViewerEnvironmentVariable = value; }
        }

        private IPerformanceIterationNotifier _iterationNotifier;
        /// <summary>
        /// Gets or sets the notifier.
        /// </summary>
        public IPerformanceIterationNotifier IterationNotifier {
            get { return _iterationNotifier; }
            set { _iterationNotifier = value; }
        }

        public Dictionary<string, PerformanceEvent> Events {
            get { return _events; }
            set { _events = value; }
        }

        #endregion

        #region Constructor

        public PerformanceManager() {
            _events = new Dictionary<string, PerformanceEvent>();
        }

        #endregion

        #region Indexers

        public PerformanceEvent this[string name] {
            get { return GetEvent(name); }
        }

        #endregion

        #region Private methods

        private bool IsValidKey(string key) {
            if(key == null || key.Length == 0) {
                return false;
            }

            return _events.ContainsKey(key);
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Add a new performance event
        /// </summary>
        /// <param name="name">The name of the performance event.</param>
        /// <param name="start">Specifies whether or not to start the event immediately.</param>
        /// <returns>The newly added event.</returns>
        public PerformanceEvent AddEvent(string name, bool start) {
            if(name == null) {
                throw new ArgumentNullException("name");
            }

            PerformanceEvent e = new PerformanceEvent();
            e.Name = name;
            e.Manager = this;
            _events.Add(name, e);

            if(start) {
                e.Start();
            }

            return e;
        }


        /// <summary>
        /// Add a new performance event
        /// </summary>
        /// <param name="name">The name of the performance event.</param>
        /// <returns>The newly added event.</returns>
        public PerformanceEvent AddEvent(string name) {
            return AddEvent(name, false);
        }


        /// <summary>
        /// Get the event by its name
        /// </summary>
        /// <param name="name">The name of the performance event.</param>
        public PerformanceEvent GetEvent(string name) {
            if(IsValidKey(name) == false) {
                return null;
            }

            return _events[name];
        }


        /// <summary>
        /// Remove the event
        /// </summary>
        /// <param name="name">The name of the performance event.</param>
        public bool RemoveEvent(string name) {
            if(IsValidKey(name) == false) {
                return false;
            }

            _events.Remove(name);
            return true;
        }

        /// <summary>
        /// Start the event.
        /// </summary>
        /// <param name="name">The name of the performance event.</param>
        public void StartEvent(string name) {
            if(IsValidKey(name) == false) {
                return;
            }

            _events[name].Start();
        }


        /// <summary>
        /// Start the event with a timer attached
        /// </summary>
        /// <param name="name">The name of the performance event.</param>
        /// <param name="duration">The time to monitor the hit count.</param>
        /// <param name="onTimeEllapsed">The Method to call when the specified time elapses.</param>
        public void StartTimedEvent(string name, TimeSpan duration, TimeEllapsedDelegate onTimeEllapsed) {
            if(IsValidKey(name) == false) {
                return;
            }

            _events[name].StartTimed(duration, onTimeEllapsed);
        }


        /// <summary>
        /// Start the event with a timer attached
        /// </summary>
        /// <param name="name">The name of the performance event.</param>
        /// <param name="duration">The time to monitor the hit count.</param>
        public void StartTimedEvent(string name, TimeSpan duration) {
            StartTimedEvent(name, duration, null);
        }


        /// <summary>
        /// Stop the event.
        /// </summary>
        /// <param name="name">The name of the performance event.</param>
        /// <returns>
        /// The duration of the last iteration if the event was found;
        /// TimeSpan.MaxValue, otherwise.
        /// </returns>
        public TimeSpan StopEvent(string name) {
            if(IsValidKey(name) == false) {
                return TimeSpan.MaxValue;
            }

            _events[name].Stop();
            return _events[name].Iterations[_events[name].IterationCount - 1].Duration;
        }


        /// <summary>
        /// Increase the hit count of the running iteration for the given performance event
        /// </summary>
        /// <param name="name">The name of the performance event.</param>
        public void IncreaseHitCount(string name) {
            if(IsValidKey(name) == false) {
                return;
            }

            _events[name].Hit();
        }


        /// <summary>
        /// Serialize all _events
        /// </summary>
        /// <param name="path">The path where to save the serialized _events.</param>
        /// <returns>
        /// true if the _events could be serialized;
        /// false, otherwise.
        /// </returns>
        public bool SerializeEvents(string path) {
            if(path == null) {
                throw new ArgumentNullException("path");
            }

            if(path.Length == 0) {
                return false;
            }

            StreamWriter writer = null;

            try {
                // convert the dictionary to list
                List<PerformanceEvent> list = new List<PerformanceEvent>();

                foreach(KeyValuePair<string, PerformanceEvent> kvp in _events) {
                    list.Add(kvp.Value);
                }

                XmlSerializer serializer = new XmlSerializer(typeof(List<PerformanceEvent>));
                writer = new StreamWriter(path);

                if(writer.BaseStream.CanWrite) {
                    serializer.Serialize(writer, list);
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
        /// Launch the attached performance viewer
        /// </summary>
        /// /// <returns>
        /// true if the viewer could be launched;
        /// false, otherwise.
        /// </returns>
        public bool LaunchPerformanceViewer() {
            string viewerPath = _performanceViewerPath;

            if(viewerPath == null && _performanceViewerEnvironmentVariable != null) {
                viewerPath = Environment.GetEnvironmentVariable(_performanceViewerEnvironmentVariable);
            }

            if(viewerPath == null && File.Exists(viewerPath) == false) {
                return false;
            }

            // save to a temporary File
            string filePath = Path.GetTempPath() + Path.GetRandomFileName() + ".xml";

            if(SerializeEvents(filePath)) {
                try {
                    Process.Start(viewerPath, filePath);
                    return true;
                }
                catch(Exception e) {
                    Console.WriteLine(e.Message);
                    return false;
                }
            }

            return false;
        }

        #region Html summary

        /// <summary>
        /// Generate a report in HTML format about the stored object counters
        /// </summary>
        /// <param name="path">The location where to save the report.</param>
        /// <param name="title">The title of the report.</param>
        /// <param name="open">Specifies whether or not to open the report in the default browser.</param>
        /// <returns>
        /// true if the report could be generated;
        /// false, otherwise.
        /// </returns>
        public bool GenerateHtmlSummary(string path, string title, bool open) {
            if(path == null) {
                throw new ArgumentNullException("path");
            }

            StreamWriter writer = new StreamWriter(path);

            if(writer.BaseStream.CanWrite == false) {
                return false;
            }

            int[] typeCount = new int[3];
            typeCount[0] = typeCount[1] = typeCount[2] = 0;

            writer.WriteLine("<html>");
            writer.WriteLine("<head>");
            writer.WriteLine("<title>" + (title == null ? "Performance manager summary" : title) + "</title>");
            writer.WriteLine("</head>");
            writer.WriteLine("<body bgcolor=\"#FFFFFF\" text=\"black\" link=\"#525674\" vlink=\"purple\" alink=\"red\">");
            writer.WriteLine("<p><font face=\"Arial\" size=\"5\" color=\"#525674\"><b>" + (title == null ? "Performance manager summary" : title) +
                             "</b></font><br><font face=\"Arial\" size=\"4\" color=\"#525674\">Summary created on: " +
                             DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString() + "</font><font color=\"#525674\"><br></font></p>");

            if(_events != null && _events.Count > 0) {
                foreach(KeyValuePair<string, PerformanceEvent> kvp in _events) {
                    PerformanceEvent e = kvp.Value;

                    writer.Write("<p><font face=\"Arial\" size=\"3\" color=\"#525674\">Event name: " + e.Name + "<br>");
                    writer.Write("Maximum time: " + (e.MaximumTime == TimeSpan.MaxValue ? "Not set" : (e.MaximumTime.TotalSeconds.ToString() + " sec")) + "<br>");
                    writer.WriteLine("Average duration: " + (e.AverageDuration.HasValue ? (e.AverageDuration.Value.TotalSeconds.ToString() + " sec") : "Not available") + "</font></p>");

                    if(e.IterationCount > 0) {
                        // create a table for the iterations
                        writer.WriteLine("<table border=\"1\" cellspacing=\"0\" width=\"640\" bordercolordark=\"white\" bordercolorlight=\"black\">");
                        writer.WriteLine("<tr bgcolor=\"#525674\">");

                        writer.WriteLine("<td align=\"center\" width=\"4%\"><p><font face=\"Arial\" size=\"2\" color=\"white\"><b>ID</b></font></p></td>");
                        writer.WriteLine("<td align=\"center\" width=\"24%\"><p><font face=\"Arial\" size=\"2\" color=\"white\"><b>Duration</b></font></p></td>");
                        writer.WriteLine("<td align=\"center\" width=\"24%\"><p><font face=\"Arial\" size=\"2\" color=\"white\"><b>Hit count</b></font></p></td>");
                        writer.WriteLine("<td align=\"center\" width=\"24%\"><p><font face=\"Arial\" size=\"2\" color=\"white\"><b>Hits/sec</b></font></p></td>");
                        writer.WriteLine("<td align=\"center\" width=\"24%\"><p><font face=\"Arial\" size=\"2\" color=\"white\"><b>Delta working set</b></font></p></td>");
                        writer.WriteLine("</tr>\n");

                        int ct = 0;

                        foreach(PerformanceIteration pi in e.Iterations) {
                            writer.WriteLine((ct % 2 == 0) ? "<tr bgcolor=\"FFFFFF\">" : "<tr bgcolor=\"#E9E9F3\">");

                            writer.Write("<td align=\"center\"><p><font face=\"Arial\" size=\"2\" color=\"black\">");
                            writer.Write(((int)(ct + 1)).ToString());
                            writer.WriteLine("</font></p></td>");

                            writer.Write("<td align=\"center\"><p><font face=\"Arial\" size=\"2\" color=\"black\">");
                            writer.Write(string.Format("{0:F3}", pi.Duration.TotalSeconds) + " sec");
                            writer.WriteLine("</font></p></td>");

                            writer.Write("<td align=\"center\"><p><font face=\"Arial\" size=\"2\" color=\"black\">");
                            writer.Write(pi.HitCount.ToString());
                            writer.WriteLine("</font></p></td>");

                            writer.Write("<td align=\"center\"><p><font face=\"Arial\" size=\"2\" color=\"black\">");
                            writer.Write((double.IsNaN(pi.HitsPerSecond) ? "Not available" : string.Format("{0:F2}", pi.HitsPerSecond)));
                            writer.WriteLine("</font></p></td>");

                            writer.Write("<td align=\"center\"><p><font face=\"Arial\" size=\"2\" color=\"black\">");
                            writer.Write(pi.DeltaWorkingSet.ToString());
                            writer.WriteLine("</font></p></td>");
                            writer.WriteLine("</tr>\n");

                            ct++;
                        }

                        writer.WriteLine("</table>");
                        writer.WriteLine("<br><hr color=\"#6E707F\">");
                    }
                    else {
                        writer.Write("<p><font face=\"Arial\" size=\"3\" color=\"#525674\">No iterations performed.</font></p>");
                        writer.WriteLine("<hr color=\"#6E707F\">");
                    }
                }
            }

            writer.WriteLine("<p><font face=\"Arial\" size=\"3\" color=\"#525674\">Total events: " + this.EventCount.ToString() + "<br></font></p>");
            writer.WriteLine("<hr noshade color=\"#525674\">");
            writer.WriteLine("<font face=\"Arial\" color=\"#525674\"><span style=\"font-size:9pt;\">Generated by Debug Library v.1.0 &nbsp| &nbsp Copyright &copy 2007 <a href=\"mailto:lgratian@gmail.com\">Lup Gratian</a></span></font><span style=\"font-size:9pt;\">");
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
        /// true if the report could be generated;
        /// false, otherwise.
        /// </returns>
        public bool GenerateHtmlSummary() {
            // save to a temporary File
            string filePath = Path.GetTempPath() + Path.GetRandomFileName() + ".htm";
            return GenerateHtmlSummary(filePath, null, true);
        }

        #endregion

        #endregion
    }
}
