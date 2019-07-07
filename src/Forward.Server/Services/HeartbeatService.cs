using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Forward.Server.Common;
using Forward.Server.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Forward.Server.Services
{
    /// <summary>
    /// 心跳服务
    /// </summary>
    public class HeartbeatService : IDisposable
    {
        private readonly byte[] _okBuff;
        private readonly object _lock = new object();
        private readonly ILogger _logger;
        private readonly CancellationTokenSource _tokenSource;
        private TcpListener _tcpListener;
        private readonly HashSet<TcpClient> _tcpClients;
        private readonly HashSet<HeartbeatTcpClient> _heartbeatTcpClients;
        private readonly IMemoryCache _cache;
        private readonly ForwardOptions _forwardOptions;
        private bool isStart;
        public bool Connectioned { get; private set; }

        public HeartbeatService(ILogger<HeartbeatService> logger, IMemoryCache cache, IOptions<ForwardOptions> options)
        {
            _logger = logger;
            _tokenSource = new CancellationTokenSource();
            _tcpClients = new HashSet<TcpClient>();
            _heartbeatTcpClients = new HashSet<HeartbeatTcpClient>();
            _cache = cache;
            _forwardOptions = options.Value;
            _okBuff = Encoding.ASCII.GetBytes("1");
        }

        /// <summary>
        /// 启动心跳包服务
        /// </summary>
        public async Task Start(int port)
        {
            if (isStart) return;
            isStart = true;

            _tcpListener = TcpListener.Create(port);
            await InternalStart();
        }

        /// <summary>
        /// 停止心跳服务
        /// </summary>
        public void Stop()
        {
            isStart = false;
            _tcpListener.Stop();
            _tokenSource.Cancel();
        }

        /// <summary>
        /// 启动心跳服务
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
                _ = CheckTcpClientType(tcp, _tokenSource.Token);
            }
        }

        /// <summary>
        /// 获取客户端连接 
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<(TcpClient RequestTcpClient, Action EndNotice)> GetRequestTcpClient(CancellationToken token, ForwardRuleDetail forwardRuleDetail)
        {
            var forwardId = Guid.NewGuid().ToString("n");
            var tcp = _heartbeatTcpClients.FirstOrDefault(u => u.ClientId == forwardRuleDetail.ClientId);
            var result = default(TcpClient);

            var tcsEndNotice = new TaskCompletionSource<TcpClient>();
            if (tcp != null)
            {
                var tcs = new TaskCompletionSource<TcpClient>();
                var cacheKey = $"/CancellationToken/{forwardId}";
                var requestInfo = new ForwardRequesInfo
                {
                    CompletionSource = tcs,
                    EndNotice = tcsEndNotice.Task
                };
                _cache.Set(cacheKey, requestInfo, TimeSpan.FromSeconds(20));

                var message = $"connection:{forwardId},{forwardRuleDetail.RuleId}";
                var buff = Encoding.ASCII.GetBytes(message);
                _logger.LogInformation("请求连接客户端连接, 回复客户端消息:{0}", message);
                await tcp.TcpClient.GetStream().WriteAsync(buff, 0, buff.Length, token);
                _logger.LogInformation("请求连接客户端连接, 回复客户端消息:{0}, 成功", message);

                await Task.WhenAny(tcs.Task.ContinueWith(t => { result = t.Result; }), Task.Delay(10 * 1000, token));
            }
            return (result, () => tcsEndNotice.SetResult(result));
        }

        /// <summary>
        /// 校验请求类型
        /// </summary>
        /// <param name="client"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task CheckTcpClientType(TcpClient client, CancellationToken token)
        {
            var remoteEndPoint = string.Empty;
            try
            {
                remoteEndPoint = client.Client.RemoteEndPoint.ToString();
                _logger.LogInformation("心跳端口收到连接:{0}", remoteEndPoint);

                using (var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token))
                {
                    cancellationTokenSource.CancelAfter(5000);
                    var buff = new byte[1024];
                    var index = 0;
                    using (cancellationTokenSource.Token.Register(() => client.Close()))
                    {
                        index = await client.GetStream().ReadAsync(buff, 0, buff.Length, cancellationTokenSource.Token);
                    }

                    if (index > 0)
                    {
                        var cmd = Encoding.ASCII.GetString(buff, 0, index);
                        _logger.LogInformation("心跳端口收到连接:{0}, cmd:{1}", remoteEndPoint, cmd);
                        if (cmd.StartsWith("heartbeat"))
                        {
                            await CheckHeartbeat(cmd, client, token);
                        }
                        else if (cmd.StartsWith("connection"))
                        {
                            await ConnectionHandle(cmd, client);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "系统异常:{0}", remoteEndPoint);
            }

            _tcpClients.Remove(client);
            client.Dispose();
        }

        /// <summary>
        /// 连接请求处理
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="tcpClient"></param>
        /// <returns></returns>
        private async Task ConnectionHandle(string cmd, TcpClient tcpClient)
        {
            var remoteEndPoint = string.Empty;
            try
            {
                remoteEndPoint = tcpClient.Client.RemoteEndPoint.ToString();
                var forwardId = cmd.Split(":").Last();
                var cacheKey = $"/CancellationToken/{forwardId}";
                var requesInfo = _cache.Get<ForwardRequesInfo>(cacheKey);
                if (requesInfo == null)
                {
                    _logger.LogInformation("连接请求:{0}, {1}, 未在缓存中查询到 ForwardRequesInfo 对象", cmd, remoteEndPoint);
                    _logger.LogInformation("现存活动心跳包连接:{0} 个", _tcpClients.Count);
                    return;
                }
                _cache.Remove(cacheKey);

                if (!requesInfo.CompletionSource.TrySetResult(tcpClient))
                {
                    _logger.LogInformation("连接请求:{0}, {1},设置 TaskCompletionSource 返回值失败", cmd, remoteEndPoint);
                    return;
                }

                await requesInfo.EndNotice;
                _logger.LogInformation("请求端断开连接:{0}, {1},设置 TaskCompletionSource 关闭客户端连接", cmd, remoteEndPoint);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "连接请求:{0}, {1}, 出现异常", cmd, remoteEndPoint);
            }
        }

        /// <summary>
        /// 校验心跳包连接是否断开
        /// </summary>
        /// <param name="client"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task CheckHeartbeat(string cmd, TcpClient client, CancellationToken token)
        {
            var remoteEndPoint = client.Client.RemoteEndPoint.ToString();
            if (!CheckHearbeatConnectionMessage(cmd, out var clientId)) return;
            if (_heartbeatTcpClients.Any(u => u.ClientId == clientId))
            {
                _logger.LogInformation("tcpClient:{0}, clientId:{0}, 已经存在心跳包对象, 拒绝本次连接", remoteEndPoint, clientId);
                return;
            }

            var heartbeat = new HeartbeatTcpClient(client, clientId);
            try
            {
                _heartbeatTcpClients.Add(heartbeat);

                _logger.LogInformation("接受到一个心跳请求tcpClient:{0}", remoteEndPoint);
                var buff = new byte[1024];
                var stream = client.GetStream();
                while (isStart && !token.IsCancellationRequested)
                {
                    var index = await stream.ReadAsync(buff, 0, buff.Length, token);
                    if (index <= 0) break;

                    await stream.WriteAsync(_okBuff, 0, _okBuff.Length, token);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "tcpClient:{0} 读取心跳包出现异常", remoteEndPoint);
            }

            _heartbeatTcpClients.Remove(heartbeat);
            _logger.LogInformation("tcpClient:{0} 心跳包连接断开", remoteEndPoint);
        }

        /// <summary>
        /// 校验心跳包签名
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        private bool CheckHearbeatConnectionMessage(string cmd, out string clientId)
        {
            clientId = string.Empty;
            var message = cmd.Split(":").Last();
            var args = message.Split(",");

            if (args.Length != 4)
            {
                _logger.LogInformation("心跳包签名验证格式错误, cmd:{0} ", cmd);
                return false;
            }

            var id = args[0];
            int.TryParse(args[1], out var timeStamp);
            var random = args[2];
            var clientSign = args[3];

            var clientInfo = _forwardOptions.Clients.FirstOrDefault(u => u.ClientId == id);
            if (clientInfo == null)
            {
                _logger.LogInformation("心跳包签名验证客户端不存在, cmd:{0}", cmd);
                return false;
            }

            if (Math.Abs(DateTimeUtility.NowUnixTimeStamp - timeStamp) > 60 * 5)
            {
                _logger.LogInformation("心跳包签名验证已过期, cmd:{0} ", cmd);
                return false;
            }

            var preSign = string.Concat(id, timeStamp.ToString(), random, clientInfo.ClientKey);
            var serverSign = Encrypt.MD5(preSign);
            if (!serverSign.Equals(clientSign, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("心跳包签名验证签名错误, cmd:{0}, preSign:{1}, serverSign:{2}, clientSign:{3}", cmd, preSign, serverSign, clientSign);
                return false;
            }

            clientId = id;
            return true;
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