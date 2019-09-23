using System.ComponentModel;
using System.ComponentModel.Composition;

using Rock.Attribute;

namespace church.ccv.Utility.Communication
{
    /// <summary>
    /// Sends a communication through SMTP protocol
    /// </summary>
    [Description( "Sends a communication through SMTP protocol. CCV Customized." )]
    [Export( typeof( Rock.Communication.TransportComponent ) )]
    [ExportMetadata( "ComponentName", "CCVSMTP" )]

    [TextField( "Server", "", true, "", "", 0 )]
    [IntegerField( "Port", "", false, 25, "", 1 )]
    [TextField( "User Name", "", false, "", "", 2 )]
    [TextField( "Password", "", false, "", "", 3, null, true )]
    [BooleanField( "Use SSL", "", false, "", 4 )]
    public class CCVSMTP : CCVSMTPComponent
    {
        /// <summary>
        /// Gets the username.
        /// </summary>
        /// <value>
        /// The username.
        /// </value>
        public override string Username
        {
            get
            {
                return GetAttributeValue( "UserName" );
            }
        }
    }
}
