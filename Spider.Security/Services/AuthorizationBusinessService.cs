using Spider.Shared.Interfaces;
using Spider.Shared.Extensions;
using Spider.Security.Interface;
using Azure.Storage.Blobs;
using Spider.Security.Enums;
using Spider.Security.DTO;
using Microsoft.EntityFrameworkCore;

namespace Spider.Security.Services
{
    public class AuthorizationBusinessService<TUser> : AuthorizationBusinessServiceGenerated where TUser : class, IUser, new()
    {
        private readonly IApplicationDbContext _context;
        private readonly AuthenticationService _authenticationService;
        private readonly BlobContainerClient _blobContainerClient;

        public AuthorizationBusinessService(IApplicationDbContext context, AuthenticationService authenticationService, BlobContainerClient blobContainerClient)
            : base(context, authenticationService, blobContainerClient)
        {
            _context = context;
            _authenticationService = authenticationService;
            _blobContainerClient = blobContainerClient;
        }

        public override async Task RoleSingleReadAuthorize(int roleId)
        {
            await _context.WithTransactionAsync(async () =>
            {
                await AuthorizeAndThrowAsync<TUser>(PermissionCodes.ReadRole);
            });
        }

        public override async Task RoleAuthorizeUpdateAndThrow(RoleDTO roleDTO) // FT: Save
        {
            await _context.WithTransactionAsync(async () =>
            {
                await AuthorizeAndThrowAsync<TUser>(PermissionCodes.EditRole);
            });
        }

        public override async Task RoleAuthorizeUpdateAndThrow(int roleId) // FT: Blob
        {
            await _context.WithTransactionAsync(async () =>
            {
                await AuthorizeAndThrowAsync<TUser>(PermissionCodes.EditRole);
            });
        }

        public override async Task RoleSingleInsertAuthorize(RoleDTO roleDTO) // FT: Save
        {
            await _context.WithTransactionAsync(async () =>
            {
                await AuthorizeAndThrowAsync<TUser>(PermissionCodes.InsertRole);
            });
        }

        public override async Task RoleSingleInsertAuthorize() // FT: Blob, the id will always be 0, so we don't need to pass it.
        {
            await _context.WithTransactionAsync(async () =>
            {
                await AuthorizeAndThrowAsync<TUser>(PermissionCodes.InsertRole);
            });
        }

        public override async Task RoleListReadAuthorize() // FT: Same for table, excel, autocomplete, dropdown
        {
            await _context.WithTransactionAsync(async () =>
            {
                await AuthorizeAndThrowAsync<TUser>(PermissionCodes.ReadRole);
            });
        }

        public override async Task RoleDeleteAuthorize(int roleId)
        {
            await _context.WithTransactionAsync(async () =>
            {
                await AuthorizeAndThrowAsync<TUser>(PermissionCodes.DeleteRole);
            });
        }
        public override async Task PermissionSingleReadAuthorize(int permissionId)
        {
            await _context.WithTransactionAsync(async () =>
            {
                await AuthorizeAndThrowAsync<TUser>(PermissionCodes.ReadPermission);
            });
        }

        public override async Task PermissionAuthorizeUpdateAndThrow(PermissionDTO permissionDTO) // FT: Save
        {
            await _context.WithTransactionAsync(async () =>
            {
                await AuthorizeAndThrowAsync<TUser>(PermissionCodes.EditPermission);
            });
        }

        public override async Task PermissionAuthorizeUpdateAndThrow(int permissionId) // FT: Blob
        {
            await _context.WithTransactionAsync(async () =>
            {
                await AuthorizeAndThrowAsync<TUser>(PermissionCodes.EditPermission);
            });
        }

        public override async Task PermissionSingleInsertAuthorize(PermissionDTO permissionDTO) // FT: Save
        {
            await _context.WithTransactionAsync(async () =>
            {
                await AuthorizeAndThrowAsync<TUser>(PermissionCodes.InsertPermission);
            });
        }

        public override async Task PermissionSingleInsertAuthorize() // FT: Blob, the id will always be 0, so we don't need to pass it.
        {
            await _context.WithTransactionAsync(async () =>
            {
                await AuthorizeAndThrowAsync<TUser>(PermissionCodes.InsertPermission);
            });
        }

        public override async Task PermissionListReadAuthorize() // FT: Same for table, excel, autocomplete, dropdown
        {
            await _context.WithTransactionAsync(async () =>
            {
                await AuthorizeAndThrowAsync<TUser>(PermissionCodes.ReadPermission);
            });
        }

        public override async Task PermissionDeleteAuthorize(int permissionId)
        {
            await _context.WithTransactionAsync(async () =>
            {
                await AuthorizeAndThrowAsync<TUser>(PermissionCodes.DeletePermission);
            });
        }

    }
}
