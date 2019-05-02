using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Spatial;
using System.Linq;
using church.ccv.Actions;
using church.ccv.CCVRest.Common;
using church.ccv.CCVRest.MobileApp.Model;
using church.ccv.Podcast;
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
        const int GroupTypeId_NeighborhoodGroup = 49;
        const int GroupRoleId_ChildInFamily = 4;

        public static MAPersonModel GetMobileAppPerson( int personId )
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

            string publicAppRoot = GlobalAttributesCache.Value( "PublicApplicationRoot" ).EnsureTrailingForwardslash();

            MAPersonModel personModel = new MAPersonModel();

            // first get their basic info
            personModel.PrimaryAliasId = person.PrimaryAliasId.Value; //See definition of PersonModel - person.PrimaryAliasId can never actually be null.
            personModel.FirstName = person.NickName;
            personModel.LastName = person.LastName;
            personModel.Email = person.Email;
            personModel.Birthdate = person.BirthDate;
            personModel.Age = person.Age;

            if ( person.PhotoId.HasValue )
            {
                personModel.PhotoURL = publicAppRoot + "GetImage.ashx?Id=" + person.PhotoId;
            }
            else
            {
                personModel.PhotoURL = string.Empty;
            }

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

            // set their campus
            personModel.CampusId = family.CampusId;

            // set info about each family member
            personModel.FamilyMembers = new List<FamilyMemberModel>();
            foreach ( GroupMember groupMember in family.Members )
            {
                // don't add the person we're getting info about into their list of family members
                if ( groupMember.Person.Id != personId )
                {
                    FamilyMemberModel familyMember = new FamilyMemberModel
                    {
                        PrimaryAliasId = groupMember.Person.PrimaryAliasId ?? groupMember.Person.Id,
                        FirstName = groupMember.Person.FirstName,
                        LastName = groupMember.Person.LastName,
                        Age = groupMember.Person.Age
                    };

                    if ( groupMember.Person.PhotoId.HasValue )
                    {
                        familyMember.PhotoURL = publicAppRoot + "GetImage.ashx?Id=" + groupMember.Person.PhotoId;
                    }
                    else
                    {
                        familyMember.PhotoURL = string.Empty;
                    }

                    // if they're a child, set that, and also flag that this person HAS children
                    if ( groupMember.GroupRoleId == GroupRoleId_ChildInFamily )
                    {
                        familyMember.IsChild = true;
                        personModel.FamilyHasChildren = true;
                    }

                    personModel.FamilyMembers.Add( familyMember );
                }
                // however, since this IS the group member for that person, we can see if they're a child / adult
                else
                {
                    // if they're a child, set that, and also flag that this person HAS children
                    if ( groupMember.GroupRoleId == GroupRoleId_ChildInFamily )
                    {
                        personModel.IsChild = true;
                        personModel.FamilyHasChildren = true;
                    }
                }
            }

            // now get the neighborhood groups (and classes?) they're in
            personModel.Groups = new List<MAGroupModel>();

            // lazily load each group member
            foreach ( GroupMember member in person.Members )
            {
                // and group; to see if it's a neighborhood group
                if ( member.Group.GroupTypeId == GroupTypeId_NeighborhoodGroup )
                {
                    // it is, so add it (we can take just the non member view since specifics about the people in it doesn't matter)
                    MAGroupModel groupResult = MAGroupService.GetMobileAppGroup( member.Group, MAGroupService.MAGroupMemberView.NonMemberView );

                    if ( groupResult != null )
                    {
                        personModel.Groups.Add( groupResult );
                    }
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
                personModel.IsWorshipping = Actions_Adult.ERA.IsERA( person.Id );
                personModel.IsGiving = Actions_Adult.Give.IsGiving( person.Id );
                
                Actions_Adult.PeerLearning.Result peerLearningResult;
                Actions_Adult.PeerLearning.IsPeerLearning( person.Id, out peerLearningResult );
                personModel.IsConnected = peerLearningResult.IsPeerLearning;

                Actions_Adult.Serving.Result servingResult;
                Actions_Adult.Serving.IsServing( person.Id, out servingResult );
                personModel.IsServing = servingResult.IsServing;

                Actions_Adult.Teaching.Result teachingResult;
                Actions_Adult.Teaching.IsTeaching( person.Id, out teachingResult );
                personModel.IsCoaching = teachingResult.IsTeaching;

                List<int> storyIds;
                personModel.SharedStory = Actions_Adult.ShareStory.SharedStory( person.Id, out storyIds );
            }
            // get the students version
            else
            {
                // for students, they become eligible for more badges as their age / grade goes up
                
                // note - this is intentionally NOT wrapped by one big age if-statement, even tho most of them are 9+ / 4th grade

                // worship - always eligible
                personModel.IsWorshipping = Actions_Student.ERA.IsERA( person.Id );

                // baptism is 9+ / 4th grade
                if ( person.Age >= 9 )
                {
                    DateTime? baptismDate;
                    personModel.IsBaptised = Actions_Student.Baptised.IsBaptised( person.Id, out baptismDate );
                }
                else
                {
                    personModel.IsBaptised = null;
                }

                // giving is 9+ / 4th grade
                if ( person.Age >= 9 )
                {
                    personModel.IsGiving = Actions_Student.Give.IsGiving( person.Id );
                }
                else
                {
                    personModel.IsGiving = null;
                }

                // connected is 9+ / 4th grade
                if ( person.Age >= 9 )
                {
                    Actions_Student.PeerLearning.Result peerLearningResult;
                    Actions_Student.PeerLearning.IsPeerLearning( person.Id, out peerLearningResult );
                    personModel.IsConnected = peerLearningResult.IsPeerLearning;
                }
                else
                {
                    personModel.IsConnected = null;
                }

                // serving is 9+ / 4th grade
                if ( person.Age >= 9 )
                {
                    Actions_Student.Serving.Result servingResult;
                    Actions_Student.Serving.IsServing( person.Id, out servingResult );
                    personModel.IsServing = servingResult.IsServing;
                }
                else
                {
                    personModel.IsServing = null;
                }

                // shared story is 14+ / 9th grade
                if ( person.Age >= 14 )
                {
                    List<int> storyIds;
                    personModel.SharedStory = Actions_Student.ShareStory.SharedStory( person.Id, out storyIds );
                }
                else
                {
                    personModel.SharedStory = null;
                }

                // Students are never eligible to be coaches
                personModel.IsCoaching = null;
            }

            return personModel;
        }

        public enum UpdateMobileAppResult
        {
            Success,
            PersonNotFound,
            InvalidData
        }
        public static UpdateMobileAppResult UpdateMobileAppPerson( MAPersonModel mobileAppPerson )
        {
            RockContext rockContext = new RockContext();
            
            // find this person
            PersonAliasService personAliasService = new PersonAliasService( rockContext );
            PersonAlias personAlias = personAliasService.Get( mobileAppPerson.PrimaryAliasId );
            if ( personAlias == null )
            {
                return UpdateMobileAppResult.PersonNotFound;
            }

            // verify the core info is still valid
            if ( string.IsNullOrWhiteSpace( mobileAppPerson.FirstName ) == true ||
                 string.IsNullOrWhiteSpace( mobileAppPerson.LastName ) == true ||
                 string.IsNullOrWhiteSpace( mobileAppPerson.Email ) == true ||
                 mobileAppPerson.Email.IsValidEmail( ) == false )
            {
                return UpdateMobileAppResult.InvalidData;
            }

            // update the person's core info
            personAlias.Person.FirstName = mobileAppPerson.FirstName.Trim();
            personAlias.Person.NickName = mobileAppPerson.FirstName.Trim();
            personAlias.Person.LastName = mobileAppPerson.LastName.Trim();
            personAlias.Person.Email = mobileAppPerson.Email.Trim();

            // set the phone number (we only support Cell Phone for the Mobile App)
            DefinedValueCache cellPhoneType = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE );
            personAlias.Person.UpdatePhoneNumber( cellPhoneType.Id, PhoneNumber.DefaultCountryCode(), mobileAppPerson.PhoneNumberDigits, null, null, rockContext );

            // set their birthday, if provided
            if ( mobileAppPerson.Birthdate.HasValue )
            {
                personAlias.Person.SetBirthDate( mobileAppPerson.Birthdate );
            }
            
            // for address / campus updating, only do it if the person is in ONE family
            // otherwise a kid in a split family might update their address, only to
            // update mommy's address when they meant to update daddy's, and then mommy
            // thinks daddy is hacking her and they fight.
            List<Group> families = personAlias.Person.GetFamilies( rockContext ).ToList();
            if ( families.Count == 1 )
            {
                Group personFamily = families.First();

                // update their address if all fields are valid (street2 being optional)
                if ( string.IsNullOrWhiteSpace( mobileAppPerson.Street1 ) == false &&
                     string.IsNullOrWhiteSpace( mobileAppPerson.State ) == false &&
                     string.IsNullOrWhiteSpace( mobileAppPerson.City ) == false &&
                     string.IsNullOrWhiteSpace( mobileAppPerson.Zip ) == false )
                {
                    // get the location
                    var homeAddressDv = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME.AsGuid() );
                    
                    // take the group location flagged as a home address and mapped
                    GroupLocation familyAddress = personFamily.GroupLocations
                        .Where( l =>
                            l.GroupLocationTypeValueId == homeAddressDv.Id &&
                            l.IsMappedLocation )
                        .FirstOrDefault();

                    if ( familyAddress != null )
                    {
                        familyAddress.Location.Street1 = mobileAppPerson.Street1;
                        familyAddress.Location.Street2 = mobileAppPerson.Street2;
                        familyAddress.Location.City = mobileAppPerson.City;
                        familyAddress.Location.State = mobileAppPerson.State;
                        familyAddress.Location.PostalCode = mobileAppPerson.Zip;
                    }
                }

                // verify they're setting a valid campus
                if ( mobileAppPerson.CampusId.HasValue )
                {
                    CampusCache campus = CampusCache.Read( mobileAppPerson.CampusId.Value );
                    if ( campus != null )
                    {
                        personFamily.CampusId = mobileAppPerson.CampusId;
                    }
                }
            }

            // save changes and we're done!
            rockContext.SaveChanges();

            return UpdateMobileAppResult.Success;
        }

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

        public static int? GetNearestCampus( double longitude, double latitude, double maxDistanceMeters )
        {
            List<CampusCache> campusCacheList = CampusCache.All( false );

            // assume we're too far away from any campuses
            double closestDistance = maxDistanceMeters;
            int? closestCampusId = null;

            // take the provided long/lat and get a geoPoint out of it
            DbGeography geoPoint = DbGeography.FromText( string.Format( "POINT({0} {1})", longitude, latitude ) );

            // now go thru each campus
            foreach ( CampusCache campusCache in campusCacheList )
            {
                // if the campus has a geopoint defined
                if ( campusCache.Location.Longitude.HasValue && campusCache.Location.Latitude.HasValue )
                {
                    // put it in a geoPoint
                    DbGeography campusGeoPoint = DbGeography.FromText( string.Format( "POINT({0} {1})", campusCache.Location.Longitude, campusCache.Location.Latitude ) );

                    // take the distance between the the provided point and this campus
                    double? distanceFromCampus = campusGeoPoint.Distance( geoPoint );

                    // if a calcluation could be performed and it's closer than what we've already found, take it.
                    if ( distanceFromCampus.HasValue && distanceFromCampus < closestDistance )
                    {
                        closestDistance = distanceFromCampus.Value;
                        closestCampusId = campusCache.Id;
                    }
                }
            }

            // return whatever the closest campus was (or null if none were close enough)
            return closestCampusId;
        }

        // This is the Id for the attendance group created in the Check-In system within Rock. It should never change.
        const int Attendance_GroupId_CCVMobileAttendance = 2595385;

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

            const int InteractionComponent_Id = 156245;

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

        public static MASeriesModel PodcastSeriesToMobileAppSeries( PodcastUtil.PodcastSeries series )
        {
            string publicAppRoot = GlobalAttributesCache.Value( "PublicApplicationRoot" ).EnsureTrailingForwardslash();

            MASeriesModel maSeriesModel = new MASeriesModel();

            maSeriesModel.Name = series.Name;
            maSeriesModel.Description = series.Description;

            // parse and setup the date range for the series
            string dateRangeStr = string.Empty;
            series.Attributes.TryGetValue( "DateRange", out dateRangeStr );
            if ( string.IsNullOrWhiteSpace( dateRangeStr ) == false )
            {
                string[] dateRanges = dateRangeStr.Split( ',' );
                string startDate = DateTime.Parse( dateRanges[0] ).ToShortDateString();
                string endDate = DateTime.Parse( dateRanges[1] ).ToShortDateString();

                maSeriesModel.DateRange = startDate + " - " + endDate;
            }

            // set the images
            if ( string.IsNullOrWhiteSpace( series.Attributes["Image_16_9"] ) == false )
            {
                maSeriesModel.ImageURL = publicAppRoot + "GetImage.ashx?Guid=" + series.Attributes["Image_16_9"];
            }

            if ( string.IsNullOrWhiteSpace( series.Attributes["Image_1_1"] ) == false )
            {
                maSeriesModel.ThumbnailURL = publicAppRoot + "GetImage.ashx?Guid=" + series.Attributes["Image_1_1"];
            }

            // after we process messages, we'll use this value for deciding whether the Series is hidden.
            bool allMessagesHidden = true;

            // Now generate each message of the series
            maSeriesModel.Messages = new List<MobileAppMessageModel>();
            foreach ( PodcastUtil.PodcastMessage message in series.Messages )
            {
                MobileAppMessageModel maMessageModel = new MobileAppMessageModel();

                // (because this is a new attrib, and not all messages have it, check for null. default to TRUE since that's what thye'll want the majority of the time.)
                // This is totally confusing, but the KEY is called "Active", however, its name in Rock is "Approved". So confusing. My mistake. Ugh.
                bool messageActive = true;
                if ( message.Attributes.ContainsKey( "Active" ) )
                {
                    messageActive = bool.Parse( message.Attributes["Active"] );
                }

                // if the message doesn't start yet, or hasn't been approved, set it to private.
                if ( message.Date > RockDateTime.Now || messageActive == false )
                {
                    maMessageModel.Hidden = true;
                }
                else
                {
                    // this message is _NOT_ hidden, therefore we can set allMessagesHidden to false
                    allMessagesHidden = false;
                }

                maMessageModel.Name = message.Name;
                maMessageModel.Speaker = message.Attributes["Speaker"];
                maMessageModel.Date = message.Date.Value.ToShortDateString();
                maMessageModel.ImageURL = maSeriesModel.ImageURL;
                maMessageModel.ThumbnailURL = maSeriesModel.ThumbnailURL;

                string noteUrlValue = message.Attributes["NoteUrl"];
                if ( string.IsNullOrWhiteSpace( noteUrlValue ) == false )
                {
                    maMessageModel.NoteURL = noteUrlValue;
                }

                string watchUrlValue = message.Attributes["WatchUrl"];
                if ( string.IsNullOrWhiteSpace( watchUrlValue ) == false )
                {
                    maMessageModel.VideoURL = watchUrlValue;
                }

                string shareUrlValue = message.Attributes["ShareUrl"];
                if ( string.IsNullOrWhiteSpace( shareUrlValue ) == false )
                {
                    maMessageModel.ShareURL = shareUrlValue;
                }

                string discussionGuideUrlValue = null;
                message.Attributes.TryGetValue( "DiscussionGuideUrl", out discussionGuideUrlValue );
                if ( string.IsNullOrWhiteSpace( discussionGuideUrlValue ) == false )
                {
                    maMessageModel.DiscussionGuideURL = discussionGuideUrlValue;
                }

                maSeriesModel.Messages.Add( maMessageModel );
            }

            // Finally, let's see if the series should be flagged as Hidden.
            // It should be hidden if all messages are hidden OR the seriesActive flag is false
            bool isSeriesActive = series.Attributes["Active"] == "True" ? true : false;

            if ( allMessagesHidden || isSeriesActive == false )
            {
                maSeriesModel.Hidden = true;
            }

            return maSeriesModel;
        }

        internal static KidsContentModel BuildKidsContent( Person person )
        {
            // first, we need to know what grade range we'll be getting content for
            const string GradeRange_Infants = "Infants";
            const string GradeRange_EK = "Early Kids";
            const string GradeRange_LK = "Later Kids";
            const string GradeRange_JH = "Junior High";
            const string GradeRange_HS = "High School";

            // this is technically cheating, but Rock abstracts grade and doesn't natively
            // know about the US standard. To simplify things, let's do the conversion here
            int realGrade = 0; //(assume infant / pre-k)
            if ( person.GradeOffset.HasValue )
            {
                realGrade = 12 - person.GradeOffset.Value;
            }
            else
            {
                // before we completely assume 1st grade, see if we can use their age
                if ( person.Age.HasValue )
                {
                    if ( person.Age >= 14 )
                    {
                        realGrade = 9;
                    }
                    else if ( person.Age >= 10 )
                    {
                        realGrade = 6;
                    }
                    else if ( person.Age >= 6 )
                    {
                        realGrade = 1;
                    }
                    else
                    {
                        realGrade = 0;
                    }
                }
            }

            // now see which grade level they're in
            string targetGradeRange = string.Empty;
            if ( realGrade >= 9 )
            {
                targetGradeRange = GradeRange_HS;
            }
            else if ( realGrade >= 7 )
            {
                targetGradeRange = GradeRange_JH;
            }
            else if ( realGrade >= 5 )
            {
                targetGradeRange = GradeRange_LK;
            }
            else if ( realGrade >= 1 )
            {
                targetGradeRange = GradeRange_EK;
            }
            else
            {
                targetGradeRange = GradeRange_Infants;
            }
            
            // now that we know the range, build the content channel queries
            RockContext rockContext = new RockContext();
            ContentChannelService contentChannelService = new ContentChannelService( rockContext );

            // first, get AtCCV
            const int ContentChannelId_AtCCV = 286;
            ContentChannel atCCV = contentChannelService.Get( ContentChannelId_AtCCV );

            // sort by date
            var atCCVItems = atCCV.Items.OrderByDescending( i => i.StartDateTime );

            // now take the first one that matches our grade offset.

            // while iterating over these in memory could become slow as the list grows, the business
            // requirements of CCV mean it won't. Because there will always be a new entry each week for each grade level,
            // it won't ever realistically go over the first 4 items
            ContentChannelItem atCCVItem = null;
            foreach ( var item in atCCVItems )
            {
                // this is the slow part. If it ever does become an issue, replace it with an AV table join.
                item.LoadAttributes();
                if ( item.AttributeValues["GradeLevel"].ToString() == targetGradeRange )
                {
                    atCCVItem = item;
                    break;
                }
            }


            // next, get Faith Building At Home
            const int ContentChannelId_FaithBuilding = 287;
            ContentChannel faithBuilding = contentChannelService.Get( ContentChannelId_FaithBuilding );

            // sort by date
            var faithBuildingItems = faithBuilding.Items.OrderByDescending( i => i.StartDateTime );

            // as above, we'll iterate over the whole list in memory, knowing we'll actually only load attributes for about 4 items.
            ContentChannelItem faithBuildingItem = null;
            foreach ( var item in faithBuildingItems )
            {
                item.LoadAttributes();
                if ( item.AttributeValues["GradeLevel"].ToString() == targetGradeRange )
                {
                    faithBuildingItem = item;
                    break;
                }
            }


            // finally, get the resources available for the grade level
            const int ContentChannelId_Resources = 288;
            ContentChannel resourceChannel = contentChannelService.Get( ContentChannelId_Resources );

            List<ContentChannelItem> resourceList = new List<ContentChannelItem>();
            foreach ( var item in resourceChannel.Items )
            {
                item.LoadAttributes();
                if ( item.AttributeValues["GradeLevel"].ToString().Contains( targetGradeRange ) )
                {
                    resourceList.Add( item );
                }
            }

            // sort the resource list by priority
            resourceList.Sort( delegate ( ContentChannelItem a, ContentChannelItem b )
            {
                if ( a.Priority < b.Priority )
                {
                    return -1;
                }
                else if ( a.Priority == b.Priority )
                {
                    return 0;
                }
                else
                {
                    return 1;
                }
            } );

            
            // prepare our model - we'll require both main category items 
            // and otherwise return failure (note that resources CAN be empty)
            if ( atCCVItem != null && faithBuildingItem != null )
            {
                string publicAppRoot = GlobalAttributesCache.Value( "PublicApplicationRoot" ).EnsureTrailingForwardslash();

                KidsContentModel contentModel = new KidsContentModel();

                // At CCV
                contentModel.AtCCV_Title = atCCVItem.Title;
                contentModel.AtCCV_Date = atCCVItem.StartDateTime;
                contentModel.AtCCV_Content = atCCVItem.Content;
                contentModel.AtCCV_Date = atCCVItem.StartDateTime;
                contentModel.AtCCV_DiscussionTopic_One = atCCVItem.AttributeValues["DiscussionTopic1"].ToString();
                contentModel.AtCCV_DiscussionTopic_Two = atCCVItem.AttributeValues["DiscussionTopic2"].ToString();

                string seriesImageGuid = atCCVItem.AttributeValues["SeriesImage"].Value.ToString();
                if ( string.IsNullOrWhiteSpace( seriesImageGuid ) == false )
                {
                    contentModel.AtCCV_ImageURL = publicAppRoot + "GetImage.ashx?Guid=" + seriesImageGuid;
                }
                else
                {
                    contentModel.AtCCV_ImageURL = string.Empty;
                }

                // Faith building
                contentModel.FaithBuilding_Title = faithBuildingItem.Title;
                contentModel.FaithBuilding_Content = faithBuildingItem.Content;

                // resources CAN be empty, so just take whatever's available
                contentModel.Resources = new List<KidsResourceModel>();

                foreach ( var resourceItem in resourceList )
                {
                    KidsResourceModel resModel = new KidsResourceModel
                    {
                        Title = resourceItem.Title,
                        Subtitle = resourceItem.AttributeValues["Subtitle"].ToString(),
                        URL = resourceItem.AttributeValues["URL"].ToString()
                    };

                    contentModel.Resources.Add( resModel );
                }

                return contentModel;
            }
            else
            {
                return null;
            }
        }
    }
}
