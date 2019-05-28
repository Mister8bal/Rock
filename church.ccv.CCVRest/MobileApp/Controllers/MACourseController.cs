using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http;
using church.ccv.CCVRest.MobileApp.Model;
using church.ccv.Podcast;
using Rock.Data;
using Rock.Model;
using Rock.Rest.Filters;
using Rock.Web.Cache;

namespace church.ccv.CCVRest.MobileApp
{
    public partial class NewMobileAppController : Rock.Rest.ApiControllerBase
    {
        [Serializable]
        public enum SearchCoursesResponse
        {
            NotSet = -1,

            Success
        }

        [System.Web.Http.HttpGet]
        [System.Web.Http.Route( "api/NewMobileApp/SearchCourses" )]
        [Authenticate, Secured]
        public HttpResponseMessage SearchCourses( string nameKeyword = "",
                                                 string descriptionKeyword = "",
                                                 string street = "",
                                                 string city = "",
                                                 string state = "",
                                                 string zip = "",
                                                 int? skip = null,
                                                 int top = 10 )
        {
            // see if there's a location to use for sorting courses by distance
            Location locationForDistance = null;

            if ( string.IsNullOrWhiteSpace( street ) == false &&
                 string.IsNullOrWhiteSpace( city ) == false &&
                 string.IsNullOrWhiteSpace( state ) == false &&
                 string.IsNullOrWhiteSpace( zip ) == false )
            {
                // take the address provided and get a location object from it
                RockContext rockContext = new RockContext();
                Location foundLocation = new LocationService( rockContext ).Get( street, string.Empty, city, state, zip, GlobalAttributesCache.Read().OrganizationCountry );

                // if we found a location and it's geo-coded, we'll use it to sort courses by distance from it
                if ( foundLocation != null && foundLocation.GeoPoint != null )
                {
                    locationForDistance = foundLocation;
                }
            }

            List<MACourseModel> courseResults = MACourseService.GetMobileAppCourses( nameKeyword, descriptionKeyword, locationForDistance, skip, top );

            return Common.Util.GenerateResponse( true, SearchCoursesResponse.Success.ToString(), courseResults );
        }

        [Serializable]
        public enum CourseResponse
        {
            NotSet = -1,

            Success,

            NotFound
        }

        [System.Web.Http.HttpGet]
        [System.Web.Http.Route( "api/NewMobileApp/Course" )]
        [Authenticate, Secured]
        public HttpResponseMessage Course( int courseId )
        {
            // find the Rock Group (yes, group), and then we'll get a mobile app course from that
            RockContext rockContext = new RockContext();
            GroupService groupService = new GroupService( rockContext );
            Group group = groupService.Get( courseId );

            if ( group != null )
            {
                MACourseModel mobileAppCourse = MACourseService.GetMobileAppCourse( group );

                return Common.Util.GenerateResponse( true, CourseResponse.Success.ToString(), mobileAppCourse );
            }
            else
            {
                return Common.Util.GenerateResponse( false, CourseResponse.NotFound.ToString(), null );
            }
        }

        [Serializable]
        public enum JoinCourseResponse
        {
            NotSet = -1,

            Success,

            Success_SecurityIssue, //The user was processed, but needs a review by security before they can join the course.

            AlreadyInCourse, //The user is already in this course

            CourseNotFound, //A course with the provided id wasn't found

            InvalidModel,

            Failed
        }

        [System.Web.Http.HttpPost]
        [System.Web.Http.Route( "api/NewMobileApp/JoinCourse" )]
        [Authenticate, Secured]
        public HttpResponseMessage JoinCourse( [FromBody] JoinGroupModel joinCourseModel )
        {
            // Note - It's intentional that we're accepting a JoinGroupModel, 
            // since the params for group and course joining are the same.

            // make sure the model is valid
            if ( joinCourseModel == null ||
                string.IsNullOrWhiteSpace( joinCourseModel.FirstName ) == true ||
                string.IsNullOrWhiteSpace( joinCourseModel.LastName ) == true ||
                string.IsNullOrWhiteSpace( joinCourseModel.Email ) == true )
            {
                return Common.Util.GenerateResponse( false, JoinCourseResponse.InvalidModel.ToString(), null );
            }

            // try letting them join the course - for JOINING a course, the code is _idential_ to how a group is joined, so just use it.
            MAGroupService.RegisterPersonResult result = MAGroupService.RegisterPersonInGroup( joinCourseModel );
            switch ( result )
            {
                case MAGroupService.RegisterPersonResult.Success:
                    return Common.Util.GenerateResponse( true, JoinCourseResponse.Success.ToString(), null );

                case MAGroupService.RegisterPersonResult.SecurityIssue:
                    return Common.Util.GenerateResponse( true, JoinCourseResponse.Success_SecurityIssue.ToString(), null );

                case MAGroupService.RegisterPersonResult.GroupNotFound:
                    return Common.Util.GenerateResponse( true, JoinCourseResponse.CourseNotFound.ToString(), null );

                case MAGroupService.RegisterPersonResult.AlreadyInGroup:
                    return Common.Util.GenerateResponse( true, JoinCourseResponse.AlreadyInCourse.ToString(), null );

                default:
                    return Common.Util.GenerateResponse( false, JoinCourseResponse.Failed.ToString(), null );
            }
        }
    }
}
