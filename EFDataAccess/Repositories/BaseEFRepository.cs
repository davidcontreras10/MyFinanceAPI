using EFDataAccess.Models;

namespace EFDataAccess.Repositories
{
	public class BaseEFRepository
	{
		protected MyFinanceContext Context { get; }

		protected BaseEFRepository(MyFinanceContext context)
		{
			Context = context;
		}
    }
}
