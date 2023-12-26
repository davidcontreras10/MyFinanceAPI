using System;
using System.Collections.Generic;
using System.Text;

namespace EFDataAccess.Models
{
	public class EFAccountIdName
	{
		public int AccountId { get; set; }
		public string AccountName { get; set; }
	}

	public class EFLoginResult
	{
        public bool IsAuthenticated { get; set; }
        public bool ResetPassword { get; set; }
        public int ResultCode { get; set; }
        public string ResultMessage { get; set; }
    }
}
