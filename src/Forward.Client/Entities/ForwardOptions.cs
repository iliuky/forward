using System.Collections.Generic;
using System.Net;

namespace Forward.Client.Entities
{
    public class ForwardOptions
    {
        /// <summary>
        /// 客户端配置
        /// </summary>
        public ForwardServerOptions Forward { get; set; }

        /// <summary>
        /// 转发规则配置
        /// </summary>
        public IEnumerable<ForwardClientOptions> ForwardRules { get; set; }
    }

    public class ForwardServerOptions
    {
        /// <summary>
        /// ip 或者 域名
        /// </summary>
        /// <value></value>
        public string ServerHost { get; set; }

        /// <summary>
        /// 转发端口
        /// </summary>
        /// <value></value>
        public int ServerPort { get; set; }

        /// <summary>
        /// 客户端ip
        /// </summary>
        /// <value></value>
        public string ClientId { get; set; }

        /// <summary>
        /// 客户端key
        /// </summary>
        /// <value></value>
        public string ClientKey { get; set; }
    }

    public class ForwardClientOptions
    {
        /// <summary>
        /// 转发id 需和服务端 的id一致
        /// </summary>
        /// <value></value>
        public string RuleId { get; set; }

        /// <summary>
        /// ip 或者 域名
        /// </summary>
        /// <value></value>
        public string Host { get; set; }

        /// <summary>
        /// 转发端口
        /// </summary>
        /// <value></value>
        public int Port { get; set; }
    }
}