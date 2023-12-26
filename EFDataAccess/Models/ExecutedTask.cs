using System;
using System.Collections.Generic;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace EFDataAccess.Models
{
    public partial class ExecutedTask
    {
        public int ExecutedTaskId { get; set; }
        public Guid AutomaticTaskId { get; set; }
        public Guid ExecutedByUserId { get; set; }
        public DateTime ExecuteDatetime { get; set; }
        public int ExecutionStatus { get; set; }
        public string ExecutionMsg { get; set; }

        public virtual AutomaticTask AutomaticTask { get; set; }
        public virtual AppUser ExecutedByUser { get; set; }
    }
}
