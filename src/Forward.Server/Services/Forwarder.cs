using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Forward.Server.Common;

namespace Forward.Server.Services
{
    /// <summary>
    /// 数据转发器
    /// </summary>
    public class Forwarder : IDisposable
    {
        public bool Connectioned { get; private set; }
        private readonly TcpClient _requester;
        private readonly TcpClient _server;
        private readonly CancellationTokenSource _tokenSource;
        private readonly int _receiveTimeout = 1000 * 60;
        private readonly int _sendTimeout = 1000 * 60;
        private bool isStart;

        public Forwarder(TcpClient requester, TcpClient server)
        {
            _requester = requester;
            _server = server;
            _tokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// 启动转发
        /// </summary>
        /// <returns></returns>
        public async Task Start()
        {
            if (isStart) return;
            isStart = true;

            await InternalStart();
        }

        /// <summary>
        /// 停止转发服务
        /// </summary>
        public void Stop()
        {
            isStart = false;
            _tokenSource.Cancel();
        }

        /// <summary>
        /// 启动转发
        /// </summary>
        /// <returns></returns>
        private async Task InternalStart()
        {
            var task1 = Forward(_requester, _server, _tokenSource.Token);
            var task2 = Forward(_server, _requester, _tokenSource.Token);

            Connectioned = true;
            await Task.WhenAny(task1, task2).ContinueWith(task =>
            {
                Connectioned = false;
                _tokenSource.Cancel();
                _requester.Close();
                _server.Close();
                if (task.Result?.Exception != null) throw task.Result.Exception;
            });
        }

        /// <summary>
        /// 进行数据转发
        /// </summary>
        /// <param name="tcpClient1"></param>
        /// <param name="tcpClient2"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task Forward(TcpClient tcpClient1, TcpClient tcpClient2, CancellationToken token)
        {
            var s1 = tcpClient1.GetStream();
            var s2 = tcpClient2.GetStream();
            var buff = new byte[1024];

            while (!token.IsCancellationRequested)
            {
                var index = await s1.ReadAsync(buff, 0, buff.Length, token).TimeoutAfter(TimeSpan.FromMilliseconds(_receiveTimeout), token);
                if (index <= 0)
                {
                    break;
                }
                await s2.WriteAsync(buff, 0, index, token).TimeoutAfter(TimeSpan.FromMilliseconds(_sendTimeout), token);
            }
        }

        public void Dispose()
        {
            isStart = false;
            _tokenSource.Cancel();
            _tokenSource.Dispose();
        }
    }
}