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

namespace DebugUtils.Debugger {
    public class DelegateHelper {
        public static Delegate[] GetDelegateList(Delegate del) {
            if(del == null) {
                throw new ArgumentNullException("del");
            }

            return del.GetInvocationList();
        }

        public static MethodInfo[] GetMethodList(Delegate del) {
            if(del == null) {
                throw new ArgumentNullException("del");
            }

            Delegate[] delegates = del.GetInvocationList();
            MethodInfo[] methods = new MethodInfo[delegates.Length];

            for(int i = 0; i < delegates.Length; i++) {
                methods[i] = delegates[i].Method;
            }

            return methods;
        }

        public static string[] GetMethodStringList(Delegate del) {
            if(del == null) {
                throw new ArgumentNullException("del");
            }

            Delegate[] delegates = del.GetInvocationList();
            string[] methods = new string[delegates.Length];

            for(int i = 0; i < delegates.Length; i++) {
                methods[i] = delegates[i].Method.Name;
            }

            return methods;

        }

        public static string GetMethodListSummary(Delegate del) {
            if(del == null) {
                throw new ArgumentNullException("del");
            }

            StringBuilder builder = new StringBuilder();
            string[] methods = GetMethodStringList(del);

            foreach(string s in methods) {
                builder.Append(s);
                builder.AppendLine();
            }

            builder.AppendLine();
            builder.Append("Methods: ");
            builder.Append(methods.Length);
            return builder.ToString();
        }

        public static bool ContainsMethod(Delegate del, string name) {
            if(del == null || name == null) {
                throw new ArgumentNullException("del | name");
            }

            Delegate[] delegates = GetDelegateList(del);

            for(int i = 0; i < delegates.Length; i++) {
                if(delegates[i].Method.Name == name) {
                    return true;
                }
            }

            return false;
        }
    }
}
