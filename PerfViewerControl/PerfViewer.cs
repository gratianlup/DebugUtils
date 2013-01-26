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
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DebugUtils.Debugger;
using System.IO;
using System.Xml.Serialization;
using System.Windows.Forms.VisualStyles;

namespace PerfViewerControl {
    public partial class PerfViewer : UserControl {
        private enum FlagType {
            None,
            Red,
            Yellow,
            Green,
            Purple,
            Pink,
            Blue
        }

        private class ContextMenuHelper {
            public ListView ParentListView;
            public ListViewItem Item;
            public ListViewItem.ListViewSubItem SubItem;
            public Object Data;

            public ContextMenuHelper(ListView parent, ListViewItem item,
                                     ListViewItem.ListViewSubItem subitem, object data) {
                ParentListView = parent;
                Item = item;
                SubItem = subitem;
                Data = data;
            }
        }

        private const double MinZoom = 0.25;
        private const double DefaultZoom = 1.00;
        private const int DefaultLeftMargin = 8;
        private const int DefaultBulletSize = 6;
        private const int DefaultIterationDistance = 48;
        private Color DefaultGraphColor = Color.Black;
        private Color InfoZoneBackColor = Color.LightGray;
        private const int InfoZoneHeight = 14;

        private List<PerformanceEvent> performanceData;
        private Dictionary<PerformanceIteration, string> iterationFlagMappings;
        private List<PerformanceEvent> eventGraph;
        private Dictionary<PerformanceEvent, Color> eventGraphColors;
        private Dictionary<Point, PerformanceIteration> iterationLocations;
        private double graphMaxDuration;
        private double zoom = DefaultZoom;
        private bool drawAverageBar;
        private bool drawInfo;
        ListViewItem selectedIterationItem;

        private string _loadPath;
        public string LoadPath {
            get { return _loadPath; }
            set { _loadPath = value; }
        }

        public PerfViewer() {
            this.DoubleBuffered = true;
            InitializeComponent();

            // initialize data
            iterationFlagMappings = new Dictionary<PerformanceIteration, string>();
            eventGraph = new List<PerformanceEvent>();
            eventGraphColors = new Dictionary<PerformanceEvent, Color>();
            iterationLocations = new Dictionary<Point, PerformanceIteration>();
        }

        private void LoadPerformanceData() {
            // clear ListView
            EventList.Items.Clear();

            if(performanceData == null) {
                return;
            }

            // freeze updating
            EventList.BeginUpdate();

            // add the events in the list
            foreach(PerformanceEvent perfEvent in performanceData) {
                ListViewItem item = new ListViewItem();
                item.Text = perfEvent.Name;
                item.SubItems.Add(perfEvent.IterationCount.ToString());
                item.SubItems.Add(FlagType.None.ToString());
                item.SubItems.Add("false");
                item.ImageKey = "tag_yellow.png";
                item.Tag = perfEvent;
                EventList.Items.Add(item);
            }

            // update list
            EventList.EndUpdate();
            EventStatusLabel.Text = "Events: " + EventList.Items.Count.ToString();

            // select first available event
            if(EventList.Items.Count > 0) {
                EventList.Items[0].Selected = true;
            }
        }

        private void toolStripButton1_Click(object sender, EventArgs e) {
            OpenPerfData();
        }

        private void OpenPerfData() {
            OpenFileDialog dialog = new OpenFileDialog();

            dialog.Filter = "All files (*.*)|*.*";

            if(dialog.ShowDialog() == DialogResult.OK) {
                if(!LoadStandardData(dialog.FileName)) {
                    MessageBox.Show("Failed to load performance data.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                LoadPerformanceData();
            }
        }

        private bool LoadStandardData(string path) {
            StreamReader reader = null;

            try {
                reader = new StreamReader(path);
                XmlSerializer serializer = new XmlSerializer(typeof(List<PerformanceEvent>));

                performanceData = (List<PerformanceEvent>)serializer.Deserialize(reader);
            }
            catch(Exception e) {
                return false;
            }
            finally {
                if(reader != null) {
                    reader.Close();
                }
            }

            return true;
        }

        private void EventList_DrawSubItem(object sender, DrawListViewSubItemEventArgs e) {
            if(e.ColumnIndex == 2) {
                int x = e.Bounds.Left + EventList.Columns[e.ColumnIndex].Width / 2 - 
                        ListViewImageList.ImageSize.Width / 2;
                int y = e.Bounds.Top;
                string flagString = "flag_" + e.SubItem.Text.ToLowerInvariant() + ".png";

                // draw default background
                if((e.ItemState & ListViewItemStates.Selected) == ListViewItemStates.Selected) {
                    e.Graphics.FillRectangle(new SolidBrush(EventList.BackColor), e.Bounds);
                }

                if(ListViewImageList.Images.ContainsKey(flagString)) {
                    e.Graphics.DrawImage(ListViewImageList.Images[flagString], x, y);
                }
            }
            else if(e.ColumnIndex == 3) {
                int x = e.Bounds.Left + EventList.Columns[e.ColumnIndex].Width / 2 - 
                        ListViewImageList.ImageSize.Width / 2;
                int y = e.Bounds.Top + e.Bounds.Height / 2 - 
                        CheckBoxRenderer.GetGlyphSize(e.Graphics, CheckBoxState.UncheckedNormal).Height / 2;

                if(e.SubItem.Text == "true") {
                    CheckBoxRenderer.DrawCheckBox(e.Graphics, new Point(x, y), CheckBoxState.CheckedNormal);
                }
                else {
                    CheckBoxRenderer.DrawCheckBox(e.Graphics, new Point(x, y), CheckBoxState.UncheckedNormal);
                }
            }
            else {
                e.DrawDefault = true;
            }
        }

        private void EventList_MouseUp(object sender, MouseEventArgs e) {
            ListViewItem item = EventList.GetItemAt(e.X, e.Y);

            if(item != null) {
                // check if the click was made in the flag column
                if(item.GetSubItemAt(e.X, e.Y) == item.SubItems[2]) {
                    // show the menu
                    FlagTypeSelector.Tag = new ContextMenuHelper(EventList, item, item.SubItems[2], item.Tag);
                    FlagTypeSelector.Show(EventList.PointToScreen(new Point(e.X, e.Y)));
                }
                else if(item.GetSubItemAt(e.X, e.Y) == item.SubItems[3]) {
                    PerformanceEvent perfEvent = (PerformanceEvent)item.Tag;

                    if(item.SubItems[3].Text == "true") {
                        item.SubItems[3].Text = "false";

                        // remove from the graph list
                        if(eventGraph.Contains(perfEvent)) {
                            eventGraph.Remove(perfEvent);
                        }

                        UpdateGraph();
                    }
                    else {
                        item.SubItems[3].Text = "true";

                        if(!eventGraph.Contains(perfEvent)) {
                            eventGraph.Add(perfEvent);
                        }

                        UpdateGraph();
                    }
                }
            }
        }

        private Color StringToColor(string value, Color defaultColor) {
            Color color = Color.White;

            switch(value) {
                case "None": { color = defaultColor; break; }
                case "Red": { color = Color.LightPink; break; }
                case "Blue": { color = Color.LightBlue; break; }
                case "Green": { color = Color.PaleGreen; break; }
                case "Yellow": { color = Color.LightYellow; break; }
                case "Pink": { color = Color.LavenderBlush; break; }
                case "Purple": { color = Color.Thistle; break; }
            }

            return color;
        }

        private Color StringToColorStrong(string value, Color defaultColor) {
            Color color = Color.White;

            switch(value) {
                case "None": { color = defaultColor; break; }
                case "Red": { color = Color.Red; break; }
                case "Blue": { color = Color.Blue; break; }
                case "Green": { color = Color.Green; break; }
                case "Yellow": { color = Color.Goldenrod; break; }
                case "Pink": { color = Color.DeepPink; break; }
                case "Purple": { color = Color.DarkOrchid; break; }
            }

            return color;
        }

        private void SetListItemBackColor(ListViewItem item, Color color) {
            foreach(ListViewItem.ListViewSubItem subitem in item.SubItems) {
                subitem.BackColor = color;
            }
        }

        private void FlagTypeSelector_ItemClicked(object sender, ToolStripItemClickedEventArgs e) {
            if(FlagTypeSelector.Tag != null) {
                ContextMenuHelper helper = (ContextMenuHelper)FlagTypeSelector.Tag;

                helper.SubItem.Text = e.ClickedItem.Text;
                Color backColor = StringToColor(e.ClickedItem.Text, helper.ParentListView.BackColor);

                foreach(ListViewItem.ListViewSubItem subitem in helper.Item.SubItems) {
                    subitem.BackColor = backColor;
                }

                if(helper.Data != null) {
                    if(helper.Data is PerformanceIteration) {
                        PerformanceIteration iteration = (PerformanceIteration)helper.Data;

                        SetIterationFlag(iteration, e.ClickedItem.Text);
                        UpdateGraph();
                    }
                    else if(helper.Data is PerformanceEvent) {
                        PerformanceEvent perfEvent = (PerformanceEvent)helper.Data;
                        Color graphColor = StringToColorStrong(e.ClickedItem.Text, DefaultGraphColor);

                        if(eventGraphColors.ContainsKey(perfEvent)) {
                            // just update the color
                            eventGraphColors[perfEvent] = graphColor;
                        }
                        else {
                            eventGraphColors.Add(perfEvent, graphColor);
                        }

                        UpdateGraph();
                    }
                }
            }
        }

        private void SetIterationFlag(PerformanceIteration iteration, string value) {
            if(iterationFlagMappings.ContainsKey(iteration)) {
                iterationFlagMappings[iteration] = value;
            }
            else {
                iterationFlagMappings.Add(iteration, value);
            }
        }
        private void EventList_SelectedIndexChanged(object sender, EventArgs e) {
            if(EventList.SelectedIndices.Count > 0) {
                LoadIterations(EventList.SelectedItems[0]);
            }
        }

        private string FormatTimeSpan(TimeSpan time) {
            return string.Format("{0:D2}:{1:D2}:{2:D2}:{3:D3}",
                                 time.Hours, time.Minutes, time.Seconds, time.Milliseconds);
        }

        private void LoadIterations(ListViewItem eventItem) {
            if(eventItem == null) {
                return;
            }

            // clear and freeze the list
            IterationList.Items.Clear();
            IterationList.BeginUpdate();

            TimeSpan maxTime = TimeSpan.MinValue;
            TimeSpan minTime = TimeSpan.MaxValue;
            int maxIterationIndex = -1;
            int minIterationIndex = -1;
            PerformanceEvent perfEvent = (PerformanceEvent)eventItem.Tag;

            for(int i = 0; i < perfEvent.IterationCount; i++) {
                PerformanceIteration iteration = perfEvent.Iterations[i];
                ListViewItem item = new ListViewItem();
                TimeSpan duration = iteration.Duration;

                item.Text = i.ToString();
                item.SubItems.Add(FormatTimeSpan(TimeSpan.FromMilliseconds(iteration.StartTime)));
                item.SubItems.Add(FormatTimeSpan(TimeSpan.FromMilliseconds(iteration.EndTime)));
                item.SubItems.Add(FormatTimeSpan(duration));
                item.SubItems.Add(iteration.HitCount.ToString());
                item.SubItems.Add(iteration.HitsPerSecond == double.NaN ? "Nan" : iteration.HitsPerSecond.ToString());
                item.SubItems.Add(iteration.DeltaWorkingSet.ToString());

                if(iterationFlagMappings.ContainsKey(iteration)) {
                    item.SubItems.Add(iterationFlagMappings[iteration]);
                    SetListItemBackColor(item, StringToColor(iterationFlagMappings[iteration], IterationList.BackColor));
                }
                else {
                    item.SubItems.Add("");
                }

                item.Tag = iteration;
                item.ImageKey = "bullet_go.png";
                IterationList.Items.Add(item);

                // compute maximum/minimum duration
                if(duration > maxTime) {
                    maxTime = duration;
                    maxIterationIndex = i;
                }

                if(duration < minTime) {
                    minTime = duration;
                    minIterationIndex = i;
                }
            }

            // change the background color for the min/max duration iterations
            if(maxIterationIndex >= 0) {
                IterationList.Items[maxIterationIndex].SubItems[7].Text = FlagType.Red.ToString();
                SetListItemBackColor(IterationList.Items[maxIterationIndex], 
                                     StringToColor(FlagType.Red.ToString(), IterationList.BackColor));
                SetIterationFlag(IterationList.Items[maxIterationIndex].Tag as PerformanceIteration, 
                                 FlagType.Red.ToString());
            }

            if(minIterationIndex >= 0) {
                IterationList.Items[minIterationIndex].SubItems[7].Text = FlagType.Green.ToString();
                SetListItemBackColor(IterationList.Items[minIterationIndex],
                                     StringToColor(FlagType.Green.ToString(), IterationList.BackColor));
                SetIterationFlag(IterationList.Items[minIterationIndex].Tag as PerformanceIteration, 
                                 FlagType.Green.ToString());
            }

            IterationList.EndUpdate();
            UpdateEventInfoPanel(perfEvent);
        }

        private void UpdateEventInfoPanel(PerformanceEvent perfEvent) {
            if(perfEvent == null) {
                return;
            }

            NameLabel.Text = (perfEvent.Name == null || perfEvent.Name == string.Empty) ?
                             "Untitled" : perfEvent.Name;
            IterationLabel.Text = perfEvent.IterationCount.ToString();

            // average duration
            TimeSpan? avgDuration = perfEvent.AverageDuration;

            if(avgDuration.HasValue) {
                AvgDurationLabel.Text = FormatTimeSpan(avgDuration.Value);
            }
            else {
                AvgDurationLabel.Text = "None";
            }

            // maximum duration
            if(perfEvent.IterationCount > 0) {
                TimeSpan maxDuration = TimeSpan.MinValue;

                for(int i = 0; i < perfEvent.IterationCount; i++) {
                    if(perfEvent.Iterations[i].Duration > maxDuration) {
                        maxDuration = perfEvent.Iterations[i].Duration;
                    }
                }

                MaxDurationLabel.Text = FormatTimeSpan(maxDuration);
            }
            else {
                MaxDurationLabel.Text = "None";
            }
        }

        private void IterationList_DrawSubItem(object sender, DrawListViewSubItemEventArgs e) {
            if(e.ColumnIndex == 7) {
                int x = e.Bounds.Left + IterationList.Columns[e.ColumnIndex].Width / 2 - 
                        ListViewImageList.ImageSize.Width / 2;
                int y = e.Bounds.Top;
                string flagString = "flag_" + e.SubItem.Text.ToLowerInvariant() + ".png";

                // draw default background
                if((e.ItemState & ListViewItemStates.Selected) == ListViewItemStates.Selected) {
                    e.Graphics.FillRectangle(Brushes.LightGray, e.Bounds);
                }

                if(ListViewImageList.Images.ContainsKey(flagString)) {
                    e.Graphics.DrawImage(ListViewImageList.Images[flagString], x, y);
                }
            }
            else {
                e.DrawDefault = true;
            }
        }

        private void EventList_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e) {
            e.DrawDefault = true;
        }

        private void IterationList_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e) {
            e.DrawDefault = true;
        }

        private void IterationList_MouseUp(object sender, MouseEventArgs e) {
            ListViewItem item = IterationList.GetItemAt(e.X, e.Y);

            if(item != null) {
                // check if the click was made in the flag column
                if(item.GetSubItemAt(e.X, e.Y) == item.SubItems[7]) {
                    // show the menu
                    FlagTypeSelector.Tag = new ContextMenuHelper(IterationList, item, item.SubItems[7], item.Tag);
                    FlagTypeSelector.Show(IterationList.PointToScreen(new Point(e.X, e.Y)));
                }
            }
        }


        private void UpdateGraph() {
            if(eventGraph == null) {
                return;
            }

            graphMaxDuration = TimeSpan.MinValue.TotalMilliseconds;
            int maxIterations = int.MinValue;

            for(int eventIndex = 0; eventIndex < eventGraph.Count; eventIndex++) {
                PerformanceEvent perfEvent = eventGraph[eventIndex];

                if(perfEvent.IterationCount > maxIterations) {
                    maxIterations = perfEvent.IterationCount;
                }

                for(int iterationIndex = 0; iterationIndex < perfEvent.IterationCount; iterationIndex++) {
                    double duration = perfEvent.Iterations[iterationIndex].Duration.TotalMilliseconds;

                    if(duration > graphMaxDuration) {
                        graphMaxDuration = duration;
                    }
                }
            }

            // don't allow maxDuration  = 0
            graphMaxDuration = Math.Max(1, graphMaxDuration);

            // set scroolbar maximum value
            int max = AdjustToZoom(maxIterations * DefaultIterationDistance);
            GraphHostScroolbar.Maximum = Math.Max(0, max - GraphHost.Width);
            GraphHostScroolbar.Visible = GraphHostScroolbar.Maximum > 0;
            GraphHost.Refresh();
        }

        private int AdjustToZoom(int value) {
            return (int)Math.Ceiling((double)value * zoom);
        }

        private double AdjustToZoom(double value) {
            return value * zoom;
        }

        private bool IsMinDuration(int index, PerformanceEvent perfEvent) {
            if(perfEvent == null || index < 0) {
                return false;
            }

            for(int i = 0; i < eventGraph.Count; i++) {
                if(eventGraph[i].IterationCount > index) {
                    if(eventGraph[i].Iterations[index].Duration < perfEvent.Iterations[index].Duration) {
                        return false;
                    }
                }
            }

            return true;
        }

        private void GraphHost_Paint(object sender, PaintEventArgs e) {
            DrawEventGraph(e.Graphics);
        }

        private void DrawEventGraph(Graphics g) {
            if(graphMaxDuration == 0) {
                return;
            }

            // set graphics quality
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

            int leftMargin = AdjustToZoom(DefaultLeftMargin);
            int iterationDistance = AdjustToZoom(DefaultIterationDistance);
            int startX = -Math.Max(0, GraphHostScroolbar.Value);
            int startIndex = (-startX) / iterationDistance;
            int bulletSize = AdjustToZoom(DefaultBulletSize);
            int maxHeight = GraphHost.Height - bulletSize / 2;
            int lastInfoDrawn = -1;
            PerformanceIteration selectedIteration = null;

            // clear background
            g.FillRectangle(new SolidBrush(GraphHost.BackColor), 0, 0, GraphHost.Width, GraphHost.Height);

            // reset locations
            iterationLocations.Clear();

            if(drawInfo) {
                maxHeight -= InfoZoneHeight;

                // draw the info region
                g.FillRectangle(new SolidBrush(InfoZoneBackColor), 0, maxHeight, 
                                GraphHost.Width, maxHeight + InfoZoneHeight);
            }

            if(selectedIterationItem != null && selectedIterationItem.Tag is PerformanceIteration) {
                selectedIteration = (PerformanceIteration)selectedIterationItem.Tag;
            }

            for(int eventIndex = 0; eventIndex < eventGraph.Count; eventIndex++) {
                PerformanceEvent perfEvent = eventGraph[eventIndex];

                int lastX = 0;
                int lastY = 0;
                int currentX = 0;
                int currentY = 0;
                Brush drawingBrush;
                Pen drawingPen;
                Color drawingColor = GetDrawingColor(perfEvent);
                drawingBrush = new SolidBrush(drawingColor);
                drawingPen = new Pen(drawingColor, 1.5f);

                if(drawAverageBar && perfEvent.AverageDuration.HasValue) {
                    drawingColor = DrawAverageBar(g, bulletSize, maxHeight, perfEvent, drawingColor);
                }

                // draw each component of the event
                for(int iterationIndex = startIndex; iterationIndex < perfEvent.IterationCount; iterationIndex++) {
                    PerformanceIteration iteration = perfEvent.Iterations[iterationIndex];

                    double iterationDuration = iteration.Duration.TotalMilliseconds;
                    currentX = leftMargin + iterationIndex * iterationDistance + startX;
                    currentY = maxHeight - (int)Math.Ceiling((iterationDuration / graphMaxDuration) *
                                                             (double)(maxHeight - bulletSize / 2));
                    if(iterationIndex == startIndex) {
                        lastX = currentX;
                        lastY = currentY;
                    }

                    // draw a distinct background for the selected item
                    if(iteration == selectedIteration) {
                        g.DrawRectangle(drawingPen, currentX - bulletSize * 2, 0, bulletSize * 4, maxHeight);
                    }

                    // draw info
                    if(drawInfo) {
                        lastInfoDrawn = DrawEventInfo(g, maxHeight, lastInfoDrawn, perfEvent, 
                                                      currentX, currentY, iterationIndex);
                    }

                    // draw line
                    g.DrawLine(drawingPen, lastX, lastY, currentX, currentY);

                    // draw bullet
                    g.FillEllipse(drawingBrush, currentX - bulletSize / 2,
                                  currentY - bulletSize / 2, bulletSize, bulletSize);

                    // check if the iteration has a color associated with it
                    if(iterationFlagMappings.ContainsKey(iteration)) {
                        Color penColor = StringToColorStrong(iterationFlagMappings[iteration], Color.Empty);
                        g.DrawEllipse(new Pen(penColor, (float)AdjustToZoom(2.0)),
                                      currentX - bulletSize / 2 - 1, currentY - bulletSize / 2 - 1,
                                      bulletSize + 1, bulletSize + 1);
                    }

                    lastX = currentX;
                    lastY = currentY;

                    // put the iteration in the list
                    Point location = new Point(currentX, currentY);

                    if(!iterationLocations.ContainsKey(location)) {
                        iterationLocations.Add(location, iteration);
                    }

                    // break if out of screen
                    if(lastX > GraphHost.Width) {
                        break;
                    }
                }
            }
        }

        private int DrawEventInfo(Graphics g, int maxHeight, int lastInfoDrawn, PerformanceEvent perfEvent, 
                                  int currentX, int currentY, int iterationIndex) {
            if(iterationIndex > lastInfoDrawn) {
                string info = iterationIndex.ToString();
                SizeF infoSize = g.MeasureString(info, this.Font);
                float infoX = currentX - infoSize.Width / 2;
                float infoY = maxHeight + InfoZoneHeight / 2 - infoSize.Height / 2 + 2;

                // draw string
                g.DrawString(info, this.Font, Brushes.Black, infoX, infoY);

                // prevent redrawing the same info again4
                lastInfoDrawn = iterationIndex;
            }

            if(IsMinDuration(iterationIndex, perfEvent)) {
                Pen p = new Pen(Brushes.LightGray);
                p.DashStyle = System.Drawing.Drawing2D.DashStyle.DashDot;
                g.DrawLine(p, currentX, currentY, currentX, maxHeight);
            }
            return lastInfoDrawn;
        }

        private Color DrawAverageBar(Graphics g, int bulletSize, int maxHeight, 
                                     PerformanceEvent perfEvent, Color drawingColor) {
            // disable antialiasing while drawing the average line
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.Default;

            Pen linePen = new Pen(drawingColor, 1.0f);
            int lineY = maxHeight - (int)Math.Ceiling((perfEvent.AverageDuration.Value.TotalMilliseconds / graphMaxDuration) *
                       (double)(maxHeight - bulletSize / 2));

            linePen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
            g.DrawLine(linePen, 0, lineY, GraphHost.Width, lineY);

            // reenable antialiasing
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            return drawingColor;
        }

        private Color GetDrawingColor(PerformanceEvent perfEvent) {
            Color drawingColor;

            if(eventGraphColors.ContainsKey(perfEvent)) {
                drawingColor = eventGraphColors[perfEvent];
            }
            else {
                // set default
                drawingColor = DefaultGraphColor;
            }
            return drawingColor;
        }

        private void GraphHostScroolbar_Scroll(object sender, ScrollEventArgs e) {
            UpdateGraph();
        }

        private void button4_Click(object sender, EventArgs e) {
            zoom += 0.25;
            UpdateGraph();
        }

        private void button3_Click(object sender, EventArgs e) {
            zoom = Math.Max(MinZoom, zoom - 0.25);
            UpdateGraph();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e) {
            drawAverageBar = AvgLinesCheckbox.Checked;
            UpdateGraph();
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e) {
            drawInfo = ShowInfoCheckbox.Checked;
            UpdateGraph();
        }

        private void GraphHost_MouseMove(object sender, MouseEventArgs e) {
            int x = e.X + GraphHostScroolbar.Value;
            int y = e.Y;
            int bulletSize = AdjustToZoom(DefaultBulletSize);

            foreach(KeyValuePair<Point, PerformanceIteration> kvp in iterationLocations) {
                Point position = kvp.Key;

                if(e.X >= position.X - bulletSize / 2 &&
                   e.X <= position.X + bulletSize / 2 &&
                    e.Y >= position.Y - bulletSize / 2 &&
                    e.Y <= position.Y + bulletSize / 2) {
                    // found
                    IterationStatusLabel.Text = FormatTimeSpan(kvp.Value.Duration);
                    return;
                }
            }

            IterationStatusLabel.Text = "";
        }

        private void button5_Click(object sender, EventArgs e) {
            Bitmap bitmap = new Bitmap(GraphHost.Width, GraphHost.Height);
            Graphics g = Graphics.FromImage(bitmap);
            Point hostLocation = GraphHost.PointToScreen(new Point(0, 0));
            g.CopyFromScreen(hostLocation.X, hostLocation.Y, 0, 0, new Size(GraphHost.Width, GraphHost.Height));

            Clipboard.SetImage(bitmap);
        }

        private void button6_Click(object sender, EventArgs e) {
            if(colorDialog1.ShowDialog() == DialogResult.OK) {
                GraphHost.BackColor = colorDialog1.Color;
            }
        }

        private void GraphHost_Resize(object sender, EventArgs e) {
            UpdateGraph();
        }

        private void IterationList_SelectedIndexChanged(object sender, EventArgs e) {
            if(IterationList.SelectedItems.Count > 0) {
                selectedIterationItem = IterationList.SelectedItems[0];
                BringIterationIntoView(IterationList.SelectedIndices[0]);
                UpdateGraph();
            }
        }

        private void BringIterationIntoView(int index) {
            int delta = Math.Max(0, GraphHostScroolbar.Value);
            int x = AdjustToZoom(index * DefaultIterationDistance);
            int width = AdjustToZoom(DefaultBulletSize) * 4;

            if((x + width) - delta >= GraphHost.Width) {
                GraphHostScroolbar.Value = Math.Min((x + width) - GraphHost.Width, GraphHostScroolbar.Maximum);
            }
            else if(x - delta < 0) {
                GraphHostScroolbar.Value = Math.Min(x, GraphHostScroolbar.Maximum);
            }
        }

        #region Settings

        public void SaveSettings() {
            Properties.Settings.Default.MainSplitterDistance = MainSplitContainer.SplitterDistance;
            Properties.Settings.Default.GraphSplitterDistance = DetailsSplitContainer.SplitterDistance;
            Properties.Settings.Default.EventInfoSplitterDistance = InfoSplitContainer.SplitterDistance;

            Properties.Settings.Default.Save();
        }

        #endregion

        int val;
        private void PerfViewer_Load(object sender, EventArgs e) {
            drawInfo = ShowInfoCheckbox.Checked;
            drawAverageBar = AvgLinesCheckbox.Checked;
            MainSplitContainer.SplitterDistance = Properties.Settings.Default.MainSplitterDistance;
            val = Properties.Settings.Default.GraphSplitterDistance;

            if(_loadPath != null && _loadPath != "") {
                if(File.Exists(_loadPath)) {
                    LoadStandardData(_loadPath);
                    LoadPerformanceData();
                }
            }
        }

        private void button2_Click(object sender, EventArgs e) {
            EventInfoToolButton.Checked = false;
        }

        private void button1_Click(object sender, EventArgs e) {
            GraphToolButton.Checked = false;
        }

        private void GraphToolButton_CheckedChanged(object sender, EventArgs e) {
            DetailsSplitContainer.Panel2Collapsed = !GraphToolButton.Checked;
        }

        private void EventInfoToolButton_CheckedChanged(object sender, EventArgs e) {
            InfoSplitContainer.Panel2Collapsed = !EventInfoToolButton.Checked;
        }

        private void AboutToolButton_Click(object sender, EventArgs e) {
            new AboutBox().ShowDialog();
        }

        private void ReportPreviewToolButton_Click(object sender, EventArgs e) {
            PreviewHtmlReport();
        }

        #region Exporting

        private void PreviewHtmlReport() {
            if(performanceData == null) {
                // data not loaded yet
                return;
            }

            PerformanceManager manager = new PerformanceManager();
            manager.Events = new Dictionary<string, PerformanceEvent>();

            foreach(PerformanceEvent perfEvent in performanceData) {
                if(!manager.Events.ContainsKey(perfEvent.Name)) {
                    manager.Events.Add(perfEvent.Name, perfEvent);
                }
            }

            manager.GenerateHtmlSummary();
        }

        private void ReportToolButton_Click(object sender, EventArgs e) {
            SaveHtmlReport();
        }

        private void SaveHtmlReport() {
            SaveFileDialog dialog = new SaveFileDialog();

            dialog.Filter = "HTML Files (*.htm)|*.htm";

            if(dialog.ShowDialog() == DialogResult.OK) {
                PerformanceManager manager = new PerformanceManager();
                manager.Events = new Dictionary<string, PerformanceEvent>();

                foreach(PerformanceEvent perfEvent in performanceData) {
                    if(!manager.Events.ContainsKey(perfEvent.Name)) {
                        manager.Events.Add(perfEvent.Name, perfEvent);
                    }
                }

                if(!manager.GenerateHtmlSummary(dialog.FileName, null, false)) {
                    MessageBox.Show("Failed to save report.", "Error", 
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void SaveCSVFile() {
            SaveFileDialog dialog = new SaveFileDialog();

            dialog.Filter = "CSV Files (*.csv)|*.csv";

            if(dialog.ShowDialog() == DialogResult.OK) {
                StreamWriter writer = null;

                try {
                    writer = new StreamWriter(dialog.FileName);

                    foreach(PerformanceEvent perfEvent in performanceData) {
                        writer.WriteLine(perfEvent.Name);
                        writer.WriteLine("Id,Duration,HitCount,HitsPerSecond,DeltaWorkingSet");

                        for(int iterationIndex = 0; iterationIndex < perfEvent.IterationCount; iterationIndex++) {
                            PerformanceIteration iteration = perfEvent.Iterations[iterationIndex];

                            writer.WriteLine("{0},{1},{2},{3},{4}",
                                             iterationIndex,
                                             iteration.Duration,
                                             iteration.HitCount,
                                             iteration.HitsPerSecond,
                                             iteration.DeltaWorkingSet);
                        }

                        writer.WriteLine(",");
                    }
                }
                catch(Exception e) {
                    MessageBox.Show("Failed to save CSV file.", "Error", 
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally {
                    if(writer != null) {
                        writer.Close();
                    }
                }
            }
        }

        #endregion

        private void CsvToolButton_Click(object sender, EventArgs e) {
            SaveCSVFile();
        }

        private void button7_Click(object sender, EventArgs e) {
            DeleteEvent();
        }

        private void DeleteEvent() {
            while(EventList.SelectedItems.Count > 0) {
                ListViewItem eventItem = EventList.SelectedItems[0];
                PerformanceEvent perfEvent = (PerformanceEvent)eventItem.Tag;

                // remove from data
                performanceData.Remove(perfEvent);
                eventGraphColors.Remove(perfEvent);
                eventGraph.Remove(perfEvent);

                // remove iteration flags
                foreach(PerformanceIteration iteration in perfEvent.Iterations) {
                    iterationFlagMappings.Remove(iteration);
                }

                // remove from listview
                EventList.Items.Remove(eventItem);
            }

            // select the first available event
            if(EventList.Items.Count > 0) {
                EventList.Items[0].Selected = true;
            }
            else {
                IterationList.Items.Clear();
            }

            UpdateGraph();
        }

        private void toolStripButton1_Click_1(object sender, EventArgs e) {
            SaveData();
        }

        private void SaveData() {
            SaveFileDialog dialog = new SaveFileDialog();

            dialog.Filter = "All Files (*.*)|*.*";

            if(dialog.ShowDialog() == DialogResult.OK) {
                PerformanceManager manager = new PerformanceManager();
                manager.Events = new Dictionary<string, PerformanceEvent>();

                foreach(PerformanceEvent perfEvent in performanceData) {
                    if(!manager.Events.ContainsKey(perfEvent.Name)) {
                        manager.Events.Add(perfEvent.Name, perfEvent);
                    }
                }

                if(!manager.SerializeEvents(dialog.FileName)) {
                    MessageBox.Show("Failed to save performance data.", "Error", 
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
