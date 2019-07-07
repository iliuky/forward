using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace Forward.Client.Service
{
    partial class ForwardClientService : ServiceBase
    {
        private ForwardClientWrap _forwardClient;
        public ForwardClientService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                Common.Log("服务启动");
                _forwardClient = new ForwardClientWrap();
                _forwardClient.Start();
            }
            catch (Exception ex)
            {
                Common.Log("服务启动失败:{0}", ex);
            }
        }

        protected override void OnStop()
        {
            try
            {
                Common.Log("服务停止");
                _forwardClient?.Stop();
            }
            catch (Exception ex)
            {
                Common.Log("服务退出失败:{0}", ex);
            }           
        }
    }
}
