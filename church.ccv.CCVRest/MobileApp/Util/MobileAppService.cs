using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using church.ccv.Actions;
using church.ccv.CCVRest.Common;
using church.ccv.CCVRest.MobileApp.Model;
using Rock;
using Rock.Communication;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;

namespace church.ccv.CCVRest.MobileApp
{
    public class MobileAppService
    {
        public static MobileAppPersonModel GetMobileAppPerson( int personId )
        {
            RockContext rockContext = new RockContext();
            PersonService personService = new PersonService( rockContext );

            // start by getting the person. if we can't do that, we should fail
            Person person = personService.Queryable().Include( a => a.PhoneNumbers ).Include( a => a.Aliases )
                .FirstOrDefault( p => p.Id == personId );

            if ( person == null )
            {
                return null;
            }

            MobileAppPersonModel personModel = new MobileAppPersonModel();

            // first get their basic info
            personModel.PrimaryAliasId = person.PrimaryAliasId.Value; //See definition of PersonModel - person.PrimaryAliasId can never actually be null.
            personModel.FirstName = person.NickName;
            personModel.LastName = person.LastName;
            personModel.Email = person.Email;
            personModel.PhotoId = person.PhotoId;
            personModel.Birthdate = person.BirthDate;

            var mobilePhoneType = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() );
            if ( mobilePhoneType != null )
            {
                PhoneNumber phoneNumber = person.PhoneNumbers.FirstOrDefault( n => n.NumberTypeValueId == mobilePhoneType.Id );
                if ( phoneNumber != null )
                {
                    personModel.PhoneNumberDigits = phoneNumber.Number;
                }
            }

            // now get info about their family
            Group family = person.GetFamily();
            personModel.FamilyId = family.Id;

            personModel.FamilyMembers = new List<FamilyMemberModel>();
            foreach ( GroupMember groupMember in family.Members )
            {
                if ( groupMember.Person.Id != personId )
                {
                    FamilyMemberModel familyMember = new FamilyMemberModel
                    {
                        PrimaryAliasId = groupMember.Person.PrimaryAliasId ?? groupMember.Person.Id,
                        FirstName = groupMember.Person.FirstName,
                        LastName = groupMember.Person.LastName,
                        PhotoId = groupMember.Person.PhotoId

                    };

                    personModel.FamilyMembers.Add( familyMember );
                }
            }

            // now try to get ther home address for this family
            Guid? homeAddressGuid = Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME.AsGuidOrNull();
            if ( homeAddressGuid.HasValue )
            {
                var homeAddressDv = DefinedValueCache.Read( homeAddressGuid.Value );
                if ( homeAddressDv != null )
                {
                    // take the group location flagged as a home address and mapped
                    GroupLocation familyAddress = family.GroupLocations
                        .Where( l =>
                            l.GroupLocationTypeValueId == homeAddressDv.Id &&
                            l.IsMappedLocation )
                        .FirstOrDefault();

                    if ( familyAddress != null )
                    {
                        personModel.Street1 = familyAddress.Location.Street1;
                        personModel.Street2 = familyAddress.Location.Street2;
                        personModel.City = familyAddress.Location.City;
                        personModel.State = familyAddress.Location.State;
                        personModel.Zip = familyAddress.Location.PostalCode;
                    }
                }
            }

            // their age determines whether we use adult vs student actions. If they have no age, or are >= 18, they're an adult
            if ( person.Age.HasValue == false || person.Age >= 18 )
            {
                DateTime? baptismDate;
                personModel.IsBaptised = Actions_Adult.Baptised.IsBaptised( person.Id, out baptismDate );
                personModel.IsERA = Actions_Adult.ERA.IsERA( person.Id );
                personModel.IsGiving = Actions_Adult.Give.IsGiving( person.Id );

                DateTime? membershipDate;
                personModel.IsMember = Actions_Adult.Member.IsMember( person.Id, out membershipDate );

                Actions_Adult.Mentored.Result mentoredResult;
                Actions_Adult.Mentored.IsMentored( person.Id, out mentoredResult );
                personModel.IsMentored = mentoredResult.IsMentored;

                Actions_Adult.PeerLearning.Result peerLearningResult;
                Actions_Adult.PeerLearning.IsPeerLearning( person.Id, out peerLearningResult );
                personModel.IsPeerLearning = peerLearningResult.IsPeerLearning;

                Actions_Adult.Serving.Result servingResult;
                Actions_Adult.Serving.IsServing( person.Id, out servingResult );
                personModel.IsServing = servingResult.IsServing;

                Actions_Adult.Teaching.Result teachingResult;
                Actions_Adult.Teaching.IsTeaching( person.Id, out teachingResult );
                personModel.IsTeaching = teachingResult.IsTeaching;

                DateTime? startingPointDate;
                personModel.TakenStartingPoint = Actions_Adult.StartingPoint.TakenStartingPoint( person.Id, out startingPointDate );

                List<int> storyIds;
                personModel.SharedStory = Actions_Adult.ShareStory.SharedStory( person.Id, out storyIds );
            }
            // get the students version
            else
            {
                DateTime? baptismDate;
                personModel.IsBaptised = Actions_Student.Baptised.IsBaptised( person.Id, out baptismDate );
                personModel.IsERA = Actions_Student.ERA.IsERA( person.Id );
                personModel.IsGiving = Actions_Student.Give.IsGiving( person.Id );

                DateTime? membershipDate;
                personModel.IsMember = Actions_Student.Member.IsMember( person.Id, out membershipDate );

                Actions_Student.Mentored.Result mentoredResult;
                Actions_Student.Mentored.IsMentored( person.Id, out mentoredResult );
                personModel.IsMentored = mentoredResult.IsMentored;

                Actions_Student.PeerLearning.Result peerLearningResult;
                Actions_Student.PeerLearning.IsPeerLearning( person.Id, out peerLearningResult );
                personModel.IsPeerLearning = peerLearningResult.IsPeerLearning;

                Actions_Student.Serving.Result servingResult;
                Actions_Student.Serving.IsServing( person.Id, out servingResult );
                personModel.IsServing = servingResult.IsServing;

                Actions_Student.Teaching.Result teachingResult;
                Actions_Student.Teaching.IsTeaching( person.Id, out teachingResult );
                personModel.IsTeaching = teachingResult.IsTeaching;

                DateTime? startingPointDate;
                personModel.TakenStartingPoint = Actions_Student.StartingPoint.TakenStartingPoint( person.Id, out startingPointDate );

                List<int> storyIds;
                personModel.SharedStory = Actions_Student.ShareStory.SharedStory( person.Id, out storyIds );
            }

            return personModel;
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

            // save all changes
            person.SaveAttributeValues( rockContext );
            rockContext.SaveChanges();


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
                                true,
                                false );

                return true;
            }
            catch
            {
                // fail on exception
                return false;
            }
        }

        public static bool SendForgotPasswordEmail( string personEmail )
        {
            // Define our constant values here in the function to keep them organized
            // Note that even tho this function is "ForgotPassword", technically it sends them a list of Usernames in an email,
            // which is why we use a Username Email Template
            const string ForgotUserNamesEmailTemplateGuid = "113593FF-620E-4870-86B1-7A0EC0409208";
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

                var emailMessage = new RockEmailMessage( new Guid( ForgotUserNamesEmailTemplateGuid ) );
                emailMessage.AddRecipient( new RecipientData( personEmail, mergeFields ) );
                emailMessage.CreateCommunicationRecord = false;
                emailMessage.Send();

                return true;
            }

            return false;
        }

        // This is the Id for the attendance group created in the Check-In system within Rock. It should never change.
        const int Attendance_GroupId_CCVMobileAttendance = 2592301;

        public static bool HasAttendanceRecord( PersonAlias personAlias )
        {
            RockContext rockContext = new RockContext();
            AttendanceService attendanceService = new AttendanceService( rockContext );

            var dt = DateTime.Now;
            dt = TimeZoneInfo.ConvertTime( dt, RockDateTime.OrgTimeZoneInfo );

            return Common.Util.HasAttendanceRecord( personAlias.PersonId, Attendance_GroupId_CCVMobileAttendance, dt, attendanceService, rockContext );
        }

        public static bool SaveAttendanceRecord( PersonAlias personAlias, int? campusId, string host, string userAgent )
        {
            RockContext rockContext = new RockContext();
            AttendanceService attendanceService = new AttendanceService( rockContext );
            PersonAliasService paService = new PersonAliasService( rockContext );

            var dt = DateTime.Now;
            dt = TimeZoneInfo.ConvertTime( dt, RockDateTime.OrgTimeZoneInfo );

            // attempt to save the record. if we did, this will return true
            if ( Util.CreateAttendanceRecord( personAlias.PersonId, campusId, Attendance_GroupId_CCVMobileAttendance, dt, attendanceService, paService, rockContext ) )
            {
                // log the interaction
                AddMobileAppAttendanceInteraction( personAlias, dt, host, userAgent, rockContext );

                rockContext.SaveChanges();

                return true;
            }
            // if a record already existed, false will be returned
            else
            {
                return false;
            }
        }

        private static void AddMobileAppAttendanceInteraction( PersonAlias personAlias, DateTime interactionDateTime, string host, string userAgent, RockContext rockContext )
        {
            // Interactions work as follows:
            // 1. Create an Interaction Medium (or pick an existing one) in the Defined Type here: https://rock.ccv.church/page/119?definedTypeId=510
            // 2. In SQL, create an Interaction Channel (ensure you set UsesSession to 1) that is bound to the Medium in Step 1.
            // 3. In SQL, create an Interaction Component that is bound to the Interaction Channel in Step 2.
            // 4. Make note of the InteractionComponent Id in Step 3.

            // To use:
            // The relationship is as follows:
            // Interaction points to an InteractionComponent (created above) and an InteractionSession that's created with each Interaction.
            // The InteractionSession points to an InteractionDeviceType, which can either be an existing one, or new, depending on whether an appropriate DeviceType can be found.
            // Typically each new Interaction use case (like this Mobile App Attendance Interaction) defines its own format for the InteractionDeviceType, so they'll be unique within the scope of
            // the interaction use case.
            //
            // What follows is a typical example of recording one.
            // 1. Check for a useable InteractionDeviceType. If one isn't found, create it.
            // 2. Create the InteractionSession and give it the InteractionDeviceType. This will always happen.
            // 3. Create the Interaction and give it the InteractionSession and InteractionComponent. (The InteractionComponent Id should be hardcoded)


            // Hardcode constant values we use for Mobile App attendance interaction. 
            // There isn't a place in Rock that centralizes this stuff, so this is as close to centralized as is possible.
            const string InteractionDeviceType_Name = "Mobile App";
            const string InteractionDeviceType_ClientType = "Mobile";
            const string InteractionDeviceType_Application = "CCV Mobile App";

            const string InteractionSession_SessionData = "Mobile App Attendance";

            const string Interaction_Operation = "Attend";

            const int InteractionComponent_Id = 154114;

            // first, see if there's already a device type matching this requester - We store device types 
            // with our defined InteractionDeviceTypeData above, and the userAgent string. That's it.
            InteractionDeviceTypeService interactionDeviceTypeService = new InteractionDeviceTypeService( rockContext );
            var interactionDeviceType = interactionDeviceTypeService.Queryable()
                                                                    .Where( a =>
                                                                            a.Name == InteractionDeviceType_Name &&
                                                                            a.ClientType == InteractionDeviceType_ClientType &&
                                                                            a.DeviceTypeData == userAgent &&
                                                                            a.Application == InteractionDeviceType_Application &&
                                                                            a.OperatingSystem == null )
                                                                    .FirstOrDefault();

            // if we didn't get one back, create one.
            if ( interactionDeviceType == null )
            {
                interactionDeviceType = new InteractionDeviceType
                {
                    Name = InteractionDeviceType_Name,
                    ClientType = InteractionDeviceType_ClientType,
                    DeviceTypeData = userAgent,
                    Application = InteractionDeviceType_Application,
                    CreatedByPersonAliasId = personAlias.Id
                };

                interactionDeviceTypeService.Add( interactionDeviceType );
            }


            // now create the interaction session
            InteractionSessionService interactionSessionService = new InteractionSessionService( rockContext );
            InteractionSession interactionSession = new InteractionSession
            {
                SessionData = InteractionSession_SessionData,
                DeviceTypeId = interactionDeviceType.Id,
                IpAddress = host,
                CreatedByPersonAliasId = personAlias.Id
            };
            interactionSessionService.Add( interactionSession );


            // finally, add the interaction itself
            InteractionService interactionService = new InteractionService( rockContext );
            Interaction thisInteraction = new Interaction()
            {
                Operation = Interaction_Operation,
                InteractionDateTime = interactionDateTime,
                PersonAliasId = personAlias.Id,
                InteractionComponentId = InteractionComponent_Id,
                InteractionSessionId = interactionSession.Id
            };
            interactionService.Add( thisInteraction );
        }
    }
}
