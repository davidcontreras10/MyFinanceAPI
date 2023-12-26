using System;
using System.Collections.Generic;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace EFDataAccess.Models
{
    public partial class MethodsSupported
    {
        public int EntitiesSupportedId { get; set; }
        public int MethodId { get; set; }
        public bool? Colones { get; set; }

        public virtual EntitiesSupported EntitiesSupported { get; set; }
    }
}
