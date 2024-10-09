using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.Security.Entities
{
    public class NotificationUser
    {
        public bool? IsMarkedAsRead { get; set; }

        public long NotificationsId { get; set; }

        public long UsersId { get; set; }
    }
}
