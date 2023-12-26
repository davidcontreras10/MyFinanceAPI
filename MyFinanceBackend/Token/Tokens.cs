using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyFinanceBackend.Token
{
    public class SimpelToken
    {
        public DateTime Issued { get; set; }
        public TimeSpan ExpireTime { get; set; }
    }
}
