using System;
using System.Collections.Generic;

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
    }
}
