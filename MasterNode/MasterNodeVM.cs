using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Pipes;
using System.Windows.Input;
using System.ComponentModel;
using System.Windows;

namespace MasterNode
{
    public class MasterNodeVM : INotifyPropertyChanged
    {
        private const string PipeName = "MasterPipe";
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(3); // Maximum of 3 concurrent clients
        private int _onlineClientsCount;
        private Dictionary<NamedPipeServerStream, StreamWriter> _clientConnected = new Dictionary<NamedPipeServerStream, StreamWriter>();
        private readonly static object obj = new object();

        public MasterNodeVM()
        {
            Task.Run(() => InitializePipeServer());
        }

        private async Task InitializePipeServer()
        {
            try
            {
                while (true)
                {
                    _semaphore.Wait(); // Wait until semaphore allows more connections
                     NamedPipeServerStream pipeServer = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 3, PipeTransmissionMode.Message);
                     await Task.Run(() => WaitForClient(pipeServer));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating pipe server: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task WaitForClient(NamedPipeServerStream pipeServer)
        {
            await pipeServer.WaitForConnectionAsync();
           
            lock (obj)
            {
                OnlineClientsCount++;
            }

            await Task.Run(() => HandleClientConnection(pipeServer));
        }

        private async void HandleClientConnection(NamedPipeServerStream pipeServer)
        {
            StreamWriter writer = new StreamWriter(pipeServer);
            _clientConnected.Add(pipeServer, writer);

            try
            {
                while (true && pipeServer.IsConnected)
                {
                    writer.AutoFlush = true;
                    writer.WriteLine("");

                    await Task.Delay(20000);

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in HandleClientConnection: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                lock (obj)
                {
                    OnlineClientsCount--;
                }

                _clientConnected.Remove(pipeServer);
                pipeServer.Close();
                _semaphore.Release(); // Release semaphore to allow another connection
            }
        }

        private void SendNotification()
        {
            try
            {
                foreach (KeyValuePair<NamedPipeServerStream, StreamWriter> pipeServerStream in _clientConnected)
                {
                    if (pipeServerStream.Key.IsConnected)
                    {
                        StreamWriter writer = pipeServerStream.Value;
                        writer.WriteLine("Notification from Master!");                      
                    }
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in SendNotification: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
   
        public ICommand SendNotificationCommand => new RelayCommand(SendNotification);

        public int OnlineClientsCount
        {
            get { return _onlineClientsCount; }
            set
            {
                if (_onlineClientsCount != value)
                {
                    _onlineClientsCount = value;
                    OnPropertyChanged(nameof(OnlineClientsCount));

                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
    }
}
