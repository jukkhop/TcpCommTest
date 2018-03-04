using System;
using System.Net;
using System.Net.Sockets;

namespace TcpCommLib
{
    public class Server : TcpBase
    {
        private TcpListener _listener;
        private int _listenPort;

        public Server() {
        }

        public void Listen(int port) {
            if(_listener != null) {
                Stop();
            }

            _listenPort = port;

            try {
                _listener = new TcpListener(IPAddress.Any,_listenPort);
                _listener.Start();
                _listener.BeginAcceptTcpClient(new AsyncCallback(acceptClient),_listener);

            } catch(Exception ex) {
                Log.Write(ex.ToString());
            }
        }

        public void Stop() {
            if(_listener != null) {
                Disconnect();

                try {
                    _listener.Stop();
                } catch(SocketException) {
                }

                _listener = null;
            }
        }

        private void acceptClient(IAsyncResult result) {
            try {
                TcpClient client = null;

                // For some reason this callback gets invoked after the listener is
                // Stop()'d, so if any of the below exceptions are thrown, simply exit.
                try {
                    // Accept current connection
                    client = _listener.EndAcceptTcpClient(result);

                    // Continue accepting new connections
                    _listener.BeginAcceptTcpClient(new AsyncCallback(acceptClient),_listener);

                } catch(ObjectDisposedException) {
                    return;
                } catch(NullReferenceException) {
                    return;
                } catch(SocketException) {
                    return;
                }

                if(!IsConnected.Value) {
                    stopThreads();
                    IsConnected.Value = true;

                    _client = client;
                    _stream = client.GetStream();

                    _receiver = new Receiver(_stream);
                    _receiver.DataReceived += onDataReceived;
                    _receiver.Start();

                    _sender = new Sender(_stream);
                    _sender.Start();
                    _sender.SendAsync(CONNECTION_OK);

                    startKeepAlive();

                } else {
                    // Another client is already connected (or has yet to time out)
                    NetworkStream stream = client.GetStream();
                    Sender sender = new Sender(stream);
                    sender.Start();
                    sender.Send(SERVER_BUSY);
                    sender.Stop();
                    stream.Close();
                    client.Close();
                }

            } catch(Exception ex) {
                Log.Write(ex.ToString());
            }
        }
    }
}
