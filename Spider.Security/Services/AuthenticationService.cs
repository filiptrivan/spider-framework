using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Spider.Security.Entities;
using Spider.Security.Interfaces;
using Spider.Shared.Interfaces;
using Spider.Shared.Services;
using Spider.Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Spider.Shared.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Spider.Security.Services
{
    public class AuthenticationService : BusinessServiceBase
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IApplicationDbContext _context;
        private readonly BlobContainerClient _blobContainerClient;

        public AuthenticationService(IHttpContextAccessor httpContextAccessor, IApplicationDbContext context, BlobContainerClient blobContainerClient)
            : base(context, blobContainerClient)
        {
            _httpContextAccessor = httpContextAccessor;
            _context = context;
            _blobContainerClient = blobContainerClient;
        }
        
        public long GetCurrentUserId()
        {
            return Helper.GetCurrentUserId(_httpContextAccessor.HttpContext);
        }

        public string GetCurrentUserEmail()
        {
            return Helper.GetCurrentUserEmail(_httpContextAccessor.HttpContext);
        }

        public async Task<TUser> GetCurrentUser<TUser>() where TUser : class, IUser, new()
        {
            return await _context.WithTransactionAsync(async () =>
            {
                return await GetInstanceAsync<TUser, long>(GetCurrentUserId(), null);
            });
        }

        public async Task<string> GetAccessTokenAsync()
        {
            return await _httpContextAccessor.HttpContext.GetTokenAsync("Bearer", "access_token");
        }

        public string GetIPAddress()
        {
            return Helper.GetIPAddress(_httpContextAccessor.HttpContext);
        }
    }
}
