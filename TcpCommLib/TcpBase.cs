using System;
using System.Net.Sockets;
using Timer = System.Timers.Timer;

namespace TcpCommLib
{
    public class TcpBase
    {
        public const string CONNECTION_OK = "CONNECTION_OK";
        public const string CONNECTION_CLOSE = "CONNECTION_CLOSE";
        public const string SERVER_BUSY = "SERVER_BUSY";

        public delegate void DataReceivedHandler(string message);
        public delegate void ConnectionLostHandler();

        public event DataReceivedHandler DataReceived;
        public event ConnectionLostHandler ConnectionLost;

        public BoolWrapper IsConnected = new BoolWrapper(false);

        protected TcpClient _client;
        protected NetworkStream _stream;
        protected Sender _sender;
        protected Receiver _receiver;
        protected Timer _keepAliveTimer;
        protected volatile bool _keepAliveTimerStop;

        private const string _keepAliveRequest = "PING";
        private const string _keepAliveResponse = "PONG";
        private const int _keepAliveIntervalMs = 5000;
        private const int _timeoutMs = 15000;

        public TcpBase() {
        }

        public bool Send(string message) {
            if(_sender != null && !String.IsNullOrEmpty(message)) {
                return _sender.Send(message);
            }

            return false;
        }

        public void SendAsync(string message) {
            if(_sender != null && !String.IsNullOrEmpty(message)) {
                _sender.SendAsync(message);
            }
        }

        public void Disconnect(bool sendCloseSignal = true) {
            if(!IsConnected.Value) {
                return;
            }

            if(sendCloseSignal) {
                _sender.Send(CONNECTION_CLOSE);
            }

            stopThreads();

            if(_client != null) {
                _client.Close();
                _client = null;
            }

            if(_stream != null) {
                _stream.Close();
                _stream = null;
            }

            IsConnected.Value = false;
        }

        protected void onDataReceived(string message) {
            if(String.IsNullOrEmpty(message)) {
                return;
            }

            if(message == _keepAliveRequest) {
                SendAsync(_keepAliveResponse);

            } else if(message == _keepAliveResponse) {
                // Do nothing

            } else {
                if(DataReceived != null) {
                    DataReceived(message);
                }

                if(message == CONNECTION_CLOSE || message == SERVER_BUSY) {
                    Disconnect(false);
                }
            }
        }

        protected void startKeepAlive() {
            if(_keepAliveTimer != null) {
                return;
            }

            _keepAliveTimerStop = false;

            _keepAliveTimer = new Timer();
            _keepAliveTimer.Interval = _keepAliveIntervalMs;
            _keepAliveTimer.AutoReset = false;
            _keepAliveTimer.Elapsed += (s,e) => {
                try {
                    if(_sender != null && _receiver != null) {
                        SendAsync(_keepAliveRequest);

                        TimeSpan timeSinceSent = DateTime.Now - _sender.LastSent;
                        TimeSpan timeSinceReceived = DateTime.Now - _receiver.LastReceived;

                        if(timeSinceSent.TotalMilliseconds > _timeoutMs ||
                           timeSinceReceived.TotalMilliseconds > _timeoutMs) {

                            Disconnect();

                            if(ConnectionLost != null) {
                                ConnectionLost();
                            }

                        } else if(!_keepAliveTimerStop) {
                            _keepAliveTimer.Start();
                        }
                    }
                } catch(Exception ex) {
                    Log.Write(ex.ToString());
                }
            };
            _keepAliveTimer.Start();
        }

        protected void stopThreads() {
            if(_receiver != null) {
                _receiver.Stop();
                _receiver.DataReceived -= onDataReceived;
                _receiver = null;
            }

            if(_sender != null) {
                _sender.Stop();
                _sender = null;
            }

            if(_keepAliveTimer != null) {
                _keepAliveTimerStop = true;
                _keepAliveTimer.Stop();
                _keepAliveTimer.Close();
                _keepAliveTimer.Dispose();
                _keepAliveTimer = null;
            }
        }
    }
}
