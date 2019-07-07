using Forward.Client.Common;
using Forward.Client.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Forward.Client.Services
{
    /// <summary>
    /// 心跳包服务
    /// </summary>
    public class HeartbeatService : IDisposable
    {
        private readonly ILogger _logger;
        private readonly ForwardServerOptions _forwardOptions;
        private readonly CancellationTokenSource _tokenSource;
        public bool Connectioned { get; private set; }
        private bool isStart;

        public HeartbeatService(IOptions<ForwardOptions> forwardOptions, ILogger<HeartbeatService> logger)
        {
            _forwardOptions = forwardOptions.Value.Forward;
            _logger = logger;
            _tokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// 接收到服务端请求连接通知事件
        /// </summary>
        public event Func<string, Task> RequestConnectionEvent;

        /// <summary>
        /// 启动心跳包服务
        /// </summary>
        public async Task Start()
        {
            if (isStart) return;
            isStart = true;

            await InternalStart();
        }

        /// <summary>
        /// 停止心跳服务
        /// </summary>
        public void Stop()
        {
            isStart = false;
            _tokenSource.Cancel();
        }

        /// <summary>
        /// 启动心跳包作业
        /// </summary>
        private async Task InternalStart()
        {
            _logger.LogWarning("心跳服务启动: {0}:{1}", _forwardOptions.ServerHost, _forwardOptions.ServerPort);
            while (isStart)
            {
                try
                {
                    Connectioned = true;
                    _logger.LogInformation("创建心跳包连接:{0}:{1}", _forwardOptions.ServerHost, _forwardOptions.ServerPort);

                    using (CancellationTokenSource tokenSource = CancellationTokenSource.CreateLinkedTokenSource(_tokenSource.Token))
                    using (var beatClient = new TcpClient(_forwardOptions.ServerHost, _forwardOptions.ServerPort))
                    {
                        var stream = beatClient.GetStream();
                        var sendTask = SendHeartbeat(tokenSource.Token, stream);
                        var receiveTask = Receive(tokenSource.Token, stream);

                        await Task.WhenAny(sendTask, receiveTask).ContinueWith(task =>
                        {
                            tokenSource.Cancel();
                            Connectioned = false;
                            if (task.Result?.Exception != null) throw task.Result.Exception;
                        });
                    }

                    _logger.LogInformation("心跳包连接退出:{0}:{1}", _forwardOptions.ServerHost, _forwardOptions.ServerPort);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "心跳包异常");
                }

                await Task.Delay(1000 * 10);
            }
        }

        /// <summary>
        /// 发送心跳包
        /// </summary>
        /// <param name="token"></param>
        /// <param name="stream"></param>
        /// <returns></returns>
        private async Task SendHeartbeat(CancellationToken token, Stream stream)
        {
            var connectionMessage = BuildHearbeatConnectionMessage();
            await stream.WriteAsync(connectionMessage, 0, connectionMessage.Length);

            var beatBuff = new byte[1];
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(1000 * 10);
                await stream.WriteAsync(beatBuff);
            }
        }

        /// <summary>
        /// 接收心跳包
        /// </summary>
        /// <param name="token"></param>
        /// <param name="stream"></param>
        /// <returns></returns>
        private async Task Receive(CancellationToken token, Stream stream)
        {
            var buff = new byte[1024];
            while (!token.IsCancellationRequested)
            {
                var index = await stream.ReadAsync(buff, 0, buff.Length, token);
                if (index <= 0)
                {
                    break;
                }
                var cmd = Encoding.ASCII.GetString(buff, 0, index);

                _logger.LogDebug("获得心跳包连接返回值:{0}", cmd);

                if (cmd.StartsWith("connection") && RequestConnectionEvent != null)
                {
                    _logger.LogInformation("收到访问请求:{0}", cmd);
                    var parameter = cmd.Split(":").LastOrDefault();
                    _ = RequestConnectionEvent(parameter);
                }
            }
        }

        /// <summary>
        /// 构建心跳连接消息
        /// </summary>
        /// <returns></returns>
        private byte[] BuildHearbeatConnectionMessage()
        {
            var args = new string[] {
                 _forwardOptions.ClientId,
                 DateTimeUtility.NowUnixTimeStamp.ToString(),
                 Guid.NewGuid().ToString("n")
            };
            var preSign = string.Concat(args) + _forwardOptions.ClientKey;
            var sign = Encrypt.MD5(preSign);

            var heartbeatString = $"heartbeat:{string.Join(",", args)},{sign}";
            return Encoding.ASCII.GetBytes(heartbeatString);
        }

        public void Dispose()
        {
            isStart = false;
            _tokenSource.Cancel();
            _tokenSource.Dispose();
        }
    }
}