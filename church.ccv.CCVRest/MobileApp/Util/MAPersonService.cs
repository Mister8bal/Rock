using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using church.ccv.Actions;
using church.ccv.CCVRest.MobileApp.Model;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace church.ccv.CCVRest.MobileApp
{
    public class MAPersonService
    {
        const int GroupRoleId_ChildInFamily = 4;
        const int RecordStatusId_InActive = 1;
        const int Attendance_GroupId_CCVMobileAttendance = 2595385;

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

            personModel.DisplayAge = GetDisplayAge( person );

            // get an access token so they can SSO to Rock sites
            personModel.RockAccessToken = "rckipid=" + person.GetImpersonationToken();

            if ( person.PhotoId.HasValue )
            {
                personModel.PhotoURL = publicAppRoot + "GetImage.ashx?Id=" + person.PhotoId + "&width=1200";
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
                // don't add the person we're getting info about into their list of family members,
                // and don't take family members that have an InActive record status
                // and to be extra safe, don't include deceased persons
                if ( groupMember.Person.Id != personId && 
                     groupMember.Person.RecordStatusValueId != RecordStatusId_InActive && 
                     groupMember.Person.IsDeceased == false )
                {
                    FamilyMemberModel familyMember = new FamilyMemberModel
                    {
                        PrimaryAliasId = groupMember.Person.PrimaryAliasId ?? groupMember.Person.Id,
                        FirstName = groupMember.Person.NickName,
                        LastName = groupMember.Person.LastName,
                        Email = groupMember.Person.Email,
                        Age = groupMember.Person.Age
                    };

                    familyMember.DisplayAge = GetDisplayAge( groupMember.Person );

                    if ( groupMember.Person.PhotoId.HasValue )
                    {
                        familyMember.ThumbnailPhotoURL = publicAppRoot + "GetImage.ashx?Id=" + groupMember.Person.PhotoId + "&width=180";
                    }
                    else
                    {
                        familyMember.ThumbnailPhotoURL = string.Empty;
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

            // lazily load each group member & group
            foreach ( GroupMember member in person.Members )
            {
                // if we're not inactive, and it's a NH group
                if( member.GroupMemberStatus != GroupMemberStatus.Inactive && MAGroupService.IsNeighborhoodGroup( member.Group ) )
                {
                    // get the group using the scope appropriate to their role
                    MAGroupService.MAGroupMemberView memberView = MAGroupService.GetViewForMember( member );

                    // it is, so add it (using the scope appropriate for their member)
                    MAGroupModel groupResult = MAGroupService.GetMobileAppGroup( member.Group, memberView );
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
                personModel.IsBaptised = Actions_Adult.Baptised.IsBaptised( person.Id, out personModel.BaptismDate );
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

                // baptism is 7+ / 2nd grade
                if ( person.Age >= 7 )
                {
                    personModel.IsBaptised = Actions_Student.Baptised.IsBaptised( person.Id, out personModel.BaptismDate );
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

            // hack for Teenager Spratt - Turn all her steps on
            if ( person.Id == 695355 )
            {
                personModel.IsBaptised = true;
                personModel.BaptismDate = new DateTime( 2018, 1, 5 );
                personModel.IsWorshipping = true;
                personModel.IsGiving = true;
                personModel.IsServing = true;
                personModel.IsConnected = true;
                personModel.SharedStory = true;
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
                 mobileAppPerson.Email.IsValidEmail() == false )
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
                    else
                    {
                        GroupService.AddNewGroupAddress( rockContext, 
                                                         personFamily, 
                                                         homeAddressDv.Guid.ToString(), 
                                                         mobileAppPerson.Street1, 
                                                         mobileAppPerson.Street2, 
                                                         mobileAppPerson.City, 
                                                         mobileAppPerson.State, 
                                                         mobileAppPerson.Zip, 
                                                         "US", 
                                                         true );
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

        public static bool HasAttendanceRecord( PersonAlias personAlias )
        {
            RockContext rockContext = new RockContext();
            AttendanceService attendanceService = new AttendanceService( rockContext );

            // We want attendance records to be limited to "once a day". This is not the same as
            // once every 24 hours.

            // create a start date that's the start of today
            var startDateTime = DateTime.Now.Date;
            startDateTime = TimeZoneInfo.ConvertTime( startDateTime, RockDateTime.OrgTimeZoneInfo );

            // and an end date that's 24 hours later, the exact end of the day
            DateTime endDateTime = startDateTime.AddDays( 1 );

            // now determine whether they've already recorded an attendance record for "today"
            return Common.Util.HasAttendanceRecord( personAlias.PersonId, Attendance_GroupId_CCVMobileAttendance, startDateTime, endDateTime, attendanceService, rockContext );
        }

        public static bool SaveAttendanceRecord( PersonAlias personAlias, int? campusId, string host, string userAgent )
        {
            RockContext rockContext = new RockContext();
            AttendanceService attendanceService = new AttendanceService( rockContext );
            PersonAliasService paService = new PersonAliasService( rockContext );

            if ( HasAttendanceRecord( personAlias ) == false )
            {
                // when recording an attendance record, use the actual date / time so we get an idea of when they were at service
                var startDateTime = DateTime.Now;
                startDateTime = TimeZoneInfo.ConvertTime( startDateTime, RockDateTime.OrgTimeZoneInfo );

                // attempt to save the record. if we did, this will return true
                if ( Common.Util.CreateAttendanceRecord( personAlias.PersonId, campusId, Attendance_GroupId_CCVMobileAttendance, startDateTime, attendanceService, paService, rockContext ) )
                {
                    // log the interaction
                    AddMobileAppAttendanceInteraction( personAlias, startDateTime, host, userAgent, rockContext );

                    rockContext.SaveChanges();

                    return true;
                }
            }

            // an attendance record already exists (or, extremely unlikely, the person couldn't be found)
            return false;
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

        private static string GetDisplayAge( Person person )
        {
            string displayAge = string.Empty;

            string formattedAge = person.FormatAge();

            if ( string.IsNullOrWhiteSpace( formattedAge ) == false )
            {
                // take just the age NUMBER
                string ageNumberStr = formattedAge.Split( ' ' )[0];

                displayAge = "Age " + ageNumberStr;

                // now determine the suffix. If it's years we do nothing. If it's months or days,
                // we'll add it.
                if ( formattedAge.Contains( "mo" ) )
                {
                    if ( ageNumberStr == "1" )
                    {
                        displayAge += " month";
                    }
                    else
                    {
                        displayAge += " months";
                    }
                }
                else if ( formattedAge.Contains( "day" ) )
                {
                    if ( ageNumberStr == "0" )
                    {
                        displayAge = "Born today!";
                    }
                    else if ( ageNumberStr == "1" )
                    {
                        displayAge += " day";
                    }
                    else
                    {
                        displayAge += " days";
                    }
                }
            }

            return displayAge;
        }
    }
}
