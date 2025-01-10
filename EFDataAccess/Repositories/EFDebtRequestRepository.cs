using EFDataAccess.Models;
using MyFinanceBackend.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFDataAccess.Repositories
{
	public class EFDebtRequestRepository(MyFinanceContext context) : BaseEFRepository(context), IDebtRequestRepository
	{

	}
}
