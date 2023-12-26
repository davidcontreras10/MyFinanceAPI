using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyFinanceBackend.Data
{
    public interface ITransactional
    {
        void BeginTransaction();
        void RollbackTransaction();
        void Commit();
    }
}
