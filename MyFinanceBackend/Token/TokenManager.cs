using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyFinanceBackend.Token
{
    public static class TokenManager
    {
        public static T CreateToken<T>(DateTime issued, TimeSpan expireTime) where T : SimpelToken, new()
        {
            var token = new T();
            token.ExpireTime = expireTime;
            token.Issued = issued;
        }
    }
}
