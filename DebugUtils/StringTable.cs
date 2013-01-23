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
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Globalization;
using System.Resources;
using System.Xml.Serialization;
using System.Reflection;
using System.Collections.Specialized;

namespace DebugUtils.Debugger {
    public class StringTable {
        #region Fields

        private StringDictionary table;
        public ResourceManager ResourceManager;

        #endregion

        #region Constructor

        public StringTable() {
            table = new StringDictionary();
        }

        #endregion

        #region Indexers

        public string this[string key] {
            get { return GetString(key); }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Add a string to the string table
        /// </summary>
        /// <param name="key">The key of the string.</param>
        /// <param name="value">The string value.</param>
        public bool AddString(string key, string value) {
            if(key == null || value == null) {
                throw new ArgumentNullException("key or value");
            }

            if(key.Length == 0) {
                return false;
            }

            // '@' on the first position is invalid
            if(key[0] == '@') {
                key = key.Substring(1);
            }

            if(key.Length == 0) {
                return false;
            }

            // check if the key is allready added
            if(table.ContainsKey(key)) {
                return false;
            }

            table.Add(key, value);
            return true;
        }


        /// <summary>
        /// Retrieve the string value for the given key
        /// </summary>
        /// <param name="key">The key of the string.</param>
        /// <remarks>If the key was not found, the value will be retrieved from the associated ResourceManager.</remarks>
        public string GetString(string key) {
            if(key == null) {
                throw new ArgumentNullException("key");
            }

            // '@' on the first position is invalid
            if(key[0] == '@') {
                key = key.Substring(1);
            }

            if(key.Length == 0) {
                return null;
            }

            // get the key
            if(table.ContainsKey(key)) {
                return table[key];
            }
            else {
                // get it from the resource manager
                if(ResourceManager != null) {
                    try {
                        return ResourceManager.GetString(key);
                    }
                    catch(Exception e) {
                        Console.WriteLine("Failed to get resource string for key {0}. Exception message: {1}", key, e.Message);
                        return null;
                    }
                }
            }

            return null;
        }


        /// <summary>
        /// Remove a string from the string table
        /// </summary>
        /// <param name="key">The key of the string.</param>
        public bool RemoveString(string key) {
            if(key == null) {
                throw new ArgumentNullException("key");
            }

            // '@' on the first position is invalid
            if(key[0] == '@') {
                key = key.Substring(1);
            }

            if(key.Length == 0) {
                return false;
            }

            if(table.ContainsKey(key)) {
                table.Remove(key);
                return true;
            }

            return false;
        }


        /// <summary>
        /// Remove all string from the string table
        /// </summary>
        public void RemoveAllStrings() {
            table.Clear();
        }


        /// <summary>
        /// Serialize the string table in XML format
        /// </summary>
        /// <param name="path">The location where to save the serialized string table.</param>
        public bool SerializeTable(string path) {
            if(path == null) {
                throw new ArgumentNullException("path");
            }

            if(path.Length == 0) {
                return false;
            }

            StreamWriter writer = null;

            try {
                // convert the dictionary to list
                List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>();

                foreach(KeyValuePair<string, string> kvp in table) {
                    list.Add(kvp);
                }

                XmlSerializer serializer = new XmlSerializer(typeof(List<KeyValuePair<string, string>>));
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
        /// Deserialize the string table from XML format
        /// </summary>
        /// <param name="path">The location from where to load the string table.</param>
        public bool DeserializeTable(string path) {
            if(path == null) {
                throw new ArgumentNullException("path");
            }

            if(path.Length == 0) {
                return false;
            }

            StreamReader reader = null;

            try {
                // convert the dictionary to list
                List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>();

                XmlSerializer serializer = new XmlSerializer(typeof(List<KeyValuePair<string, string>>));
                reader = new StreamReader(path);

                if(reader.BaseStream.CanRead) {
                    list = (List<KeyValuePair<string, string>>)serializer.Deserialize(reader);

                    // convert to dictionary
                    foreach(KeyValuePair<string, string> kvp in list) {
                        table.Add(kvp.Key, kvp.Value);
                    }

                    list.Clear();
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
                if(reader != null && reader.BaseStream.CanRead) {
                    reader.Close();
                }
            }

            return true;
        }


        /// <summary>
        /// Loads all strings found in the resource
        /// </summary>
        /// <param name="path">The path of the resource from where to load the data.</param>
        public bool LoadFromResource(string path) {
            if(path == null) {
                throw new ArgumentNullException("path");
            }

            try {
                ResourceSet set = new ResourceSet(path);

                if(set == null) {
                    return false;
                }

                // extract the string resources
                IDictionaryEnumerator id = set.GetEnumerator();

                while(id.MoveNext()) {
                    if(id.Key is string && id.Value is string) {
                        AddString((string)id.Key, (string)id.Value);
                    }
                }
            }
            catch(Exception e) {
                Console.WriteLine(e.Message);
                return false;
            }

            return true;
        }

        #endregion
    }
}
