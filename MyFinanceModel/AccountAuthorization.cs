using System;
using System.Collections.Generic;

namespace MyFinanceModel
{
    public sealed record AccountAccessScope
    {
        public Guid OwnerId { get; }
        public string OwnerUserId => OwnerId.ToString();

        public AccountAccessScope(Guid ownerId)
        {
            if (ownerId == Guid.Empty)
                throw new ArgumentException("An account scope requires an owner.", nameof(ownerId));
            OwnerId = ownerId;
        }
    }

    public sealed record AccountAuthorizationResult(AccountAccessScope Scope)
    {
        public bool IsAuthorized => Scope != null;
    }

    public sealed record AccountAccessTargets
    {
        public IReadOnlyCollection<int> AccountIds { get; init; } = Array.Empty<int>();
        public IReadOnlyCollection<int> AccountPeriodIds { get; init; } = Array.Empty<int>();
        public IReadOnlyCollection<int> AccountGroupIds { get; init; } = Array.Empty<int>();
        public IReadOnlyCollection<int> SpendTypeIds { get; init; } = Array.Empty<int>();
    }
}
