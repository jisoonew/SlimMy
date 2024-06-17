using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SlimMy.ViewModel
{
    public class Server
    {
        ClientManager _clientManager = new ClientManager();

        public Server()
        {
            Task serverStart = Task.Run(() =>
            {
                ServerRun();
            });
        }

        private void ServerRun()
        {
            TcpListener listner = new TcpListener(new IPEndPoint(IPAddress.Any, 9999));

            listner.Start();

            while(true)
            {
                Task<TcpClient> acceptTask = listner.AcceptTcpClientAsync();

                acceptTask.Wait();

                TcpClient newClient = acceptTask.Result;

                _clientManager.AddClient(newClient);
            }
        }
    }
}
