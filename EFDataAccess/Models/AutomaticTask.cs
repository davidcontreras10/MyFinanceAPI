using System;
using System.Collections.Generic;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace EFDataAccess.Models
{
    public partial class AutomaticTask
    {
        public AutomaticTask()
        {
            ExecutedTask = new HashSet<ExecutedTask>();
        }

        public Guid AutomaticTaskId { get; set; }
        public double Amount { get; set; }
        public int SpendTypeId { get; set; }
        public int CurrencyId { get; set; }
        public string TaskDescription { get; set; }
        public int AccountId { get; set; }
        public Guid UserId { get; set; }
        public int PeriodTypeId { get; set; }
        public string Days { get; set; }

        public virtual Account Account { get; set; }
        public virtual Currency Currency { get; set; }
        public virtual SpendType SpendType { get; set; }
        public virtual AppUser User { get; set; }
        public virtual SpInTrxDef SpInTrxDef { get; set; }
        public virtual TransferTrxDef TransferTrxDef { get; set; }
        public virtual ICollection<ExecutedTask> ExecutedTask { get; set; }
    }
}
