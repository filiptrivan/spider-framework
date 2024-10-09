using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Soft.Generator.Security.DTO
{
    public class JwtAuthResultDTO
    {
        /// <summary>
        /// Is nullable because of the registration
        /// </summary>
        public long? UserId { get; set; }
        public string UserEmail { get; set; }
        public string AccessToken { get; set; }
        public RefreshTokenDTO Token { get; set; }
    }
}
