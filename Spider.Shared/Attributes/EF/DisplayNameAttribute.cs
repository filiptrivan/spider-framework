using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spider.Shared.Attributes.EF
{
    public class DisplayNameAttribute : Attribute
    {
        /// <summary>
        /// Don't use nameof, because source generator will take only "Email" if you pass nameof(User.Email)
        /// </summary>
        /// <param name="softDisplayName">Pass this parameter only if the display name is like this: User.Email</param>
        public DisplayNameAttribute(string softDisplayName = null)
        {

        }
    }
}
