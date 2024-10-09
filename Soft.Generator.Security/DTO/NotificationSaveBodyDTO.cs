using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.Security.DTO
{
    public class NotificationSaveBodyDTO
    {
        public List<long> SelectedUserIds { get; set; }
        public NotificationDTO NotificationDTO { get; set; }
        public bool IsMarkedAsRead { get; set; }
    }
}
