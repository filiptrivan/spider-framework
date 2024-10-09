using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Soft.Generator.Infrastructure.Data;
using Soft.Generator.Security.DTO;
using Soft.Generator.Security.Entities;
using Soft.Generator.Security.Services;
using Soft.Generator.Shared.DTO;
using Soft.NgTable.Models;
using System.Security.Claims;

namespace Soft.Generator.WebAPI.Controllers
{
    [ApiController]
    [Route("/api/[controller]/[action]")]
    public class UserController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;
        private readonly SecurityBusinessService _securityBusinessService;
        private readonly ApplicationDbContext _context;

        public UserController(ILogger<UserController> logger, SecurityBusinessService securityBusinessService, ApplicationDbContext context)
        {
            _logger = logger;
            _securityBusinessService = securityBusinessService;
            _context = context;
        }


    }
}

