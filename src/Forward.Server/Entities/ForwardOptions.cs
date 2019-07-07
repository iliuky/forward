using System.Collections.Generic;
using System.Net;

namespace Forward.Server.Entities
{
    public class ForwardOptions
    {
        /// <summary>
        /// 侦听心跳包的端口
        /// </summary>
        /// <value></value>
        public int ListenerHeartbeatPort { get; set; }

        /// <summary>
        /// 客户端
        /// </summary>
        /// <value></value>
        public IEnumerable<Client> Clients { get; set; }
    }

    public class Client
    {
        /// <summary>
        /// 客户端id
        /// </summary>
        /// <value></value>
        public string ClientId { get; set; }

        /// <summary>
        /// 客户端秘钥
        /// </summary>
        /// <value></value>
        public string ClientKey { get; set; }

        /// <summary>
        /// 转发规则
        /// </summary>
        /// <value></value>
        public IEnumerable<ForwardRule> ForwardRules { get; set; }
    }

    public class ForwardRule
    {
        /// <summary>
        /// 侦听的转发id
        /// </summary>
        /// <value></value>
        public string RuleId { get; set; }

        /// <summary>
        /// 侦听的访问端口
        /// </summary>
        /// <value></value>
        public int ListenerPort { get; set; }
    }
}