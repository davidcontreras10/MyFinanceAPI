using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace MyFinanceModel.ClientViewModel
{
    public class ClientBasicAddSpend
    {
        private string _description;

        [Range(0.000001, float.MaxValue)]
        [Required]
        public float Amount { get; set; }

        [Range(0.000001, float.MaxValue)]
        public float? AmountNumerator { get; set; }

        [Range(0.000001, float.MaxValue)]
        public float? AmountDenominator { get; set; }

        public DateTime SpendDate { get; set; }

        [Range(1, int.MaxValue)]
        [Required]
        public int SpendTypeId { get; set; }

	    [JsonIgnore]
		public string UserId { get; set; }

        [Range(1, int.MaxValue)]
        [Required]
        public int CurrencyId { get; set; }

        [JsonIgnore]
        public string AmountType { get; set; }

        public string Description
        {
            get => _description?.Trim() ?? "";
            set => _description = value;
        }

        public bool IsPending { get; set; }

        public DateTime PaymentDate => SpendDate;

        [JsonIgnore]
        public TransactionTypeIds AmountTypeId { get; set; } = TransactionTypeIds.Invalid;
    }

    public enum TransactionTypeIds
    {
        Invalid = -1,
        Ignore = 0,
        Spend = 1,
        Saving = 2
    }
}