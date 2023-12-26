using System;
using System.Collections.Generic;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace EFDataAccess.Models
{
    public partial class DailyJob
    {
        public int DailyJobId { get; set; }
        public DateTime JobDate { get; set; }
        public string EventDesc { get; set; }
    }
}
