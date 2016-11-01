using Microsoft.AspNetCore.Authorization;
using System;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;

namespace mRPC
{
    /// <summary>
    /// Utility class to perform MVC authorization.
    /// </summary>
    internal static class Authorizations
    {
        /// <summary>
        /// Check if <see cref="ClaimsPrincipal"/> has authorization to use given Controller
        /// </summary>
        internal static async Task<bool> AuthorizeControllerAsync(
            IAuthorizationService service,
            Type controllerType,
            ClaimsPrincipal user)
        {
            foreach (var attr in controllerType.GetTypeInfo().GetCustomAttributes<AuthorizeAttribute>())
            {
                if (attr.Roles != null)
                {
                    if (!user.IsInRole(attr.Roles))
                    {
                        return false;
                    }
                }
                if (attr.Policy != null)
                {
                    if (!await service.AuthorizeAsync(user, attr.Policy))
                    {
                        return false;
                    }
                }
            }
            return true;
        }


        /// <summary>
        /// Check if <see cref="ClaimsPrincipal"/> has authorization to use given Action
        /// </summary>
        internal static async Task<bool> AuthorizeActionAsync(
            IAuthorizationService service,
            MethodInfo methodInfo,
            ClaimsPrincipal user)
        {
            foreach (var attr in methodInfo.GetCustomAttributes<AuthorizeAttribute>())
            {
                if (attr.Roles != null)
                {
                    if (!user.IsInRole(attr.Roles))
                    {
                        return false;
                    }
                }
                if (attr.Policy != null)
                {
                    if (!await service.AuthorizeAsync(user, attr.Policy))
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
