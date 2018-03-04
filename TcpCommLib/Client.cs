using System;
using System.Net;
using System.Net.Sockets;

namespace TcpCommLib
{
    public class Client : TcpBase
    {
        public Client() {
        }

        public void ConnectAsync(string ip,int port) {
            try {
                if(IsConnected.Value) {
                    Disconnect();
                }

                _client = new TcpClient();

                var address = IPAddress.Parse(ip);
                var callback = new AsyncCallback(handleConnect);

                _client.BeginConnect(address,port,callback,_client);

            } catch(Exception ex) {
                Log.Write(ex.ToString());
            }
        }

        private void handleConnect(IAsyncResult result) {
            try {
                _client.EndConnect(result);

                if(_client.Connected) {
                    stopThreads();
                    IsConnected.Value = true;

                    _stream = _client.GetStream();

                    _receiver = new Receiver(_stream);
                    _receiver.DataReceived += onDataReceived;
                    _receiver.Start();

                    _sender = new Sender(_stream);
                    _sender.Start();

                    startKeepAlive();
                }

            } catch(SocketException) {
            } catch(ArgumentException) {
            } catch(Exception ex) {
                Log.Write(ex.ToString());
            }
        }
    }
}
