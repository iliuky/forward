using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Forward.Server.Entities;
using Forward.Server.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using NLog.Extensions.Logging;

namespace Forward.Server
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var _logger = LogManager.GetCurrentClassLogger();
            _logger.Warn("端口转发程序启动");
            try
            {
                using (var services = Configure())
                {
                    await services.GetService<ListeningService>().Start();
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "程序出现异常退出");
            }
            finally
            {
                LogManager.Shutdown();
            }
        }

        private static ServiceProvider Configure()
        {
            var services = new ServiceCollection();
            var configuration = Configuration();
            services.AddSingleton<IConfiguration>(configuration);
            
            services.AddSingleton<ListeningService>();
            services.AddSingleton<HeartbeatService>();
            services.AddTransient<RequestConnectionService>();
            services.AddMemoryCache();
            services.Configure<ForwardOptions>(configuration);
            services.AddLogging(options =>
            {
                options.AddNLog();
            });

            return services.BuildServiceProvider();
        }

        /// <summary>
        /// 构建配置文件
        /// </summary>
        /// <returns></returns>
        private static IConfiguration Configuration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(AppContext.BaseDirectory))
                .AddJsonFile("appsettings.json", false);

            return builder.Build();
        }
    }
}
