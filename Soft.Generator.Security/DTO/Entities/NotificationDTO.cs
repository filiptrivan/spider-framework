using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.Security.DTO
{
    public partial class NotificationDTO
    {
        /// <summary>
        /// This property is only for current logged in user
        /// </summary>
        public bool? IsMarkedAsRead { get; set; }
    }
}
