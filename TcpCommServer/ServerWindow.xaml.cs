using System;
using System.Collections.Concurrent;
using System.Windows;
using TcpCommLib;
using Timer = System.Timers.Timer;

namespace TcpCommServer {

    /// <summary>
    /// Interaction logic for ServerWindow.xaml
    /// </summary>
    public partial class ServerWindow : Window {

        #region Dependency properties

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text",typeof(string),typeof(ServerWindow));
        public static readonly DependencyProperty ConnectedProperty = DependencyProperty.Register("Connected",typeof(BoolWrapper),typeof(ServerWindow));

        #endregion

        #region Public properties

        public string Text {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty,value); }
        }

        public BoolWrapper Connected {
            get { return (BoolWrapper)GetValue(ConnectedProperty); }
            set { SetValue(ConnectedProperty,value); }
        }

        #endregion

        #region Private fields

        private Server _server;
        private Timer _logTimer;

        #endregion

        #region Construction

        public ServerWindow() {
            InitializeComponent();

            Text = String.Empty;
            SendText.Text = "Server says hello.";
            PortText.Text = "3001";

            Log.Clear();

            _logTimer = new Timer();
            _logTimer.Interval = 1000;
            _logTimer.AutoReset = false;
            _logTimer.Start();

            _logTimer.Elapsed += (s,e) => {
                try {
                    string line = Log.Read();

                    if(line != null) {
                        Dispatcher.Invoke(() => Text = line);
                    }

                } catch(Exception ex) {
                    Log.Write(ex.Message);
                }

                _logTimer.Start();
            };

            _server = new Server();
            _server.DataReceived += conn_DataReceived;
            _server.ConnectionLost += conn_ConnectionLost;

            listen();
            Connected = _server.IsConnected;
        }

        #endregion

        #region Helper functions

        private void listen() {
            try {
                _server.Listen(Convert.ToInt32(PortText.Text));
            } catch(Exception ex) {
                Log.Write(ex.ToString());
            }
        }

        private void conn_ConnectionLost() {
            Log.Write("Connection lost");
        }

        private void conn_DataReceived(string message) {
            Log.Write("Received: " + message);
        }

        #endregion

        #region UI interaction handlers

        private void StartBtn_Click(object sender,RoutedEventArgs e) {
            listen();
        }

        private void StopBtn_Click(object sender,RoutedEventArgs e) {
            _server.Stop();
        }

        private void RestartBtn_Click(object sender,RoutedEventArgs e) {
            StopBtn_Click(null,null);
            StartBtn_Click(null,null);
        }

        private void SendAsyncBtn_Click(object sender,RoutedEventArgs e) {
            _server.SendAsync(SendText.Text);
        }

        private void SendSyncBtn_Click(object sender,RoutedEventArgs e) {
            _server.Send(SendText.Text);
        }

        private void DisconnectBtn_Click(object sender,RoutedEventArgs e) {
            _server.Disconnect();
        }

        private void ClearBtn_Click(object sender,RoutedEventArgs e) {
            Log.Clear();
            Text = String.Empty;
        }

        #endregion
    }
}
