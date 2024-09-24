using Soft.Generator.Security.Entities;
using Soft.Generator.Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.Security.Interface
{
    public interface IUser : IBusinessObject<long>
    {
        public string Email { get; set; }

        public string Password { get; set; }

        public bool HasLoggedInWithExternalProvider { get; set; }

        public int NumberOfFailedAttemptsInARow { get; set; }

        public List<Role> Roles { get; set; }

        public List<Notification> Notifications { get; set; }
    }
}
