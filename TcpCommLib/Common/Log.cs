using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Timers;
using Timer = System.Timers.Timer;

namespace TcpCommLib {
    public static class Log {

        private static ConcurrentQueue<LogEntry> _logQueue;
        private static Timer _writeTimer;

        static Log() {
            _logQueue = new ConcurrentQueue<LogEntry>();

            _writeTimer = new Timer();
            _writeTimer.Elapsed += writeTimer_Elapsed;
            _writeTimer.Interval = 1000;
            _writeTimer.AutoReset = false;
            _writeTimer.Start();
        }

        private static void writeTimer_Elapsed(object sender,ElapsedEventArgs e) {
            try {
                while(_logQueue.Count > 0) {
                    LogEntry entry = null;

                    if(_logQueue.TryDequeue(out entry)) {
                        if(entry != null) {
                            using(StreamWriter w = File.AppendText(entry.LogName + ".log")) {
                                w.WriteLine("{0} {1}",entry.Time.ToString("yyyy-MM-dd HH:mm:ss.ffffff"),entry.Line);
                            }
                        }
                    }
                }
            } catch {
            }

            _writeTimer.Start();
        }

        public static void Clear(string optionalLogName = null) {
            var file = String.IsNullOrEmpty(optionalLogName) ? Process.GetCurrentProcess().ProcessName : optionalLogName;
            var path = file + ".log";

            File.WriteAllText(path,String.Empty);
        }

        public static string Read(string optionalLogName = null) {
            var file = String.IsNullOrEmpty(optionalLogName) ? Process.GetCurrentProcess().ProcessName : optionalLogName;
            var path = file + ".log";

            if(File.Exists(path)) {
                var log = File.ReadAllText(path);
                return log;
            }

            return null;
        }

        // Asynchronous version
        public static void Write(string line,string optionalLogName = null) {
            var entry = new LogEntry {
                Time = DateTime.Now,
                Line = line,
                LogName = String.IsNullOrEmpty(optionalLogName) ? Process.GetCurrentProcess().ProcessName : optionalLogName
            };

            _logQueue.Enqueue(entry);
        }

        // Synchronous version
        public static void WriteSyncronous(string line,string optionalLogName = null) {
            var entry = new LogEntry {
                Time = DateTime.Now,
                Line = line,
                LogName = String.IsNullOrEmpty(optionalLogName) ? Process.GetCurrentProcess().ProcessName : optionalLogName
            };
            
            using(StreamWriter w = File.AppendText(entry.LogName + ".log")) {
                w.WriteLine("{0} {1}",entry.Time.ToString("yyyy-MM-dd HH:mm:ss.ffffff"),entry.Line);
            }
        }

        private class LogEntry {
            public DateTime Time;
            public string Line;
            public string LogName;
        }
    }
}
