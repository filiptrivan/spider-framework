using Spider.Security.Entities;
using Spider.Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spider.Security.Interfaces
{
    public interface IUser : IBusinessObject<long>
    {
        public string Email { get; set; }

        public bool? HasLoggedInWithExternalProvider { get; set; }

        public bool? IsDisabled { get; set; }

        public List<Role> Roles { get; }
    }
}
