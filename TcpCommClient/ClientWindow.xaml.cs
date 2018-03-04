using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Windows;
using TcpCommLib;
using Timer = System.Timers.Timer;

namespace TcpCommClient {

    /// <summary>
    /// Interaction logic for ClientWindow.xaml
    /// </summary>
    public partial class ClientWindow : Window {

        #region Dependency properties

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text",typeof(string),typeof(ClientWindow));
        public static readonly DependencyProperty ConnectedProperty = DependencyProperty.Register("Connected",typeof(BoolWrapper),typeof(ClientWindow));

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

        private Client _client;
        private Timer _logTimer;

        #endregion

        #region Construction

        public ClientWindow() {
            InitializeComponent();

            Text = String.Empty;
            SendText.Text = "Client says hello.";
            IpText.Text = "127.0.0.1";
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

                } catch (Exception ex) {
                    Log.Write(ex.Message);
                }

                _logTimer.Start();
            };

            _client = new Client(); //new Client(_logQueue);
            _client.DataReceived += conn_DataReceived;
            _client.ConnectionLost += conn_ConnectionLost;

            Connected = _client.IsConnected;
        }

        #endregion

        #region Helper functions

        private void conn_DataReceived(string message) {
            Log.Write("Received: " + message);
        }

        private void conn_ConnectionLost() {
            Log.Write("Connection lost");
        }

        #endregion

        #region UI interaction functions

        private void ConnectBtn_Click(object sender,RoutedEventArgs e) {
            try {
                _client.ConnectAsync(IpText.Text,Convert.ToInt32(PortText.Text));
            } catch(Exception ex) {
                Log.Write(ex.ToString());
            }
        }

        private void DisconnectBtn_Click(object sender,RoutedEventArgs e) {
            _client.Disconnect();
        }

        private void SendAsyncBtn_Click(object sender,RoutedEventArgs e) {
            _client.SendAsync(SendText.Text);
        }

        private void SendSyncBtn_Click(object sender,RoutedEventArgs e) {
            _client.Send(SendText.Text);
        }

        private void ClearBtn_Click(object sender,RoutedEventArgs e) {
            Log.Clear();
            Text = String.Empty;
        }

        #endregion
    }
}
