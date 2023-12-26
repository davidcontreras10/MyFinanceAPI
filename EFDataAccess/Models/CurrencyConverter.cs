using System;
using System.Collections.Generic;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace EFDataAccess.Models
{
    public partial class CurrencyConverter
    {
        public CurrencyConverter()
        {
            CurrencyConverterMethod = new HashSet<CurrencyConverterMethod>();
        }

        public int CurrencyConverterId { get; set; }

        public int CurrencyIdOne { get; set; }
        public virtual Currency CurrencyOne { get; set; }

        public int CurrencyIdTwo { get; set; }
        public virtual Currency CurrencyTwo { get; set; }

        public virtual ICollection<CurrencyConverterMethod> CurrencyConverterMethod { get; set; }
    }
}
