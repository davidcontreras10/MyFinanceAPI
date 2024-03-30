using System.Text.Json.Serialization;

namespace MyFinanceModel.ClientViewModel
{
    public class AccountGroupClientViewModel
    {
        [JsonIgnore]
        public int AccountGroupId { get; set; }
        public string AccountGroupName { get; set; }
        public int AccountGroupPosition { get; set; }
        public string AccountGroupDisplayValue { get; set; }
        public bool DisplayDefault { get; set; }

        [JsonIgnore]
        public string UserId { get; set; }
    }
}
