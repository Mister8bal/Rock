using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace church.ccv.CCVRest.MobileApp.Model
{
    [Serializable]
    public class FamilyMemberModel
    {
        public int PrimaryAliasId;

        public string FirstName;

        public string LastName;

        public bool IsChild;

        public string PhotoURL;
    }
}
