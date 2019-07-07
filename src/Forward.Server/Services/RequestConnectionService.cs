using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Forward.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Forward.Server.Services
{
    /// <summary>
    /// 请求连接服务
    /// </summary>
    public class RequestConnectionService : IDisposable
    {
        private readonly object _lock = new object();
        private readonly CancellationTokenSource _tokenSource;
        private readonly HashSet<TcpClient> _tcpClients;
        private TcpListener _tcpListener;
        private bool isStart;
        public bool Connectioned { get; private set; }

        public event Func<TcpClient, CancellationToken, Task> RequestConnectionEvent;

        public RequestConnectionService()
        {
            _tcpClients = new HashSet<TcpClient>();
            this._tokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// 启动连接端口侦听
        /// </summary>
        public async Task Start(int port)
        {
            if (isStart) return;
            isStart = true;

            _tcpListener = TcpListener.Create(port);
            await InternalStart();
        }

        /// <summary>
        /// 停止连接端口侦听
        /// </summary>
        public void Stop()
        {
            isStart = false;
            _tokenSource.Cancel();
        }

        /// <summary>
        /// 启动连接端口侦听
        /// </summary>
        private async Task InternalStart()
        {
            _tcpListener.Start();
            while (isStart)
            {
                var tcp = await _tcpListener.AcceptTcpClientAsync();
                lock (_lock)
                {
                    _tcpClients.Add(tcp);
                }
                _ = RequestConnectionHandle(tcp, _tokenSource.Token);
            }
        }

        /// <summary>
        /// 连接请求处理
        /// </summary>
        /// <param name="tcp"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task RequestConnectionHandle(TcpClient tcp, CancellationToken token)
        {
            if (RequestConnectionEvent != null)
            {
                await RequestConnectionEvent(tcp, token);
            }

            lock (_lock)
            {
                _tcpClients.Remove(tcp);
            }
            tcp.Dispose();
        }

        public void Dispose()
        {
            isStart = false;
            _tcpListener?.Stop();
            _tokenSource.Cancel();
            _tokenSource.Dispose();

            lock (_lock)
            {
                foreach (var client in _tcpClients)
                {
                    client.Dispose();
                }
                _tcpClients.Clear();
            }
        }
    }
}