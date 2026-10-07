using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyFinanceBackend.Data;
using MyFinanceModel;

namespace MyFinanceBackend.Services.AuthServices
{
    public class UserAuthorizeService : IUserAuthorizeService
    {
        private readonly IAuthorizationDataRepository _authorizationDataRepository;
        private readonly IUserRespository _userRepository;

        public UserAuthorizeService(IAuthorizationDataRepository authorizationDataRepository, IUserRespository userRepository)
        {
            _authorizationDataRepository = authorizationDataRepository;
            _userRepository = userRepository;
        }

        public async Task<bool> IsAuthorizedAsync(string authenticatedUserId, IEnumerable<string> targetUserIds,
            IEnumerable<ResourceActionNames> actionNames)
        {
            var actions = actionNames?.Distinct().ToArray();
            var targets = targetUserIds?.ToArray();
            if (!Guid.TryParse(authenticatedUserId, out var actorId) || actorId == Guid.Empty ||
                targets == null || targets.Length == 0 ||
                targets.Any(id => !Guid.TryParse(id, out var targetId) || targetId == Guid.Empty) ||
                actions == null || actions.Length == 0 ||
                actions.Any(action => action == ResourceActionNames.Unknown || !Enum.IsDefined(typeof(ResourceActionNames), action)))
            {
                return false;
            }

            foreach (var action in actions)
            {
                var allowed = false;
                var userAccessData = await
                    _authorizationDataRepository.GetUserAssignedAccessAsync(authenticatedUserId, ApplicationResources.Users,
                        action);
                foreach (var assignedAccess in userAccessData)
                {
                    var result = await EvaluateResourceAccessLevelAsync(assignedAccess.ResourceAccesLevel, authenticatedUserId,
                        targets);
                    if (result)
                    {
                        allowed = true;
                        break;
                    }
                }
                if (!allowed)
                    return false;
            }

            return true;
        }

        private async Task<bool> EvaluateResourceAccessLevelAsync(ResourceAccesLevels resourceAccesLevel, string authenticatedUserId,
            IEnumerable<string> targetUserIds)
        {
            switch (resourceAccesLevel)
            {
                case ResourceAccesLevels.Any: return AnyResourceAccessLevelEvaluation();
                case ResourceAccesLevels.Owned:
                    return await OwnedResourceAccesLevelEvaluationAsync(authenticatedUserId, targetUserIds);
                case ResourceAccesLevels.Self:
                    return SelfResourceAccessLevelEvaluation(authenticatedUserId, targetUserIds);
                default: return false;
            }
        }

        private bool AnyResourceAccessLevelEvaluation()
        {
            return true;
        }

        private bool SelfResourceAccessLevelEvaluation(string authenticatedUserId, IEnumerable<string> targetUserIds)
        {
            return targetUserIds.All(id => new Guid(id) == new Guid(authenticatedUserId));
        }

        private async Task<bool> OwnedResourceAccesLevelEvaluationAsync(string authenticatedUserId, IEnumerable<string> targetUserIds)
        {
            var owendUsers = await _userRepository.GetOwendUsersByUserIdAsync(authenticatedUserId);
            var ownedIds = owendUsers.Select(u => u.UserId).ToHashSet();
            return targetUserIds.All(id => ownedIds.Contains(new Guid(id)));
        }
    }
}
