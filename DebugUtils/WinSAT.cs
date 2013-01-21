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
using System.Reflection;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Xml;

namespace DebugUtils.Debugger {
    /// <summary>
    /// Provides functionality for querying WinSAT (Windows Experience Index) scores.
    /// </summary>
    /// <remarks>Available only in Windows Vista. The class isn't based on the WinSAT API, instead it uses the files from the WinSAT store.</remarks>
    public class WinSAT {
        #region Constants

        private const int RequiredOSMajorVersion = 6;
        private const string WinSATStoreLocation = @"Performance\WinSAT\DataStore\";

        #endregion

        #region Fields

        private bool loaded;

        #endregion

        #region Properties

        public static bool IsAvailable {
            get { return Environment.OSVersion.Version.Major >= RequiredOSMajorVersion; }
        }

        private double _computerScore;
        public double ComputerScore {
            get {
                if(loaded == false) {
                    if(LoadWinSAT() == false) {
                        return double.NaN;
                    }
                }

                return _computerScore;
            }
        }

        private double _cpuScore;
        public double CpuScore {
            get {
                if(loaded == false) {
                    if(LoadWinSAT() == false) {
                        return double.NaN;
                    }
                }

                return _cpuScore;
            }
        }

        private double _memoryScore;
        public double MemoryScore {
            get {
                if(loaded == false) {
                    if(LoadWinSAT() == false) {
                        return double.NaN;
                    }
                }

                return _memoryScore;
            }
        }

        private double _graphicsScore;
        public double GraphicsScore {
            get {
                if(loaded == false) {
                    if(LoadWinSAT() == false) {
                        return double.NaN;
                    }
                }

                return _graphicsScore;
            }
        }

        private double _gamingGraphicsScore;
        public double GamingGraphicsScore {
            get {
                if(loaded == false) {
                    if(LoadWinSAT() == false) {
                        return double.NaN;
                    }
                }

                return _gamingGraphicsScore;
            }
        }

        private double _diskScore;
        public double DiskScore {
            get {
                if(loaded == false) {
                    if(LoadWinSAT() == false) {
                        return double.NaN;
                    }
                }

                return _diskScore;
            }
        }

        public bool ScoresLoaded {
            get {
                return loaded;
            }
        }

        #endregion

        #region Private methods

        private bool LoadWinSATXML(string file) {
            if(file == null || file.Length == 0 || File.Exists(file) == false) {
                return false;
            }

            try {
                XmlDocument doc = new XmlDocument();
                doc.Load(file);
                XmlElement root = doc.DocumentElement;

                if(root != null) {
                    XmlNode scores = root.SelectSingleNode("WinSPR");

                    if(scores != null) {
                        // get the scores
                        _computerScore = Convert.ToDouble(scores.SelectSingleNode("SystemScore").InnerText);
                        _cpuScore = Convert.ToDouble(scores.SelectSingleNode("CpuScore").InnerText);
                        _memoryScore = Convert.ToDouble(scores.SelectSingleNode("MemoryScore").InnerText);
                        _graphicsScore = Convert.ToDouble(scores.SelectSingleNode("GraphicsScore").InnerText);
                        _gamingGraphicsScore = Convert.ToDouble(scores.SelectSingleNode("GamingScore").InnerText);
                        _diskScore = Convert.ToDouble(scores.SelectSingleNode("DiskScore").InnerText);

                        return true;
                    }
                }
            }
            catch(Exception e) {
                Console.WriteLine(e.Message);
                return false;
            }

            return false;
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Load the scores from the WinSAT (Windows Experience Index) store
        /// </summary>
        public bool LoadWinSAT() {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.System);

            if(string.IsNullOrEmpty(path)) {
                return false;
            }

            path = path.Substring(0, path.Length - "system32".Length - (path[path.Length - 1] == '\\' ? 1 : 0));
            path += WinSATStoreLocation;

            if(Directory.Exists(path)) {
                string[] files = Directory.GetFiles(path, "*.xml");

                if(files.Length > 0) {
                    // sort the files according to their creation time
                    // and choose the latest one
                    for(int i = 0; i < files.Length; i++) {
                        for(int j = 0; j < files.Length; j++) {
                            FileInfo fi = new FileInfo(files[i]);
                            FileInfo fj = new FileInfo(files[j]);

                            if(fi.CreationTime > fj.CreationTime) {
                                string temp = files[j];
                                files[j] = files[i];
                                files[i] = temp;
                            }
                        }
                    }

                    bool result = LoadWinSATXML(files[0]);

                    if(result) {
                        loaded = true;
                    }

                    return result;
                }
            }

            return false;
        }

        #endregion
    }
}
