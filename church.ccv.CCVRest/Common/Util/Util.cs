using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using church.ccv.CCVRest.Common.Model;
using Newtonsoft.Json;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using RestSharp;
using church.ccv.Datamart.Model;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace church.ccv.CCVRest.Common
{
    public class Util
    {
        // Utility function for handling the response model
        public static HttpResponseMessage GenerateResponse( bool success, string message, object data )
        {
            ResponseModel response = new ResponseModel
            {
                Success = success,
                Message = message,
                Data = data
            };

            StringContent restContent = new StringContent( JsonConvert.SerializeObject( response ), Encoding.UTF8, "application/json" );
            return new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = restContent
            };
        }

        public static bool CreateAttendanceRecord(
            int personId,
            int? campusId,
            int attendanceGroupId,
            DateTime startDateTime,
            AttendanceService attendanceService,
            PersonAliasService personAliasService,
            RockContext rockContext
        )
        {
            PersonAlias primaryAlias = personAliasService.GetPrimaryAlias( personId );
            if ( primaryAlias != null )
            {
                var attendance = rockContext.Attendances.Create();
                attendance.CampusId = campusId;
                attendance.GroupId = attendanceGroupId;
                attendance.PersonAlias = primaryAlias;
                attendance.PersonAliasId = primaryAlias.Id;
                attendance.StartDateTime = startDateTime;
                attendance.DidAttend = true;
                attendanceService.Add( attendance );

                return true;
            }

            return false;
        }

        public static bool HasAttendanceRecord(
            int personId,
            int attendanceGroupId,
            DateTime startDateTimeWindow,
            DateTime endDateTimeWindow,
            AttendanceService attendanceService,
            RockContext rockContext
        )
        {
            // allow a window of time to control the validity of the attendance.
            // For example: If you want to know if the user has attended on a "Saturday, 5-11-19",
            // startDateTimeWindow would be 5-11-19 at 12:00am and endDateTimeWindow would be 5-11-19 at 11:59pm.
            // If attendnace fell within that window, they attended.

            // If you wanted to allow attendance within the past "hour", it would be
            // startDateTimeWindow = Now.AddHour( -1 ) and endDateTimeWindow = Now.
            // That creates a sliding window where attendance marked for now will be counted for the next hour.
            Attendance attendance = attendanceService.Queryable( "Group,PersonAlias.Person" )
                .Where( a =>
                    a.StartDateTime >= startDateTimeWindow &&
                    a.StartDateTime < endDateTimeWindow &&
                    a.GroupId == attendanceGroupId &&
                    a.PersonAlias.PersonId == personId )
                .FirstOrDefault();

            return attendance != null;
        }

        public static void LaunchWorkflow( RockContext rockContext, 
                                           WorkflowTypeCache workflowTypeCache, 
                                           object contextEntity )
        {
            try
            {
                List<string> workflowErrors;
                var workflow = Workflow.Activate( workflowTypeCache, workflowTypeCache.Name );
                new WorkflowService( rockContext ).Process( workflow, contextEntity, out workflowErrors );
            }
            catch ( Exception ex )
            {
                ExceptionLogService.LogException( ex, null );
            }
        }

        public enum UpdatePersonPhotoResult
        {
            Success,
            PersonNotFound,
            PersonNotEligible,
            InvalidImage
        }

        public static UpdatePersonPhotoResult UpdatePersonPhoto( PersonPhotoModel personPhoto, out int? photoId )
        {
            // assume null, and set it only if this is successfull
            photoId = null;

            const int ProfilePicture_BinaryFileTypeId = 5;
            const int PhotoReview_GroupId = 1207885;
            const int PhotoReviewPending_GroupRoleId = 59;

            RockContext rockContext = new RockContext();

            // get the person ID by their primary alias id
            PersonAliasService paService = new PersonAliasService( rockContext );
            PersonAlias personAlias = paService.Get( personPhoto.PrimaryAliasId );

            if ( personAlias == null )
            {
                return UpdatePersonPhotoResult.PersonNotFound;
            }

            // find them in the datamart (if they're there) to see if they're staff, and therefore not eligible
            var datamartPersonService = new DatamartPersonService( rockContext ).Queryable().AsNoTracking();
            var datamartPerson = datamartPersonService.Where( dp => dp.PersonId == personAlias.PersonId ).SingleOrDefault();
            if ( datamartPerson != null && datamartPerson.IsStaff == true  )
            {
                return UpdatePersonPhotoResult.PersonNotEligible;
            }

            // validate the image
            byte[] imageBuffer = null;
            try
            {
                imageBuffer = Convert.FromBase64String( personPhoto.Base64ImageBuffer );
            }
            catch
            {
                return UpdatePersonPhotoResult.InvalidImage;
            }

            // setup the binary file model
            var binaryFile = new BinaryFile();

            binaryFile.IsTemporary = false;
            binaryFile.BinaryFileTypeId = ProfilePicture_BinaryFileTypeId;
            binaryFile.MimeType = "image/jpg";
            binaryFile.FileSize = imageBuffer.Length;
            binaryFile.FileName = "profile-picture-" + personPhoto.PrimaryAliasId + ".jpg";
            binaryFile.ContentStream = new MemoryStream( imageBuffer );

            // add to the database
            var binaryFileService = new BinaryFileService( rockContext );
            binaryFileService.Add( binaryFile );
            rockContext.SaveChanges();

            // update the photo id for the person
            personAlias.Person.PhotoId = binaryFile.Id;
            rockContext.SaveChanges();

            // lastly, put this person in the Photo Review group so their picture can be reviewed
            // ( or reset their status to pending if they're already in it)    
            var groupMemberService = new GroupMemberService( rockContext );
            GroupMember groupMember = groupMemberService.Queryable().Where( gm => gm.GroupId == PhotoReview_GroupId &&
                                                                                  gm.PersonId == personAlias.PersonId ).SingleOrDefault();
            if ( groupMember == null )
            {
                groupMember = new GroupMember();
                groupMember.PersonId = personAlias.PersonId;
                groupMember.GroupRoleId = PhotoReviewPending_GroupRoleId;
                groupMember.GroupId = PhotoReview_GroupId;

                groupMemberService.Add( groupMember );
            }

            groupMember.GroupMemberStatus = GroupMemberStatus.Pending;
            rockContext.SaveChanges();

            photoId = personAlias.Person.PhotoId;
            return UpdatePersonPhotoResult.Success;
        }

        public static List<string> GetAPForwardingNumber( Guid? apPrimaryAliasGuid )
        {
            const int DefinedTypeId_APForwardNumber = 32;
            const string ResponseRecipientKey = "ResponseRecipient";

            // given the primary alias guid of an Associate Pastor, get their forwarding phone number
            var smsForwardValues = new DefinedValueService( new RockContext() ).Queryable()
                                                                               .AsNoTracking()
                                                                               .Where( dv => dv.DefinedTypeId == DefinedTypeId_APForwardNumber )
                                                                               .ToList();

            // go thru each sms forward defined value
            List<string> phoneNumbers = new List<string>( );
            foreach ( var smsForward in smsForwardValues )
            {
                // see if it contains a key tying it to a person
                smsForward.LoadAttributes();
                if ( smsForward.AttributeValues.ContainsKey( ResponseRecipientKey ) )
                {
                    // if the guid matches the target person, take it
                    Guid? personAliasGuid = smsForward.AttributeValues[ResponseRecipientKey].Value.AsGuidOrNull();
                    if ( personAliasGuid == apPrimaryAliasGuid )
                    {
                        phoneNumbers.Add( smsForward.Value );
                    }
                }
            }

            return phoneNumbers;
        }

        public static async Task GetWistiaMedia( string mediaHashedId, Action<HttpStatusCode, WistiaMedia> response )
        {
            const string Wistia_MediaURL = "https://api.wistia.com/v1/medias/{0}.json?api_password={1}";
            string wistiaAPIKey = GlobalAttributesCache.Value( "WistiaMobileAppKey" );

            // do a simple request to wistia for the media id 
            string requestUrl = string.Format( Wistia_MediaURL, mediaHashedId, wistiaAPIKey );

            WistiaMedia media = null;
            HttpStatusCode statusCode = HttpStatusCode.InternalServerError;

            try
            {
                HttpClient client = new HttpClient();
                HttpResponseMessage wistiaResponse = await client.GetAsync( requestUrl );
                statusCode = wistiaResponse.StatusCode;

                if ( statusCode == HttpStatusCode.OK )
                {
                    // parse the response appropriately
                    var wistiaBlob = await wistiaResponse.Content.ReadAsAsync<JObject>();
                    media = JsonConvert.DeserializeObject<WistiaMedia>( wistiaBlob.ToString() );
                }
            }
            catch(Exception e)
            {
                Console.WriteLine( e.Message );
            }
            finally
            {
                // whether it went well or not, provide the status code and object from Wistia
                response( statusCode, media );
            }
        }

        public static string GetWistiaAssetMpeg4URL( WistiaMedia media, string platformType )
        {
            var videoAsset = media.Assets.Where( a => a.Type.ToLower() == platformType ).SingleOrDefault();

            if ( videoAsset != null )
            {
                // Per Wistia documentation: https://wistia.com/support/developers/asset-urls#ssl
                // Videos are served over http by default. For https, we must switch out the sub-domain.
                string videoURL_SSL = videoAsset.URL.Replace( "http://embed.wistia.com", "https://embed-ssl.wistia.com" );

                // per Wistia documentation: https://wistia.com/support/developers/asset-urls#modifying_file_extensions
                // Direct URLs are provided as .bin files. Some devices need a recognizable we drop the .bin, put a /, 
                // and then ANY file name and extension.
                string videoURL = videoURL_SSL.Replace( ".bin", "" );

                // make sure this platform type has mpeg 4 as its type
                if ( videoAsset.ContentType == "video/mp4" )
                {
                    // use the id of the media for the filename, to help with client caching.
                    return videoURL + "/" + media.Id + ".mp4";
                }
            }

            return null;
        }
    }
}
