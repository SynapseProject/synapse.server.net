using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synapse.Common
{
    public class DirectoryRoleProvider : IAuthorizationProvider
    {
        public List<string> Allowed { get; set; }
        public List<string> Denied { get; set; }

        public string ListSourcePath { get; set; }


        public bool HasAccess(string id)
        {
            return true;
        }
    }
}