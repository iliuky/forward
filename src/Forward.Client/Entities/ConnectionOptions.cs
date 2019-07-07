using System;
using System.Collections.Generic;
using System.Text;

namespace Forward.Client.Entities
{
    public class ConnectionOptions
    {
        /// <summary>
        /// 转发id
        /// </summary>
        /// <value></value>
        public string ForwardId { get; set; }

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
        ///本地ip
        /// </summary>
        /// <value></value>
        public string LocalHost { get; set; }

        /// <summary>
        /// 本地端口
        /// </summary>
        /// <value></value>
        public int LocalPort { get; set; }
    }
}
