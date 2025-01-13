using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.Shared.Attributes.EF
{
    public class SoftDisplayNameAttribute : Attribute
    {
        /// <summary>
        /// Don't use nameof, because source generator will take only "Email" if you pass nameof(User.Email)
        /// </summary>
        /// <param name="softDisplayName">Pass this parameter only if the display name is like this: User.Email</param>
        public SoftDisplayNameAttribute(string softDisplayName = null)
        {

        }
    }
}
