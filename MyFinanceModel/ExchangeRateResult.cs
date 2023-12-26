namespace MyFinanceModel
{
    public class ExchangeRateResult
    {
        #region Public properties

        public double Numerator { get; set; }
        public double Denominator { get; set; }
        public bool Success { get; set; }
        public ResultType ResultTypeValue { get; set; }
        public ResultError ErrorType { get; set; }
        public int MethodId { get; set; }
        public string ErrorDetails { get; set; }

        #endregion

        public static ExchangeRateResult CreateDefaultExchangeRateResult(int methodId)
        {
            return new ExchangeRateResult
                {
                    MethodId = methodId,
                    Numerator = 1,
                    Denominator = 1,
                    ResultTypeValue = ResultType.Success,
                    Success = true
                };
        }

        public static ExchangeRateResult CreateDefaultExchangeRateResult()
        {
            return CreateDefaultExchangeRateResult(0);
        }

        public static ExchangeRateResult CreateNotImplementedExchangeRateResult(int methodId)
        {
            return new ExchangeRateResult
            {
                ResultTypeValue = ResultType.Error,
                ErrorType = ResultError.MethodNotImplemented,
                Success = false,
                MethodId = methodId
            };
        }

        public enum ResultError
        {
            AfterToday,
            TooEarly,
            ExchangeException,
            ServiceNotAvailable,
            MethodNotImplemented,
            EntityHasNoResults,
            Unknown,

        }

        public enum ResultType
        {
            Success, PendingUpdate, Error
        }
    }
}
