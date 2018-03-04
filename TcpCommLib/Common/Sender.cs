using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TcpCommLib
{
    public class Sender
    {
        public DateTime LastSent { get; set; }

        private NetworkStream _stream;
        private Thread _thread;
        private bool _stop;
        private ManualResetEvent _shutdownEvent;
        private ConcurrentQueue<MessageContainer> _writeQueue;

        public Sender(NetworkStream stream) {
            _stream = stream;
            _stop = true;
            _shutdownEvent = new ManualResetEvent(false);
            _writeQueue = new ConcurrentQueue<MessageContainer>();
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

        public bool Send(string message) {
            if(!String.IsNullOrEmpty(message)) {
                var finishedEvent = new ManualResetEvent(false);
                var container = new MessageContainer(message,finishedEvent);

                _writeQueue.Enqueue(container);

                bool finished = finishedEvent.WaitOne(10000);
                finishedEvent.Close();

                if(finished && container.Exception == null) {
                    return true;
                }
            }

            return false;
        }

        public void SendAsync(string message) {
            if(!String.IsNullOrEmpty(message)) {
                _writeQueue.Enqueue(new MessageContainer(message));
            }
        }

        private void run() {
            try {
                while(!_stop) {
                    if(_writeQueue.IsEmpty) {
                        Thread.Sleep(1);
                    } else {
                        MessageContainer container = null;

                        if(_writeQueue.TryDequeue(out container)) {
                            string message = container.Message;
                            ManualResetEvent sentEvent = container.SentEvent;

                            if(!String.IsNullOrEmpty(message)) {
                                message = String.Format("{0}:{1}",Encoding.UTF8.GetBytes(message).Length,message);
                                byte[] buffer = Encoding.UTF8.GetBytes(message);

                                try {
                                    _stream.Write(buffer,0,buffer.Length);
                                    LastSent = DateTime.Now;
                                } catch(IOException ex) {
                                    container.Exception = ex;
                                }

                                if(sentEvent != null && !sentEvent.SafeWaitHandle.IsClosed) {
                                    sentEvent.Set();
                                }
                            }
                        }
                    }
                }
            } catch(Exception ex) {
                Log.Write(ex.ToString());
            } finally {
                _shutdownEvent.Set();
            }
        }

        private class MessageContainer {
            public string Message;
            public ManualResetEvent SentEvent;
            public Exception Exception;

            public MessageContainer(string message) {
                Message = message;
            }

            public MessageContainer(string message,ManualResetEvent sentEvent) {
                Message = message;
                SentEvent = sentEvent;
            }
        }
    }
}
