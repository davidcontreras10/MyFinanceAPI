using MyFinanceModel.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MyFinanceModel
{
    public class AppUser
    {
        #region Attributes 

        public string Username { set; get; }
        public string Name { set; get; }
        public Guid UserId { get; set; }
        public string PrimaryEmail { get; set; }
        public IReadOnlyCollection<AppRole> Roles { get; set; }

        #endregion

        public bool HasRole(RoleId role)
        {
            return Roles != null && Roles.Any(x => x.RoleId == role);
        }
    }
}
