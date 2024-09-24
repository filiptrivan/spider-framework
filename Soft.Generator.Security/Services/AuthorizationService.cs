using Microsoft.EntityFrameworkCore;
using Soft.Generator.Shared.Interfaces;
using System.Linq.Dynamic.Core;
using FluentValidation;
using Soft.Generator.Shared.Services;
using Soft.Generator.Shared.Extensions;
using Soft.Generator.Shared.SoftExceptions;
using Soft.Generator.Security.Interface;

namespace Soft.Generator.Security.Services
{
    public class AuthorizationService : BusinessServiceBase
    {
        private readonly IApplicationDbContext _context;
        private readonly AuthenticationService _authenticationService;

        public AuthorizationService(IApplicationDbContext context, AuthenticationService authenticationService)
            : base(context)
        {
            _context = context;
            _authenticationService = authenticationService;
        }

        //public async Task<bool> IsAuthorizedAsync<TUser>(TUser user, Enum permissionCode) where TUser : class, IUser, new()
        //{
        //    bool result = false;

        //    await _context.WithTransactionAsync(async () =>
        //    {
        //        if (user == null)
        //            throw new ArgumentNullException("The user is not provided.");

        //        if (permissionCode == null) 
        //            throw new ArgumentNullException("Permission code is not provided.");

        //       result = user.Roles.Any(role => role.Permissions.Any(permission => permission.Code == permissionCode.ToString()));
        //    });

        //    return result;
        //}

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

        //public async Task AuthorizeAndThrowAsync<TUser>(TUser user, Enum permissionCode1, Enum permissionCode2) where TUser : class, IUser, new()
        //{
        //    bool result = false;

        //    await _context.WithTransactionAsync(async () =>
        //    {
        //        if (user == null)
        //            throw new ArgumentNullException("The user is not provided.");

        //        if (permissionCode1 == null || permissionCode2 == null)
        //            throw new ArgumentNullException("Permission code is not provided.");

        //        result = user.Roles
        //            .Any(role => role.Permissions
        //                .Count(permission => permission.Code == permissionCode1.ToString() || permission.Code == permissionCode2.ToString()) == 2);
        //    });

        //    if (result == false) throw new UnauthorizedException();
        //}

        public async Task<bool> IsAuthorizedAsync<TUser>(Enum permissionCode) where TUser : class, IUser, new()
        {
            bool result = false;
            long userId = _authenticationService.GetCurrentUserId();
            await _context.WithTransactionAsync(async () =>
            {
                TUser user = await LoadInstanceAsync<TUser, long>(userId, null);

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
                TUser user = await LoadInstanceAsync<TUser, long>(userId, null);

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
                TUser user = await LoadInstanceAsync<TUser, long>(userId, null);

                if (permissionCode1 == null || permissionCode2 == null)
                    throw new ArgumentNullException("Permission code is not provided.");

                result = user.Roles
                    .Any(role => role.Permissions
                        .Count(permission => permission.Code == permissionCode1.ToString() || permission.Code == permissionCode2.ToString()) == 2);
            });

            if (result == false) throw new UnauthorizedException();
        }

        //public async Task<bool> IsAuthorizedCustomAsync<TUser>(TUser user, Enum permissionCode, Func<Task<bool>> action) where TUser : class, IUser, new()
        //{
        //    return await _context.WithTransactionAsync(async () =>
        //    {
        //        if (await IsAuthorizedAsync<TUser>(user, permissionCode) == false)
        //            return false;

        //        return await action();
        //    });
        //}

        //public async Task AuthorizeAndThrowCustomAsync<TUser>(TUser user, Enum permissionCode, Func<Task<bool>> action) where TUser : class, IUser, new()
        //{
        //    await _context.WithTransactionAsync(async () =>
        //    {
        //        if (await IsAuthorizedAsync<TUser>(user, permissionCode) == false)
        //            throw new UnauthorizedException();

        //        await action();
        //    });
        //}

    }
}
