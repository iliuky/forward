using Forward.Server.Entities;
using Forward.Server.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Forward.Server
{
    /// <summary>
    /// 转发服务管理
    /// </summary>
    public class ListeningService
    {
        private readonly IServiceProvider _lifetime;
        private readonly ILogger _logger;
        private readonly ForwardOptions _settings;

        public ListeningService(IServiceProvider lifetime, IOptions<ForwardOptions> options, ILogger<ListeningService> logger)
        {
            this._lifetime = lifetime;
            this._logger = logger;
            this._settings = options.Value;
        }

        /// <summary>
        /// 启动转发服务
        /// </summary>
        /// <returns></returns>
        public async Task Start()
        {
            CheckConfigure();

            _logger.LogWarning("转发服务启动");

            using (var scope = _lifetime.CreateScope())
            {
                var heartbeatService = scope.ServiceProvider.GetRequiredService<HeartbeatService>();
                var tasks = BuildHeartbeatService(scope.ServiceProvider, heartbeatService).ToList();

                var heartbeatTask = heartbeatService.Start(_settings.ListenerHeartbeatPort);
                _logger.LogWarning("侦听心跳端口:{0}", _settings.ListenerHeartbeatPort);

                tasks.Add(heartbeatTask);

                await Task.WhenAll(tasks);
            }

            _logger.LogWarning("转发服务停止");
        }

        /// <summary>
        /// 构建转发连接处理服务
        /// </summary>
        /// <param name="heartbeatLifetime"></param>
        /// <param name="heartbeatService"></param>
        /// <returns></returns>
        private IEnumerable<Task> BuildHeartbeatService(IServiceProvider heartbeatLifetime, HeartbeatService heartbeatService)
        {
            foreach (var client in _settings.Clients)
            {
                _logger.LogWarning("侦听客户端的连接端口:{0}", client.ClientId);

                foreach (var rule in client.ForwardRules)
                {
                    var detail = new ForwardRuleDetail
                    {
                        ClientId = client.ClientId,
                        ClientKey = client.ClientKey,
                        RuleId = rule.RuleId,
                        ListenerPort = rule.ListenerPort
                    };

                    var rcs = heartbeatLifetime.GetRequiredService<RequestConnectionService>();
                    rcs.RequestConnectionEvent += GetRequestConnectionEventHandle(heartbeatService, detail);
                    yield return rcs.Start(detail.ListenerPort);
                    _logger.LogWarning("侦听连接端口:{0}, {1}:{2}", client.ClientId, rule.RuleId, rule.ListenerPort);
                }
            }
        }

        /// <summary>
        /// 获取连接请求处理 委托
        /// </summary>
        /// <param name="heartbeatService"></param>
        /// <returns></returns>
        private Func<TcpClient, CancellationToken, Task> GetRequestConnectionEventHandle(HeartbeatService heartbeatService, ForwardRuleDetail forwardRuleDetail)
        {
            return async (tcp, serviceToken) =>
            {
                var remoteEndPoint = tcp.Client.RemoteEndPoint;
                Action EndNotice = null;
                try
                {
                    _logger.LogInformation("接收到访问请求:ClientId, {0}:{1}, 访问者:{2}", forwardRuleDetail.ClientId, forwardRuleDetail.RuleId, remoteEndPoint);
                    var (serverTcp, EndNotice2) = await heartbeatService.GetRequestTcpClient(serviceToken, forwardRuleDetail);
                    EndNotice = EndNotice2;

                    if (serverTcp == null)
                    {
                        _logger.LogInformation("连接请求失败: 未获得服务端tcp连接");
                        return;
                    }
                    using (var forwarder = new Forwarder(tcp, serverTcp))
                    {
                        await forwarder.Start();
                    }
                }
                catch (IOException ex)
                {
                    _logger.LogInformation("数据转发连接断开:ClientId, {0}:{1}, 访问者:{2}, {3}", forwardRuleDetail.ClientId, forwardRuleDetail.RuleId, remoteEndPoint, ex.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "数据转发出现异常:ClientId, {0}:{1}, 访问者:{2}", forwardRuleDetail.ClientId, forwardRuleDetail.RuleId, remoteEndPoint);
                }
                EndNotice?.Invoke();
            };
        }

        /// <summary>
        /// 校验配置文件
        /// </summary>
        private void CheckConfigure()
        {
            if (_settings.ListenerHeartbeatPort < 0 || _settings.ListenerHeartbeatPort > 65535)
            {
                throw new InvalidCastException("ListenerHeartbeatPort 错误");
            }

            if (_settings.Clients == null || !_settings.Clients.Any())
            {
                _logger.LogWarning("未配置转发规则");
                return;
            }

            var ports = new HashSet<int>();
            ports.Add(_settings.ListenerHeartbeatPort);

            foreach (var client in _settings.Clients)
            {
                foreach (var item in client.ForwardRules)
                {
                    if (ports.Contains(item.ListenerPort))
                    {
                        throw new InvalidCastException($"RuleId:{item.RuleId} ,ListenerPort:{item.ListenerPort} 端口重复");
                    }
                    if (item.ListenerPort < 0 || item.ListenerPort > 65535)
                    {
                        throw new InvalidCastException($"RuleId:{item.RuleId} ,ListenerPort:{item.ListenerPort} 格式错误");
                    }
                    ports.Add(item.ListenerPort);
                }
            }
        }
    }
}
