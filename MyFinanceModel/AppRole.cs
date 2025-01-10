using MyFinanceModel.Enums;

namespace MyFinanceModel
{
	public class AppRole
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public int Level { get; set; }

		public RoleId RoleId
		{
			get
			{
				return (RoleId)Id;
			}
		}
	}
}
