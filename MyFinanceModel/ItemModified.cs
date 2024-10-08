﻿using MyFinanceModel.Records;

namespace MyFinanceModel
{
    public class ItemModified
    {
        #region Attributes

        public int AccountId { get; set; }
        public bool IsModified { get; set; }

        #endregion

        public override bool Equals(object obj)
        {
            if (obj is ItemModified castObj)
            {
                return castObj.AccountId == AccountId;
            }

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public class SpendItemModified : ItemModified
    {
        public int SpendId { get; set; }

        public static SpendItemModified To(TrxItemModifiedRecord record)
		{
			return new SpendItemModified
			{
				AccountId = record.AccountId,
				IsModified = record.IsModified,
				SpendId = record.SpendId
			};
		}

		public override bool Equals(object obj)
        {
            if(obj is SpendItemModified castObj)
            {
                return base.Equals(obj) && castObj.SpendId == SpendId;
            }

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
	}
}
