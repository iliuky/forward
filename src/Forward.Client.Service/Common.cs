using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Forward.Client.Service
{
    public class Common
    {

        private static string _currentDirectory;
        /// <summary>
        /// 程序运行的目录
        /// </summary>
        public static string CurrentDirectory
        {
            get
            {
                if (_currentDirectory == null)
                {
                    _currentDirectory = Path.GetDirectoryName(typeof(Common).Assembly.Location);
                }
                return _currentDirectory;
            }
        }

        /// <summary>
        /// 记录日记
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public static void Log(string message, params object[] args)
        {
            if (args != null && args.Length > 0)
            {
                message = string.Format(message, args);
            }
            var loggerFile = Path.Combine(CurrentDirectory, "service.log");
            var content = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}";
            File.AppendAllLines(loggerFile, new[] { content });
        }
    }
}
