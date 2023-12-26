using System;
using System.Collections.Generic;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace EFDataAccess.Models
{
    public partial class EntitiesSupported
    {
        public int EntitiesSupportedId { get; set; }
        public string EntityName { get; set; }
        public string EntitySearchKey { get; set; }
    }
}
