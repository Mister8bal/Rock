using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.UI.WebControls;
// CCV CORE - JHM - 2-28-19 - This file needs to use the latest (v12 as of this writing) version of CsvHelper to function correctly. However, Rock.Slingshot is using CsvHelper v2, so we can't upgrade it without changing core.
// So we downloaded the source code for v12, changed the namespace to CsvHelper12, and bundled that into Rock's libs folder, and reference it here in the website.
using CsvHelper12;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI;
using System.IO;
using Rock.Web.Cache;
using CsvHelper12.Configuration.Attributes;

namespace RockWeb.Plugins.church_ccv.Core
{
    /// <summary>
    /// Imports Event Brite Attendee report and syncs attendance.  If 
    /// a record in the csv matches a group member for the provided
    /// GroupId, the Attended GroupMemberAttribute Value will be 
    /// changed to Yes.
    /// </summary>
    [DisplayName( "CCV Eventbrite Sync" )]
    [Category( "CCV > Core" )]
    [Description( "Import an attendance CSV from Eventbrite to mark group members as attended." )]
    public partial class CCVEventBriteSync : RockBlock
    {
        public class EventBriteAttendee
        {
            [Name("First Name")]
            public string FirstName { get; set; }
            [Name( "Last Name" )]
            public string LastName { get; set; }
            [Name( "Email" )]
            public string Email { get; set; }
         
        }

        /// <summary>
        /// Handles the FileUploaded event of the fuImport control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void fuImport_FileUploaded( object sender, EventArgs e )
        {
            pnlStart.Visible = false;
            pnlDone.Visible = false;
            var rockContext = new RockContext();

            var binaryFileService = new BinaryFileService( rockContext );
            var binaryFile = binaryFileService.Get( fuImport.BinaryFileId ?? 0 );
            int GroupId = 0;

            var groupService = new GroupService(rockContext);
            var groupMemberService = new GroupMemberService(rockContext);

            Group selectedGroup = null;
            IQueryable<GroupMember> groupMembers;
            GroupMember groupMember = null;

            
             //Parse the group ID from the from and
             //attempt to located the associated group.
             
            if ( Int32.TryParse(tbGroupId.Text, out GroupId))
            {
                selectedGroup = groupService.Get(GroupId);
            }

            if ( binaryFile == null )
            {
                return;
            }

            using (var reader = new StreamReader(binaryFile.ContentStream))
            {
                using (var csvReader = new CsvHelper12.CsvReader(reader))
                {
                    csvReader.Configuration.HasHeaderRecord = true;
                    csvReader.Configuration.IgnoreBlankLines = true;
                    var peopleList = csvReader.GetRecords<EventBriteAttendee>().ToList();
                    bool importErrors = false;

                    foreach (var person in peopleList)
                    {
                        using (var personRockContext = new RockContext())
                        {

                            PersonService personService = new PersonService(personRockContext);
                            var attributeValueService = new AttributeValueService(personRockContext);

                            // Try to find matching person
                            var personMatches = personService.GetByMatch(person.FirstName, person.LastName, person.Email).Select(p => p.Id);

                            if (personMatches.Count() < 1)
                            {
                                continue;
                            }

                            //Loop through list of matching people and attempt
                            //to locate a group member.
                            foreach (var pId in personMatches)
                            {
                                groupMembers = groupMemberService.GetByGroupIdAndPersonId(GroupId, pId);

                                if (groupMembers.Count() < 1)
                                {
                                    continue;
                                }

                                groupMember = groupMembers.First();

                                groupMember.LoadAttributes();

                                groupMember.SetAttributeValue("Attended", "Yes");

                                groupMember.SaveAttributeValues(rockContext);
                            }


                        }
                    }
                    if (importErrors)
                    {
                        pnlError.Visible = true;
                    }
                    else
                    {
                        pnlDone.Visible = true;
                    }
                }
            }
        }
    }
}