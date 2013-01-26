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
using System.Messaging;
using System.Threading;

namespace DebugUtils.Debugger.Listeners {
    /// <summary>
    /// Listener that writes the messages to the console.
    /// </summary>
    public class ConsoleListener : DebugListenerBase {
        #region Constructor

        public ConsoleListener(int id) {
            ListnerId = id;
            _enabled = true;
        }

        #endregion

        #region Destructor

        ~ConsoleListener() {
            Close();
        }

        #endregion

        #region Properties

        private bool _enabled;
        public override bool Enabled {
            get { return _enabled; }
            set { _enabled = value; }
        }

        #endregion

        #region Private methods

        private void WriteSeparator(ConsoleColor color) {
            Console.BackgroundColor = color;
            Console.ForegroundColor = color;
            Console.Write(new StringBuilder().Append('c', 80).Append("\n").ToString());
            Console.ResetColor();
        }


        private void WriteMethodInfo(MethodType type, string method, ConsoleColor typeColor, ConsoleColor methodColor) {
            Console.ForegroundColor = typeColor;
            Console.Write(GetMethodTypeString(type) + " ");

            string typeName = method.Substring(0, method.IndexOf(' '));
            string methodName = method.Substring(typeName.Length + 1,
                                                 method.Length - typeName.Length - 1);

            Console.Write(typeName + " ");
            Console.ForegroundColor = methodColor;
            Console.WriteLine(methodName);
        }


        private ConsoleColor GetMessageColor(DebugMessage message) {
            switch(message.Type) {
                case DebugMessageType.Error: {
                    return ConsoleColor.Red;
                    break;
                }
                case DebugMessageType.Warning: {
                    return ConsoleColor.Yellow;
                    break;
                }
                default: {
                    return ConsoleColor.Green;
                    break;
                }
            }
        }

        #endregion

        #region Public methods

        public override bool Open() {
            Console.WriteLine("Console listener opened at " + DateTime.Now.ToShortDateString().ToString() + " " + DateTime.Now.ToLongTimeString().ToString());
            IsOpen = true;
            return true;
        }

        public override bool Close() {
            Console.WriteLine("\n\n");
            WriteSeparator(ConsoleColor.Red);
            Console.WriteLine("Console listener closed at " + DateTime.Now.ToShortDateString().ToString() + " " + DateTime.Now.ToLongTimeString().ToString());
            IsOpen = false;
            return true;
        }

        public override bool DumpMessage(DebugMessage message) {
            if(!Enabled) {
                return false;
            }

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(message.Time.ToLongTimeString() + " | ");
            Console.ForegroundColor = GetMessageColor(message);

            if(message.Type == DebugMessageType.Error) {
                Console.Write("Error");
            }
            else if(message.Type == DebugMessageType.Unknown) {
                Console.Write("Unknown");
            }
            else {
                Console.Write("Warning");
            }

            Console.ResetColor();
            Console.WriteLine(" | #" + HandledMessages);

            Console.ForegroundColor = GetMessageColor(message);
            Console.WriteLine("\n" + message.Message + "\n\n");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("NAMESPACE: ");
            Console.WriteLine(message.BaseMethod.DeclaringNamespace);
            Console.ResetColor();

            Console.Write("OBJECT:    ");
            Console.WriteLine(message.BaseMethod.DeclaringObject);
            Console.ResetColor();

            Console.Write("METHOD:    ");

            if(SimplifyMethod) {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(ExtractSimplifiedMethod(message.BaseMethod.Method));
            }
            else {
                WriteMethodInfo(message.BaseMethod.Type, message.BaseMethod.Method,
                                ConsoleColor.DarkYellow, ConsoleColor.Yellow);
            }

            Console.ResetColor();
            Console.Write("LINE:      ");
            Console.WriteLine(message.BaseMethod.Line);
            Console.ResetColor();

            Console.Write("FILE:      ");

            if(TruncateFile) {
                FileInfo fi = new FileInfo(message.BaseMethod.File);
                Console.WriteLine(fi.Name);
            }
            else {
                Console.WriteLine(message.BaseMethod.File);
            }

            Console.ResetColor();

            if(UseStackInfo && message.HasStack) {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("\nSTACK:");

                for(int i = 0; i < message.StackSegments.Count; i++) {
                    Console.WriteLine();
                    Console.ResetColor();
                    Console.Write("METHOD: ");

                    WriteMethodInfo(message.StackSegments[i].Type, message.StackSegments[i].Method,
                                ConsoleColor.DarkCyan, ConsoleColor.Cyan);

                    Console.ResetColor();
                    Console.Write("LINE:   ");
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.WriteLine(message.StackSegments[i].Line);
                    Console.ResetColor();
                    Console.Write("FILE:   ");
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    if(TruncateFile) {
                        if(message.StackSegments[i].File != null) {
                            FileInfo fi = new FileInfo(message.StackSegments[i].File);
                            Console.WriteLine(fi.Name);
                        }
                    }
                    else {
                        Console.WriteLine(message.StackSegments[i].File);
                    }
                }
            }

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine();
            WriteSeparator(ConsoleColor.DarkGray);
            HandledMessages++;
            return true;
        }

        #endregion
    }
}
