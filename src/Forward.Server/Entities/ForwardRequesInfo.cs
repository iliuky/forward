using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Forward.Server.Entities
{
    public class ForwardRequesInfo
    {
        public Task EndNotice { get; set; }

        public TaskCompletionSource<TcpClient> CompletionSource { get; set; }
    }
}
