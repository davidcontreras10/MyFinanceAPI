using MongoDB.Bson.Serialization.Attributes;
using MyFinanceModel.BankTrxCategorization;
using System.Security.Cryptography;
using System.Text;

namespace MongoDB.Models
{
	public class GptClassifiedExpenseCacheItem : IMongoEntity<string>, IGptCacheKey
	{

		[BsonId]
		public string Id => GenerateCacheKey(this);

		[BsonCamelCase]
		public string Description { get; set; } = default!;

		[BsonCamelCase]
		public decimal Amount { get; set; }

		[BsonCamelCase]
		public string Currency { get; set; } = default!;

		[BsonCamelCase]
		public int CategoryId { get; set; }

		[BsonCamelCase]
		public string Category { get; set; } = default!;

		[BsonCamelCase]
		public string CategoryConfidence { get; set; } = default!;

		[BsonCamelCase]
		public string AccountName { get; set; } = default!;

		[BsonCamelCase]
		public int AccountId { get; set; }

		[BsonCamelCase]
		public string AccountConfidence { get; set; } = default!;

		[BsonCamelCase]
		public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

		private static string ToSha1(string raw)
		{
			var bytes = Encoding.UTF8.GetBytes(raw);
			var hash = SHA1.HashData(bytes);
			return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant(); // hex string
		}

		private static string GenerateCacheKey(string description, decimal amount, string currency)
		{
			var rawKey = $"{description.Trim().ToLowerInvariant()}|{amount}|{currency.ToUpper()}";
			return ToSha1(rawKey);
		}

		public static string GenerateCacheKey(IGptCacheKey cacheKey)
		{
			var description = cacheKey.Description ?? throw new ArgumentException("Null or empty", nameof(cacheKey.Description));
			var amount = cacheKey.Amount;
			var currency = cacheKey.Currency ?? throw new ArgumentException("Null or empty", nameof(cacheKey.Currency));
			return GenerateCacheKey(description, amount, currency);
		}
	}
}
