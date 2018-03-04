using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TcpCommLib
{
    public class Receiver
    {
        public DateTime LastReceived { get; set; }

        public delegate void DataReceivedHandler(string message);
        public event DataReceivedHandler DataReceived;

        private NetworkStream _stream;
        private Thread _thread;
        private bool _stop;
        private ManualResetEvent _shutdownEvent;

        public Receiver(NetworkStream stream) {
            _stream = stream;
            _stop = true;
            _shutdownEvent = new ManualResetEvent(false);
        }

        public void Start() {
            if(!_stop) {
                Stop();
            }

            _stop = false;
            _thread = new Thread(run);
            _thread.Start();
        }

        public void Stop() {
            if(!_stop) {
                _stop = true;
                _shutdownEvent.WaitOne();
                _shutdownEvent.Close();
                _shutdownEvent = null;
            }
        }

        private void run() {
            try {
                while(!_stop) {
                    try {
                        if(!_stream.DataAvailable) {
                            Thread.Sleep(1);
                        } else {
                            string message = null;
                            byte[] buffer = new byte[16];
                            int bytesRead = 0;
                            int bytesTotal = 0;

                            while(true) {
                                int bytes = _stream.Read(buffer,bytesRead,1);

                                if(bytes > 0) {
                                    bytesRead += bytes;
                                } else {
                                    _stop = true;
                                    break;
                                }

                                if(buffer[bytesRead - 1] == ':') {
                                    string len = Encoding.UTF8.GetString(buffer,0,bytesRead - 1);

                                    if(!String.IsNullOrEmpty(len)) {
                                        bytesTotal = Convert.ToInt32(len);
                                    }

                                    break;
                                }
                            }

                            if(bytesTotal > 0) {
                                bytesRead = 0;
                                buffer = new byte[bytesTotal];

                                while(bytesRead < bytesTotal) {
                                    int bytes = _stream.Read(buffer,bytesRead,bytesTotal - bytesRead);

                                    if(bytes > 0) {
                                        bytesRead += bytes;
                                    } else {
                                        _stop = true;
                                        break;
                                    }
                                }

                                message = Encoding.UTF8.GetString(buffer,0,bytesTotal);
                            }

                            if(message != null) {
                                LastReceived = DateTime.Now;

                                if(DataReceived != null) {
                                    foreach(DataReceivedHandler handler in DataReceived.GetInvocationList()) {
                                        handler.BeginInvoke(message,null,null);
                                    }
                                }
                            }
                        }
                    } catch(IOException ex) {
                        Log.Write(ex.ToString());
                    }
                }
            } catch(Exception ex) {
                Log.Write(ex.ToString());
            } finally {
                _shutdownEvent.Set();
            }
        }
    }
}
