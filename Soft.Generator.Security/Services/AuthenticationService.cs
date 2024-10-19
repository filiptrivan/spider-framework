using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Soft.Generator.Security.Entities;
using Soft.Generator.Security.Interface;
using Soft.Generator.Shared.Interfaces;
using Soft.Generator.Shared.Services;
using Soft.Generator.Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;

namespace Soft.Generator.Security.Services
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
            return long.Parse(_httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.PrimarySid)?.Value);
        }

        public async Task<TUser> GetCurrentUser<TUser>() where TUser : class, IUser, new()
        {
            return await _context.WithTransactionAsync(async () =>
            {
                return await LoadInstanceAsync<TUser, long>(GetCurrentUserId(), null);
            });
        }

        public string GetCurrentUserEmail()
        {
            return _httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value;
        }

        public async Task<string> GetAccessTokenAsync()
        {
            return await _httpContextAccessor.HttpContext.GetTokenAsync("Bearer", "access_token");
        }

        public string GetIPAddress()
        {
            string ipAddress = GetRemoteHostIpAddressUsingXForwardedFor(_httpContextAccessor.HttpContext)?.ToString();

            if (string.IsNullOrEmpty(ipAddress) == true)
                ipAddress = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString();

            if (string.IsNullOrEmpty(ipAddress) == true)
                ipAddress = GetRemoteHostIpAddressUsingXRealIp(_httpContextAccessor.HttpContext)?.ToString();

            return ipAddress;
        }

        protected IPAddress GetRemoteHostIpAddressUsingXForwardedFor(HttpContext httpContext)
        {
            IPAddress remoteIpAddress = null;
            var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (string.IsNullOrEmpty(forwardedFor) == false)
            {
                var ips = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                      .Select(s => s.Trim());
                foreach (var ip in ips)
                {
                    if (IPAddress.TryParse(ip, out var address) &&
                        (address.AddressFamily is AddressFamily.InterNetwork
                         or AddressFamily.InterNetworkV6))
                    {
                        remoteIpAddress = address;
                        break;
                    }
                }
            }
            return remoteIpAddress;
        }

        protected IPAddress GetRemoteHostIpAddressUsingXRealIp(HttpContext httpContext)
        {
            IPAddress remoteIpAddress = null;
            var xRealIpExists = httpContext.Request.Headers.TryGetValue("X-Real-IP", out var xRealIp);
            if (xRealIpExists)
            {
                if (!IPAddress.TryParse(xRealIp, out IPAddress address))
                {
                    return remoteIpAddress;
                }
                var isValidIP = (address.AddressFamily is AddressFamily.InterNetwork
                                 or AddressFamily.InterNetworkV6);

                if (isValidIP)
                {
                    remoteIpAddress = address;
                }
                return remoteIpAddress;
            }
            return remoteIpAddress;
        }
    }
}
