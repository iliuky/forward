using System;

namespace Forward.Client.Common
{
    public class DateTimeUtility
    {
        /// <summary>
        /// UTC 格林尼兹时间
        /// </summary>
        public static readonly DateTime UtcEpochTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// 当前时间的时间戳 (精度为秒)
        /// </summary>
        /// <value></value>
        public static int NowUnixTimeStamp
        {
            get
            {
                return DateTimeToUnixTimeStamp(DateTime.Now);
            }
        }

        #region 转换日期        

        /// <summary>
        /// 将时间转换成unix时间戳(精度为秒)
        /// </summary>
        /// <param name="time">本地时间</param>
        /// <returns>返回单位秒</returns>
        public static int DateTimeToUnixTimeStamp(DateTime time)
        {
            return (int)(time.AddHours(-8) - UtcEpochTime).TotalSeconds;
        }

        /// <summary>
        /// 将unix时间戳转换成时间(精度为秒)
        /// </summary>
        /// <param name="timeStamp"></param>
        /// <returns></returns>
        public static DateTime UnixTimeStampToDateTime(long timeStamp)
        {
            return UtcEpochTime.AddSeconds(timeStamp).AddHours(8);
        }

        /// <summary>
        /// 将DateID转换成时间
        /// </summary>
        /// <param name="dateId"></param>
        /// <returns></returns>
        public static DateTime ToDateTime(int dateId)
        {
            var dateTime = DateTime.Parse("1900-01-01");
            return dateTime.AddDays(dateId);
        }

        /// <summary>
        /// 将时间转换成DateID
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static int ToDateId(DateTime date)
        {
            return (int)(date - DateTime.Parse("1900-01-01")).TotalDays;
        }

        #endregion


        /// <summary>
        /// 获取指定日期的周一
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static DateTime GetMonday(DateTime date)
        {
            var weekDays = date.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)date.DayOfWeek;
            return date.AddDays(-(weekDays - 1));
        }
    }
}