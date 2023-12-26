namespace MyFinanceModel
{
    public class LoginResult
    {
        #region Attributes

        public AppUser User { set; get; }
        public bool IsAuthenticated { set; get; }
        public bool ResetPassword { set; get; }
        public string ResultCode { set; get; }
        public string ResultMessage { set; get; }

        #endregion

    }
}
