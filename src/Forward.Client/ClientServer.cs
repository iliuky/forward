using Forward.Client.Entities;
using Forward.Client.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Forward.Client
{
    /// <summary>
    /// 客户端服务管理
    /// </summary>
    public class ClientServer
    {
        private readonly IServiceProvider _lifetime;
        private readonly ForwardOptions _settings;
        private readonly ILogger _logger;

        public ClientServer(IServiceProvider lifetime, IOptions<ForwardOptions> options, ILogger<ClientServer> logger)
        {
            this._lifetime = lifetime;
            this._settings = options.Value;
            this._logger = logger;
        }

        /// <summary>
        /// 启动客户端服务
        /// </summary>
        /// <returns></returns>
        public async Task Start()
        {
            _logger.LogWarning("心跳服务启动");

            using (var heartScope = _lifetime.CreateScope())
            {
                var heartbeatService = heartScope.ServiceProvider.GetRequiredService<HeartbeatService>();
                heartbeatService.RequestConnectionEvent += GetRequestConnectionEventHandle(heartScope.ServiceProvider);
                await heartbeatService.Start();
            }

            _logger.LogWarning("心跳服务停止");
        }

        /// <summary>
        /// 获得请求连接事件处理程序
        /// </summary>
        /// <param name="service"></param>
        /// <returns></returns>
        private Func<string, Task> GetRequestConnectionEventHandle(IServiceProvider service)
        {
            return async parameter =>
            {
                using (var scope = service.CreateScope())
                {
                    try
                    {
                        var forwardService = scope.ServiceProvider.GetService<ForwardService>();
                        var connectionOptions = GetConnectionOptions(parameter);                       

                        await forwardService.Start(connectionOptions);
                        _logger.LogInformation("转发服务[{0}]退出", parameter);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "数据转发服务出现异常");
                    }
                }
            };
        }

        /// <summary>
        /// 获取客户端转发规则
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private ConnectionOptions GetConnectionOptions(string parameter)
        {
            var args = parameter.Split(",");
            if (args.Length != 2)
            {
                throw new Exception("服务端返回的请求连接参数错误");
            }

            var ruleId = args[1];
            var rule = _settings.ForwardRules.FirstOrDefault(u => u.RuleId == ruleId);

            if (rule == null)
            {
                throw new ArgumentNullException("forwardClientOptions", $"ruleId:{ruleId}转发规则未配置");
            }

            return new ConnectionOptions
            {
                ForwardId = args[0],
                ServerHost = _settings.Forward.ServerHost,
                ServerPort = _settings.Forward.ServerPort,
                LocalHost = rule.Host,
                LocalPort = rule.Port
            };
        }
    }
}
