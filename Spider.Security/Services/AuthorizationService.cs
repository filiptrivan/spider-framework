using Spider.Shared.Interfaces;
using System.Linq.Dynamic.Core;
using Spider.Shared.Services;
using Spider.Shared.Extensions;
using Spider.Shared.Exceptions;
using Spider.Security.Interface;
using Azure.Storage.Blobs;

namespace Spider.Security.Services
{
    public class AuthorizationService : BusinessServiceBase
    {
        private readonly IApplicationDbContext _context;
        private readonly AuthenticationService _authenticationService;
        private readonly BlobContainerClient _blobContainerClient;

        public AuthorizationService(IApplicationDbContext context, AuthenticationService authenticationService, BlobContainerClient blobContainerClient)
            : base(context, blobContainerClient)
        {
            _context = context;
            _authenticationService = authenticationService;
            _blobContainerClient = blobContainerClient;
        }

        public async Task AuthorizeAndThrowAsync<TUser>(TUser user, Enum permissionCode) where TUser : class, IUser, new()
        {
            bool result = false;

            await _context.WithTransactionAsync(async () =>
            {
                if (user == null)
                    throw new ArgumentNullException("The user is not provided.");

                if (permissionCode == null)
                    throw new ArgumentNullException("Permission code is not provided.");

                result = user.Roles.Any(role => role.Permissions.Any(permission => permission.Code == permissionCode.ToString()));
            });

            if (result == false) throw new UnauthorizedException();
        }

        public async Task<bool> IsAuthorizedAsync<TUser>(Enum permissionCode) where TUser : class, IUser, new()
        {
            bool result = false;
            long userId = _authenticationService.GetCurrentUserId();
            await _context.WithTransactionAsync(async () =>
            {
                TUser user = await GetInstanceAsync<TUser, long>(userId, null);

                if (permissionCode == null)
                    throw new ArgumentNullException("Permission code is not provided.");

                result = user.Roles.Any(role => role.Permissions.Any(permission => permission.Code == permissionCode.ToString()));
            });

            return result;
        }

        public async Task AuthorizeAndThrowAsync<TUser>(Enum permissionCode) where TUser : class, IUser, new()
        {
            bool result = false;
            long userId = _authenticationService.GetCurrentUserId();
            await _context.WithTransactionAsync(async () =>
            {
                TUser user = await GetInstanceAsync<TUser, long>(userId, null);

                if (permissionCode == null)
                    throw new ArgumentNullException("Permission code is not provided.");

                result = user.Roles.Any(role => role.Permissions.Any(permission => permission.Code == permissionCode.ToString()));
            });

            if (result == false) throw new UnauthorizedException();
        }

        /// <summary>
        /// Most frequent case is when checking SaveOrUpdate permissions
        /// </summary>
        public async Task AuthorizeAndThrowAsync<TUser>(Enum permissionCode1, Enum permissionCode2) where TUser : class, IUser, new()
        {
            bool result = false;
            long userId = _authenticationService.GetCurrentUserId();
            await _context.WithTransactionAsync(async () =>
            {
                TUser user = await GetInstanceAsync<TUser, long>(userId, null);

                if (permissionCode1 == null || permissionCode2 == null)
                    throw new ArgumentNullException("Permission code is not provided.");

                result = user.Roles
                    .Any(role => role.Permissions
                        .Count(permission => permission.Code == permissionCode1.ToString() || permission.Code == permissionCode2.ToString()) == 2);
            });

            if (result == false) throw new UnauthorizedException();
        }

    }
}
