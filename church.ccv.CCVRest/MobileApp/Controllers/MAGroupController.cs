﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http;
using church.ccv.CCVRest.MobileApp.Model;
using Rock.Data;
using Rock.Model;
using Rock.Rest.Filters;
using Rock.Web.Cache;

namespace church.ccv.CCVRest.MobileApp
{
    public partial class NewMobileAppController : Rock.Rest.ApiControllerBase
    {
        [Serializable]
        public enum SearchGroupsResponse
        {
            NotSet = -1,

            Success
        }

        [System.Web.Http.HttpGet]
        [System.Web.Http.Route( "api/NewMobileApp/SearchGroups" )]
        [Authenticate, Secured]
        public HttpResponseMessage SearchGroups( string nameKeyword = "", 
                                                 string descriptionKeyword = "", 
                                                 string street = "", 
                                                 string city = "", 
                                                 string state = "", 
                                                 string zip = "", 
                                                 bool? requiresChildcare = false, 
                                                 int? skip = null, 
                                                 int top = 10 )
        {
            // see if there's a location to use for sorting groups by distance
            Location locationForDistance = null;

            if ( string.IsNullOrWhiteSpace( street ) == false &&
                 string.IsNullOrWhiteSpace( city ) == false &&
                 string.IsNullOrWhiteSpace( state ) == false &&
                 string.IsNullOrWhiteSpace( zip ) == false )
            {
                // take the address provided and get a location object from it
                RockContext rockContext = new RockContext( );
                Location foundLocation = new LocationService( rockContext ).Get( street, string.Empty, city, state, zip, GlobalAttributesCache.Read().OrganizationCountry );

                // if we found a location and it's geo-coded, we'll use it to sort groups by distance from it
                if ( foundLocation != null && foundLocation.GeoPoint != null )
                {
                    locationForDistance = foundLocation;
                }
            }

            List<MAGroupModel> groupResults = MAGroupService.GetMobileAppGroups( nameKeyword, descriptionKeyword, locationForDistance, requiresChildcare, skip, top );

            return Common.Util.GenerateResponse( true, SearchGroupsResponse.Success.ToString(), groupResults );
        }

        [Serializable]
        public enum GroupResponse
        {
            NotSet = -1,

            Success,

            NotFound
        }

        [System.Web.Http.HttpGet]
        [System.Web.Http.Route( "api/NewMobileApp/Group" )]
        [Authenticate, Secured]
        public HttpResponseMessage Group( int groupId )
        {
            return InternalGroup( groupId, MAGroupService.MAGroupMemberView.NonMemberView);
        }

        [System.Web.Http.HttpGet]
        [System.Web.Http.Route( "api/NewMobileApp/Group/MemberView" )]
        [Authenticate, Secured]
        public HttpResponseMessage GroupMemberView( int groupId )
        {
            return InternalGroup( groupId, MAGroupService.MAGroupMemberView.MemberView );
        }

        [System.Web.Http.HttpGet]
        [System.Web.Http.Route( "api/NewMobileApp/Group/CoachView" )]
        [Authenticate, Secured]
        public HttpResponseMessage GroupCoachView( int groupId )
        {
            return InternalGroup( groupId, MAGroupService.MAGroupMemberView.CoachView );
        }

        private HttpResponseMessage InternalGroup( int groupId, MAGroupService.MAGroupMemberView memberView )
        {
            // find the Rock group, and then we'll get a mobile app group from that
            RockContext rockContext = new RockContext();
            GroupService groupService = new GroupService( rockContext );
            Group group = groupService.Get( groupId );

            if ( group != null )
            {
                MAGroupModel mobileAppGroup = MAGroupService.GetMobileAppGroup( group, memberView );

                return Common.Util.GenerateResponse( true, GroupResponse.Success.ToString(), mobileAppGroup );
            }
            else
            {
                return Common.Util.GenerateResponse( false, GroupResponse.NotFound.ToString(), null );
            }
        }

        [Serializable]
        public enum JoinGroupResponse
        {
            NotSet = -1,

            Success,

            Success_SecurityIssue, //The user was processed, but needs a review by security before they can join the group.

            AlreadyInGroup, //The user is already in this group

            GroupNotFound, //A group with the provided group id wasn't found

            InvalidModel,

            Failed
        }

        [System.Web.Http.HttpPost]
        [System.Web.Http.Route( "api/NewMobileApp/JoinGroup" )]
        [Authenticate, Secured]
        public HttpResponseMessage JoinGroup( [FromBody] JoinGroupModel joinGroupModel )
        {
            // make sure the model is valid
            if ( joinGroupModel == null ||
                string.IsNullOrWhiteSpace( joinGroupModel.FirstName ) == true ||
                string.IsNullOrWhiteSpace( joinGroupModel.LastName ) == true ||
                string.IsNullOrWhiteSpace( joinGroupModel.Email ) == true )
            {
                return Common.Util.GenerateResponse( false, JoinGroupResponse.InvalidModel.ToString(), null );
            }

            // try letting them join the group
            MAGroupService.RegisterPersonResult result = MAGroupService.RegisterPersonInGroup( joinGroupModel );
            switch ( result )
            {
                case MAGroupService.RegisterPersonResult.Success:
                    return Common.Util.GenerateResponse( true, JoinGroupResponse.Success.ToString(), null );

                case MAGroupService.RegisterPersonResult.SecurityIssue:
                    return Common.Util.GenerateResponse( true, JoinGroupResponse.Success_SecurityIssue.ToString(), null );

                case MAGroupService.RegisterPersonResult.GroupNotFound:
                    return Common.Util.GenerateResponse( true, JoinGroupResponse.GroupNotFound.ToString(), null );

                case MAGroupService.RegisterPersonResult.AlreadyInGroup:
                    return Common.Util.GenerateResponse( true, JoinGroupResponse.AlreadyInGroup.ToString(), null );

                default:
                    return Common.Util.GenerateResponse( false, JoinGroupResponse.Failed.ToString(), null );
            }
        }
    }
}
