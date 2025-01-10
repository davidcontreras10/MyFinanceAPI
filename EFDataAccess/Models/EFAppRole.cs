using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFDataAccess.Models
{
    public class EFAppRole
    {
		public EFAppRole()
		{
			Users = new HashSet<AppUser>();
		}

		public int Id { get; set; }
		public string Name { get; set; }
        public int Level { get; set; }

        public ICollection<AppUser> Users { get; set; }
    }
}
