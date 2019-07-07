using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Forward.Client.Service
{
    public class ForwardClientWrap : IDisposable
    {
        private readonly Process _process;

        public ForwardClientWrap()
        {
            _process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet.exe",
                    Arguments = "Forward.Client.dll",
                    WorkingDirectory = Path.Combine(Common.CurrentDirectory, "Forward.Client"),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                }
            };
            _process.OutputDataReceived += OutputDataReceived;
        }

        private void OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Common.Log("启动控制台程序的返回值:{0}", e.Data);
            }
        }

        /// <summary>
        /// 启动
        /// </summary>
        public void Start()
        {
            var success = _process.Start();
            if (!success)
            {
                throw new Exception("服务启动失败");
            }
            _process.BeginOutputReadLine();
        }

        /// <summary>
        /// 停止
        /// </summary>
        public void Stop()
        {
            using (_process)
            {
                _process.Kill();
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
