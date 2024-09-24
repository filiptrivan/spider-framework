using Microsoft.AspNetCore.Mvc;
using Soft.Generator.Security.Interface;
using Soft.Generator.Security.Services;
using Soft.Generator.Infrastructure.Data;
using Soft.Generator.Security.SecurityControllers;

namespace Soft.Generator.WebAPI.Controllers
{
    [ApiController]
    [Route("/api/[controller]/[action]")]
    public class AuthController : BaseSecurityController
    {
        private readonly ILogger<AuthController> _logger;
        private readonly SecurityBusinessService _securityBusinessService;
        private readonly IJwtAuthManager _jwtAuthManagerService;
        private readonly ApplicationDbContext _context;

        public AuthController(ILogger<AuthController> logger, SecurityBusinessService securityBusinessService, IJwtAuthManager jwtAuthManagerService, ApplicationDbContext context) 
            : base(securityBusinessService, jwtAuthManagerService, context)
        {
            _logger = logger;
            _securityBusinessService = securityBusinessService;
            _jwtAuthManagerService = jwtAuthManagerService;
            _context = context;
        }

    }
}


