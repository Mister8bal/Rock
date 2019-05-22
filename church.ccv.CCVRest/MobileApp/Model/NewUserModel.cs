using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace church.ccv.CCVRest.MobileApp.Model
{
    [Serializable]
    public class NewUserModel
    {
        public string FirstName;
        public string LastName;
        public string Email;

        public string Username;
        public string Password;
    }
}
