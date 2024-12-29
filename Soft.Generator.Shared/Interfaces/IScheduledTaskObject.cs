using Soft.Generator.Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.Shared.Interfaces
{
    public interface IScheduledTaskObject
    {
        public DateTime StartedAt { get; set; }
        public DateTime FinishedAt { get; set; }
        public ScheduledTaskType ScheduledTaskType { get; set; }
    }
}
