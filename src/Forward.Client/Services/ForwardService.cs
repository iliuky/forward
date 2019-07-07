using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Forward.Client.Entities;
using Microsoft.Extensions.Options;

namespace Forward.Client.Services
{
    /// <summary>
    /// 数据转发服务
    /// </summary>
    public class ForwardService : IDisposable
    {
        public bool Connectioned { get; private set; }
        private readonly TcpClient _remote;
        private readonly TcpClient _local;
        private readonly CancellationTokenSource _tokenSource;
        private bool isStart;

        public ForwardService(TcpClient tcpClient1, TcpClient tcpClient2)
        {
            _remote = tcpClient1;
            _local = tcpClient2;

            _tokenSource = new CancellationTokenSource();
        }
        /// <summary>
        /// 启动转发
        /// </summary>
        /// <returns></returns>
        public async Task Start(ConnectionOptions connectionOptions)
        {
            if (isStart) return;
            isStart = true;

            await InternalStart(connectionOptions);
        }

        /// <summary>
        /// 启动转发
        /// </summary>
        /// <returns></returns>
        private async Task InternalStart(ConnectionOptions connectionOptions)
        {
            await _remote.ConnectAsync(connectionOptions.ServerHost, connectionOptions.ServerPort);
            await SendRemoteMessage(connectionOptions.ForwardId);
            await _local.ConnectAsync(connectionOptions.LocalHost, connectionOptions.LocalPort);
            Connectioned = true;

            var task1 = Forward(_remote, _local, _tokenSource.Token);
            var task2 = Forward(_local, _remote, _tokenSource.Token);

            await Task.WhenAny(task1, task2).ContinueWith(task =>
            {
                Connectioned = false;
                _tokenSource.Cancel();
                _remote.Close();
                _local.Close();

                if (task.Result?.Exception != null) throw task.Result.Exception;
            });
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
        /// 向远程发送转发请求
        /// </summary>
        /// <param name="connectionId"></param>
        /// <returns></returns>
        private async Task SendRemoteMessage(string connectionId)
        {
            var stream = _remote.GetStream();
            var message = "connection:" + connectionId;
            var buff = System.Text.Encoding.ASCII.GetBytes(message);
            await stream.WriteAsync(buff, 0, buff.Length);
        }

        /// <summary>
        /// 进行数据转发
        /// </summary>
        /// <param name="tcpClient1"></param>
        /// <param name="tcpClient2"></param>
        /// <returns></returns>
        private async Task Forward(TcpClient tcpClient1, TcpClient tcpClient2, CancellationToken token)
        {
            var s1 = tcpClient1.GetStream();
            var s2 = tcpClient2.GetStream();
            var buff = new byte[1024];
            while (!token.IsCancellationRequested)
            {
                var index = await s1.ReadAsync(buff, 0, buff.Length, token);
                if (index <= 0)
                {
                    break;
                }
                await s2.WriteAsync(buff, 0, index, token);
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