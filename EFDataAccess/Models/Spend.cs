﻿using System;
using System.Collections.Generic;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace EFDataAccess.Models
{
    public partial class Spend
    {
        public Spend()
        {
            SpendOnPeriod = [];
        }

        public int SpendId { get; set; }
        public double? OriginalAmount { get; set; }
        public DateTime? SpendDate { get; set; }
        public int? SpendTypeId { get; set; }
        public string Description { get; set; }
        public int? AmountCurrencyId { get; set; }
        public int AmountTypeId { get; set; }
        public double? Numerator { get; set; }
        public double? Denominator { get; set; }
        public bool IsPending { get; set; }
        public DateTime? SetPaymentDate { get; set; }
		public DateTime? UtcRecordDate { get; set; }
		public bool? IsPurchase { get; set; }
		public int? CurrencyConverterMethodId { get; set; }
        public int? DebtorDebtRequestId { get; set; }
		public int? CreditorDebtRequestId { get; set; }


		public virtual Currency AmountCurrency { get; set; }
        public virtual AmountType AmountType { get; set; }
        public virtual SpendType SpendType { get; set; }
        public virtual LoanRecord LoanRecord { get; set; }
        public virtual ICollection<SpendOnPeriod> SpendOnPeriod { get; set; }
        public virtual EFAppTransfer SourceAppTransfer { get; set; }
		public virtual EFAppTransfer DestinationAppTransfer { get; set; }
        public virtual CurrencyConverterMethod CurrencyConverterMethod { get; set; }
		public virtual EFDebtRequest DebtorDebtRequest { get; set; }
		public virtual EFDebtRequest CreditorDebtRequest { get; set; }

		private static readonly Dictionary<int, int> AmountSign = new()
		{
            {1, 1 },
            {2, -1 }
        };

        private int GetAmountSign(bool ignore)
        {
            if (ignore)
            {
                return 1;
            }

            return AmountSign[AmountTypeId];
        }

        public double GetAmount(bool withSign)
        {
            if (OriginalAmount == null)
            {
                return 0;
            }

            if (Denominator == null || Numerator == null || Denominator == 0 || Numerator == 0)
            {
                return OriginalAmount.Value * GetAmountSign(!withSign);
            }

            return (OriginalAmount.Value * Numerator.Value * GetAmountSign(!withSign)) / Denominator.Value;
        }
    }
}
