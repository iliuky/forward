using System;
using System.Collections.Generic;
using System.Text;

namespace Forward.Server.Entities
{
    public class ForwardRuleDetail
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
    }
}
