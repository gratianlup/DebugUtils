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
using System.Diagnostics;
using System.Xml.Serialization;
using System.Reflection;
using System.Messaging;
using System.Threading;

namespace DebugUtils.Debugger.Listeners {
    /// <summary>
    /// Listener that writes the messages to a File.
    /// </summary>
    public class TextFileListener : DebugListenerBase {
        #region Fields

        private StreamWriter writer;

        #endregion

        #region Constructor

        #region Destructor

        ~TextFileListener() {
            Close();
        }

        #endregion

        #endregion

        #region Public methods

        public override bool Open() {
            if(_filePath == null || _filePath.Length == 0) {
                return false;
            }

            try {
                writer = File.CreateText(_filePath);
                writer.WriteLine("Text File listener opened at " + DateTime.Now.ToShortDateString().ToString() + " " + DateTime.Now.ToLongTimeString().ToString());
                writer.WriteLine("**************************************************\n\n");
                writer.Flush();
                IsOpen = true;
            }
            catch(Exception e) {
                Console.WriteLine(e.Message);
                IsOpen = false;
                return false;
            }

            IsOpen = writer.BaseStream.CanWrite;
            return IsOpen;
        }

        public override bool Close() {
            if(IsOpen && writer != null && writer.BaseStream.CanWrite) {
                writer.WriteLine("\n\n**************************************************");
                writer.WriteLine("Text File listener closed at " + DateTime.Now.ToShortDateString().ToString() + " " + DateTime.Now.ToLongTimeString().ToString());
                writer.Close();
                IsOpen = false;
                return true;
            }

            return false;
        }

        public override bool DumpMessage(DebugMessage message) {
            if(!Enabled) {
                return false;
            }

            writer.Write(message.Time.ToLongTimeString() + " | ");

            switch(message.Type) {
                case DebugMessageType.Error: {
                    writer.Write("Error");
                    break;
                }
                case DebugMessageType.Warning: {
                    writer.Write("Warning");
                    break;
                }
                default: {
                    writer.Write("Unknown");
                    break;
                }
            }

            writer.WriteLine(" | #" + HandledMessages);
            writer.WriteLine(message.Message + "\n");
            writer.Write("METHOD: ");
            writer.WriteLine(GetMethodTypeString(message.BaseMethod.Type) + message.BaseMethod.Method);
            writer.Write("LINE:   ");
            writer.WriteLine(message.BaseMethod.Line);
            writer.Write("FILE:   ");
            
            if(TruncateFile) {
                FileInfo fi = new FileInfo(message.BaseMethod.File);
                writer.WriteLine(fi.Name);
            }
            else {
                writer.WriteLine(message.BaseMethod.File);
            }

            if(UseStackInfo && message.HasStack) {
                writer.WriteLine("\nSTACK:");

                for(int i = 0; i < message.StackSegments.Count; i++) {
                    writer.WriteLine();
                    writer.Write("METHOD: ");
                    writer.WriteLine(GetMethodTypeString(message.BaseMethod.Type) + message.StackSegments[i].Method);
                    writer.Write("LINE:   ");
                    writer.WriteLine(message.StackSegments[i].Line);
                    writer.Write("FILE:   ");

                    if(TruncateFile) {
                        if(message.StackSegments[i].File != null) {
                            FileInfo fi = new FileInfo(message.StackSegments[i].File);
                            writer.WriteLine(fi.Name);
                        }
                    }
                    else {
                        writer.WriteLine(message.StackSegments[i].File);
                    }
                }
            }

            writer.WriteLine("**************************************************\n");
            writer.Flush();
            HandledMessages++;
            return true;
        }

        #endregion

        #region Properties

        private bool _enabled;
        public override bool Enabled {
            get { return _enabled; }
            set { _enabled = value; }
        }

        private string _filePath;
        public string FilePath {
            get { return _filePath; }
        }

        #endregion
    }
}
