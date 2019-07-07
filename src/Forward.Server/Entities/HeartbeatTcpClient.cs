using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Forward.Server.Entities
{
    public class HeartbeatTcpClient
    {
        public HeartbeatTcpClient(TcpClient tcpClient, string clientId)
        {
            this.TcpClient = tcpClient;
            this.ClientId = clientId;
        }

        public TcpClient TcpClient { get; private set; }
        public string ClientId { get; private set; }
    }
}
