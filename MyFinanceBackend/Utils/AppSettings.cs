using System;
using System.Configuration;

namespace MyFinanceBackend.Utils
{
    public static class AppSettings
    {
        public static TimeSpan ResetPasswordTokenExpireTime
        {
            get
            {
                var expireTimeType = ConfigurationManager.AppSettings["expire_rest_password_value_type"];
                var expireTimeValue = ConfigurationManager.AppSettings["expire_rest_password_value"];
                if (!double.TryParse(expireTimeValue, out double expireTime))
                {
                    throw new Exception("invalid expireTimeValue");
                }

                return GetTimeSpan(expireTimeType, expireTime);
            }
        }

        private static TimeSpan GetTimeSpan(string timeType, double timeoutValue)
        {
            TimeSpan timeout;
            switch (timeType)
            {
                case "ss":
                    timeout = TimeSpan.FromSeconds(timeoutValue);
                    break;
                case "mm":
                    timeout = TimeSpan.FromMinutes(timeoutValue);
                    break;
                case "dd":
                    timeout = TimeSpan.FromDays(timeoutValue);
                    break;
                case "hh":
                    timeout = TimeSpan.FromHours(timeoutValue);
                    break;
                default:
                    throw new Exception("expire_token_value_type");
            }

            return timeout;
        }
    }
}
