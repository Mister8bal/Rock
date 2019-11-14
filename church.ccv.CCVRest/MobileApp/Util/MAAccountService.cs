using System.Collections.Generic;
using System.Linq;
using church.ccv.CCVRest.MobileApp.Model;
using Rock;
using Rock.Communication;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;

namespace church.ccv.CCVRest.MobileApp
{
    public class MAAccountService
    {
        public static UserLogin CreateNewLogin( NewUserModel newUserModel, Person person, bool autoConfirm )
        {
            RockContext rockContext = new RockContext();

            // and now create the login for this person
            try
            {
                UserLogin login = UserLoginService.Create(
                                rockContext,
                                person,
                                Rock.Model.AuthenticationServiceType.Internal,
                                EntityTypeCache.Read( Rock.SystemGuid.EntityType.AUTHENTICATION_DATABASE.AsGuid() ).Id,
                                newUserModel.Username,
                                newUserModel.Password,
                                autoConfirm,
                                false );

                rockContext.SaveChanges();

                return login;
            }
            catch
            {
                // fail on exception
                return null;
            }
        }

        public static bool RegisterNewPerson( NewUserModel newUserModel )
        {
            RockContext rockContext = new RockContext();

            // get all required values and make sure they exist
            DefinedValueCache connectionStatusWebProspect = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_CONNECTION_STATUS_WEB_PROSPECT );
            DefinedValueCache recordStatusPending = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_PENDING );
            DefinedValueCache recordTypePerson = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON );

            if ( connectionStatusWebProspect == null || recordStatusPending == null || recordTypePerson == null )
            {
                return false;
            }

            // create a new person, which will give us a new Id
            Person person = new Person();

            // for new people, copy the stuff sent by the Mobile App
            person.FirstName = newUserModel.FirstName.Trim();
            person.LastName = newUserModel.LastName.Trim();

            person.Email = newUserModel.Email.Trim();
            person.IsEmailActive = string.IsNullOrWhiteSpace( person.Email ) == false ? true : false;
            person.EmailPreference = EmailPreference.EmailAllowed;

            // now set values so it's a Person Record Type, and pending web prospect.
            person.ConnectionStatusValueId = connectionStatusWebProspect.Id;
            person.RecordStatusValueId = recordStatusPending.Id;
            person.RecordTypeValueId = recordTypePerson.Id;

            // now, save the person so that all the extra stuff (known relationship groups) gets created.
            Group newFamily = PersonService.SaveNewPerson( person, rockContext );

            // add demographic note in person history
            var changes = new List<string>
            {
                "Created by CCV Mobile App"
            };
            HistoryService.AddChanges( rockContext, typeof( Person ), Rock.SystemGuid.Category.HISTORY_PERSON_DEMOGRAPHIC_CHANGES.AsGuid(), person.Id, changes );
            
            // save all changes
            person.SaveAttributeValues( rockContext );
            rockContext.SaveChanges();

            // and now create the login for this person
            UserLogin newLogin = CreateNewLogin( newUserModel, person, true );
            return newLogin != null ? true : false;
        }

        public static void SendConfirmAccountEmail( Person person, UserLogin userLogin )
        {
            const string ConfirmAccountUrlRoute = "ConfirmAccount";

            // send an email to the person found asking them to confirm their account
            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( null, person );

            string publicAppRoot = GlobalAttributesCache.Value( "PublicApplicationRoot" ).EnsureTrailingForwardslash();
            mergeFields.Add( "ConfirmAccountUrl", publicAppRoot + ConfirmAccountUrlRoute );

            mergeFields.Add( "Person", person );
            mergeFields.Add( "User", userLogin );

            var emailMessage = new RockEmailMessage( Rock.SystemGuid.SystemEmail.SECURITY_CONFIRM_ACCOUNT.AsGuid() );
            emailMessage.AddRecipient( new RecipientData( person.Email, mergeFields ) );
            emailMessage.CreateCommunicationRecord = false;
            emailMessage.Send();
        }

        public static bool SendForgotPasswordEmail( string personEmail )
        {
            // Define our constant values here in the function to keep them organized
            // Note that even tho this function is "ForgotPassword", technically it sends them a list of Usernames in an email,
            // which is why we use a Username Email Template
            const string ConfirmAccountUrlRoute = "ConfirmAccount";

            // setup merge fields
            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( null, null );

            string publicAppRoot = GlobalAttributesCache.Value( "PublicApplicationRoot" ).EnsureTrailingForwardslash();
            mergeFields.Add( "ConfirmAccountUrl", publicAppRoot + ConfirmAccountUrlRoute );
            var results = new List<IDictionary<string, object>>();

            var rockContext = new RockContext();
            var personService = new PersonService( rockContext );
            var userLoginService = new UserLoginService( rockContext );

            // get all the accounts associated with the person(s) using this email address
            bool hasAccountWithPasswordResetAbility = false;
            List<string> accountTypes = new List<string>();

            foreach ( Person person in personService.GetByEmail( personEmail )
                .Where( p => p.Users.Any() ) )
            {
                var users = new List<UserLogin>();
                foreach ( UserLogin user in userLoginService.GetByPersonId( person.Id ) )
                {
                    if ( user.EntityType != null )
                    {
                        var component = AuthenticationContainer.GetComponent( user.EntityType.Name );
                        if ( component.SupportsChangePassword )
                        {
                            users.Add( user );
                            hasAccountWithPasswordResetAbility = true;
                        }

                        accountTypes.Add( user.EntityType.FriendlyName );
                    }
                }

                var resultsDictionary = new Dictionary<string, object>();
                resultsDictionary.Add( "Person", person );
                resultsDictionary.Add( "Users", users );
                results.Add( resultsDictionary );
            }

            // if we found user accounts that were valid, send the email
            if ( results.Count > 0 && hasAccountWithPasswordResetAbility )
            {
                mergeFields.Add( "Results", results.ToArray() );

                var emailMessage = new RockEmailMessage( Rock.SystemGuid.SystemEmail.SECURITY_FORGOT_USERNAME.AsGuid() );
                emailMessage.AddRecipient( new RecipientData( personEmail, mergeFields ) );
                emailMessage.CreateCommunicationRecord = false;
                emailMessage.Send();

                return true;
            }

            return false;
        }
    }
}
