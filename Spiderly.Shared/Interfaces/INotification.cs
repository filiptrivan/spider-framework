﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spiderly.Shared.Interfaces
{
    public interface INotification<T> where T : class
    {
        public string Title { get; set; }

        public string Description { get; set; }

        public string EmailBody { get; set; }

        public List<T> Recipients { get; } // M2M
    }
}
