using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business
{
    public static class LogHelper
    {
        static LogHelper()
        {
            log4net.Config.XmlConfigurator.Configure();
        }
        private static readonly log4net.ILog loginfo = log4net.LogManager.GetLogger("Logger");

        /// <summary>
        /// 操作日志记录
        /// </summary>
        public static void _Info(string info)
        {
            if (loginfo.IsInfoEnabled)
            {
                loginfo.Info(info);
            }
        }

        /// <summary>
        /// 错误日志记录
        /// </summary>
        public static void _Error(string info, Exception ex = null)
        {
            if (loginfo.IsErrorEnabled)
            {
                loginfo.Error(info, ex);
            }
        }
    }
}
