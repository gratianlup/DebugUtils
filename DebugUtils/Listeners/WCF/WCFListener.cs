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
using System.Threading;
using System.ServiceModel;
using System.Runtime.Serialization;
using DebugUtils.Debugger.Listeners.WCF;

namespace DebugUtils.Debugger.Listeners {
    internal class DebugServiceEvents : IDebugServiceCallback {
        private DebugServiceClient _client;
        public DebugServiceClient Client {
            get { return _client; }
            set { _client = value; }
        }

        public AppInfo RequestApplicationInfo() {
            AppInfo info = new AppInfo();
            System.Diagnostics.Process p = System.Diagnostics.Process.GetCurrentProcess();

            try {
                info.MachineName = p.MachineName;
                info.MainWindowTitle = p.MainWindowTitle;
                info.PrivateMemorySize = p.PrivateMemorySize64;
                info.StartTime = p.StartTime;
                info.ThreadNumber = p.Threads.Count;
                info.TotalProcessorTime = p.TotalProcessorTime;
                info.UserName = Environment.UserName;
                info.UserProcessorTime = p.UserProcessorTime;
                info.VirtualMemorySize = p.VirtualMemorySize64;
                info.WorkingSet = p.WorkingSet64;
                info.OSVersion = Environment.OSVersion;
                info.ProcessorCount = Environment.ProcessorCount;
            }
            catch(Exception e) {
                Console.WriteLine("Failed to get process information. Exception: {0}", e.Message);
            }

            return info;
        }


        public void ExitApplication() {
            // disconnect first
            if(_client != null) {
                _client.Close();
            }

            Environment.Exit(0);
        }
    }


    /// <summary>
    /// Listener that sends massages to a service using WCF.
    /// </summary>
    public class WCFListener : DebugListenerBase {
        #region Fields

        private DebugServiceClient client;
        private string clientName;

        #endregion

        #region Properties

        private string _adress;
        public string Adress {
            get { return _adress; }
            set { _adress = value; }
        }


        private bool _enabled;
        public override bool Enabled {
            get { return _enabled; }
            set { _enabled = value; }
        }

        #endregion

        #region Constructor

        public WCFListener() {
            _enabled = true;
        }

        public WCFListener(int listenerId, string adress)
            : this() {
            ListnerId = listenerId;
            _adress = adress;
        }

        #endregion

        #region Destructor

        ~WCFListener() {
            // close the connection
            Close();
        }

        #endregion

        #region Private methods

        private void HandleResponse(ClientResponse response) {
            switch(response) {
                case ClientResponse.Break: {
                    System.Diagnostics.Debugger.Break();
                    break;
                }
                case ClientResponse.Exit: {
                    Environment.Exit(0);
                    break;
                }
            }
        }

        #endregion

        #region Public methods

        public override bool Open() {
            if(!IsOpen) {
                try {
                    DebugServiceEvents debugEvents = new DebugServiceEvents();
                    InstanceContext context = new InstanceContext(debugEvents);
                    client = new DebugServiceClient(context, new NetTcpBinding(), new EndpointAddress(_adress));
                    debugEvents.Client = client;

                    // set the client name as the name of the assembly
                    clientName = Assembly.GetExecutingAssembly().FullName;
                    client.Open(clientName, System.Diagnostics.Process.GetCurrentProcess().MachineName,
                                           System.Diagnostics.Process.GetCurrentProcess().ProcessName);

                    // register for all events
                    client.Subscribe(clientName, EventType.All);
                    IsOpen = true;
                }
                catch(CommunicationException ce) {
                    Console.WriteLine("Failed to connect to WCF service. Exception: {0}", ce.Message);
                    return false;
                }
                catch(Exception e) {
                    Console.WriteLine("Unknown error while connecting to WCF service. Exception: {0}", e.Message);
                    return false;
                }
            }

            return true;
        }


        public override bool Close() {
            if(IsOpen) {
                try {
                    //client.Unsubscribe(clientName, EventType.All);
                    client.Close(clientName);
                }
                catch(CommunicationException ce) {
                    Console.WriteLine("Failed to close WCF service. Exception: {0}", ce.Message);
                    return false;
                }
                catch(Exception e) {
                    Console.WriteLine("Unknown error while closing WCF service. Exception: {0}", e.Message);
                    return false;
                }
            }

            return true;
        }


        public override bool DumpMessage(DebugMessage message) {
            if(!IsOpen || !_enabled) {
                return false;
            }

            try {
                HandleResponse(client.HandleDebugMessage(clientName, message));
            }
            catch(CommunicationException ce) {
                Console.WriteLine("Failed to call method on WCF service. Exception: {0}", ce.Message);
                return false;
            }

            return true;
        }

        #endregion
    }
}
