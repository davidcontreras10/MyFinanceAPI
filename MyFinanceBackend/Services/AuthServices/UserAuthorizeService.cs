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
            if (actionNames == null || !actionNames.Any())
            {
                throw new ArgumentNullException(nameof(actionNames));
            }

            foreach (var action in actionNames)
            {
                var userAccessData = await
                    _authorizationDataRepository.GetUserAssignedAccessAsync(authenticatedUserId, ApplicationResources.Users,
                        action);
                foreach (var assignedAccess in userAccessData)
                {
                    var result = await EvaluateResourceAccessLevelAsync(assignedAccess.ResourceAccesLevel, authenticatedUserId,
                        targetUserIds);
                    if (result)
                        return true;
                }
            }

            return false;
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
                case ResourceAccesLevels.AddRegular:
                    return NoEvaluationRequired();
                default: throw new ArgumentException("Invalid argument");
            }
        }

        private bool NoEvaluationRequired()
        {
            return true;
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
            return owendUsers.All(u => targetUserIds.Any(tu => new Guid(tu) == u.UserId));
        }
    }
}
