using System.Collections.Generic;
using System.Linq;
using System.Net;
using MyFinanceModel;

namespace MyFinanceBackend.Services
{
	/// <summary>A currency conversion method, from <see cref="SourceCurrencyId"/> to <see cref="TargetCurrencyId"/>.</summary>
	public record ConverterMethodInfo(
		int Id,
		string Name,
		int SourceCurrencyId,
		int TargetCurrencyId,
		int? FinancialEntityId,
		bool IsDefault);

	/// <summary>What the repository knows about a prospective main account.</summary>
	public class AccountLinkContext
	{
		public int? ParentCurrencyId { get; set; }
		public int? ParentFinancialEntityId { get; set; }
		public IReadOnlyCollection<ConverterMethodInfo> Methods { get; set; } = [];
	}

	public class AccountLinkMethodOptions
	{
		/// <summary>Valid conversion methods (child currency -> main account currency). Empty means there is none.</summary>
		public IReadOnlyList<ConverterMethodInfo> Methods { get; init; } = [];

		/// <summary>True when there are several valid methods and the user has to pick one.</summary>
		public bool RequiresChoice { get; init; }
	}

	/// <summary>
	/// Rules for linking a sub-account (child) to a main account (parent), for currency conversion and bank:
	/// <list type="bullet">
	/// <item>If the main account has a financial entity, the sub-account must have the same one.</item>
	/// <item>Same currency: the default method (x1).</item>
	/// <item>Different currency and the main account has an entity: the single method for
	/// (child currency -> main account currency, that entity). It is chosen automatically.</item>
	/// <item>Different currency and the main account has no entity: the user picks among the methods
	/// for (child currency -> main account currency).</item>
	/// </list>
	/// </summary>
	public static class AccountLinkRules
	{
		public static AccountLinkMethodOptions GetOptions(
			int childCurrencyId,
			int parentCurrencyId,
			int? parentFinancialEntityId,
			IReadOnlyCollection<ConverterMethodInfo> catalog)
		{
			var pairMethods = catalog
				.Where(m => m.SourceCurrencyId == childCurrencyId && m.TargetCurrencyId == parentCurrencyId)
				.ToList();

			if (childCurrencyId == parentCurrencyId)
			{
				var defaults = pairMethods.Where(m => m.IsDefault).ToList();
				return new AccountLinkMethodOptions { Methods = defaults, RequiresChoice = defaults.Count > 1 };
			}

			var exchangeMethods = pairMethods.Where(m => !m.IsDefault);
			if (parentFinancialEntityId.HasValue)
			{
				exchangeMethods = exchangeMethods.Where(m => m.FinancialEntityId == parentFinancialEntityId);
			}

			var methods = exchangeMethods.OrderBy(m => m.Name).ToList();
			return new AccountLinkMethodOptions { Methods = methods, RequiresChoice = methods.Count > 1 };
		}

		/// <summary>
		/// Validates a new sub-account against its main account and returns the conversion method to store.
		/// When the method is determined by the rules the client's value is ignored; when the user has to
		/// choose, <paramref name="requestedMethodId"/> must be one of the valid options.
		/// </summary>
		public static int ResolveMethodId(
			int childCurrencyId,
			int? childFinancialEntityId,
			int parentCurrencyId,
			int? parentFinancialEntityId,
			int? requestedMethodId,
			IReadOnlyCollection<ConverterMethodInfo> catalog)
		{
			if (parentFinancialEntityId.HasValue && childFinancialEntityId != parentFinancialEntityId)
			{
				throw BadRequest("A sub-account must have the same financial entity as its main account.");
			}

			var options = GetOptions(childCurrencyId, parentCurrencyId, parentFinancialEntityId, catalog);
			if (options.Methods.Count == 0)
			{
				throw BadRequest(parentFinancialEntityId.HasValue
					? "There is no exchange method between these currencies for the main account's financial entity."
					: "There is no exchange method between these currencies.");
			}

			if (!options.RequiresChoice)
			{
				return options.Methods[0].Id;
			}

			if (requestedMethodId.HasValue && options.Methods.Any(m => m.Id == requestedMethodId.Value))
			{
				return requestedMethodId.Value;
			}

			throw BadRequest("Select the exchange method to use between the sub-account and its main account.");
		}

		private static ServiceException BadRequest(string message)
		{
			return new ServiceException(message, HttpStatusCode.BadRequest);
		}
	}
}
