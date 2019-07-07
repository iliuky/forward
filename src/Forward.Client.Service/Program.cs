using System;
using System.ServiceProcess;

namespace Forward.Client.Service
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Common.Log("服务程序入口");

                ServiceBase[] services = new ServiceBase[] { new ForwardClientService() };
                ServiceBase.Run(services);

                //using (var server = new ForwardClientWrap())
                //{
                //    server.Start();
                //    Console.ReadKey();
                //}
            }
            catch (Exception ex)
            {
                Common.Log("启动服务异常:{0}", ex);
            }
            finally
            {
                Common.Log("服务程序退出");
            }
        }
    }
}
