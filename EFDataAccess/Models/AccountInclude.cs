using System;
using System.Collections.Generic;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace EFDataAccess.Models
{
    public partial class AccountInclude
    {
        public int AccountId { get; set; }
        public int AccountIncludeId { get; set; }
        public int? CurrencyConverterMethodId { get; set; }

        public virtual Account Account { get; set; }
        public virtual Account AccountIncludeNavigation { get; set; }
        public virtual CurrencyConverterMethod CurrencyConverterMethod { get; set; }
    }
}
