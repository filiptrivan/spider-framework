using Spider.Shared.Interfaces;
using System.Linq.Dynamic.Core;
using Spider.Shared.Services;
using Spider.Shared.Extensions;
using Spider.Shared.Exceptions;
using Spider.Security.Interfaces;
using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using Spider.Security.Entities;

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

        public async Task AuthorizeAndThrowAsync<TUser>(TUser user, string permissionCode) where TUser : class, IUser, new()
        {
            if (user == null)
                throw new ArgumentNullException("The user is not provided.");

            if (permissionCode == null)
                throw new ArgumentNullException("Permission code is not provided.");

            bool result = false;

            await _context.WithTransactionAsync(async () =>
            {
                result = user.Roles.Any(role => role.Permissions.Any(permission => permission.Code == permissionCode));
            });

            if (result == false)
                throw new UnauthorizedException();
        }

        public async Task<bool> IsAuthorizedAsync<TUser>(string permissionCode) where TUser : class, IUser, new()
        {
            if (permissionCode == null)
                throw new ArgumentNullException("Permission code is not provided.");

            bool result = false;
            long userId = _authenticationService.GetCurrentUserId();

            await _context.WithTransactionAsync(async () =>
            {
                result = await _context.DbSet<TUser>()
                    .AsNoTracking()
                    .AnyAsync(user =>
                        user.Id == userId &&
                        user.Roles.Any(role => role.Permissions.Any(permission => permission.Code == permissionCode))
                    );
            });

            return result;
        }

        public async Task AuthorizeAndThrowAsync<TUser>(string permissionCode) where TUser : class, IUser, new()
        {
            if (permissionCode == null)
                throw new ArgumentNullException("Permission code is not provided.");

            bool result = false;
            long userId = _authenticationService.GetCurrentUserId();

            await _context.WithTransactionAsync(async () =>
            {
                result = await _context.DbSet<TUser>()
                    .AsNoTracking()
                    .AnyAsync(user =>
                        user.Id == userId &&
                        user.Roles.Any(role => role.Permissions.Any(permission => permission.Code == permissionCode))
                    );
            });

            if (result == false)
                throw new UnauthorizedException();
        }

        public async Task<List<string>> GetCurrentUserPermissionCodes<TUser>() where TUser : class, IUser, new()
        {
            return await _context.WithTransactionAsync(async () =>
            {
                long userId = _authenticationService.GetCurrentUserId();

                return await _context.DbSet<TUser>()
                    .AsNoTracking()
                    .Where(x => x.Id == userId)
                    .SelectMany(x => x.Roles)
                    .SelectMany(x => x.Permissions)
                    .Select(x => x.Code)
                    .Distinct()
                    .ToListAsync();
            });
        }

    }
}
