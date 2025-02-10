using Spider.Shared.Interfaces;
using Spider.Shared.Extensions;
using Spider.Security.Interfaces;
using Azure.Storage.Blobs;
using Spider.Security.Enums;
using Spider.Security.DTO;
using Microsoft.EntityFrameworkCore;

namespace Spider.Security.Services
{
    public class AuthorizationBusinessService<TUser> : AuthorizationBusinessServiceGenerated<TUser> where TUser : class, IUser, new()
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

        

    }
}
