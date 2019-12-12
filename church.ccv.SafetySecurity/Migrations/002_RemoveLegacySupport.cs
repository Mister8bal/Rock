using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rock.Plugin;

namespace church.ccv.SafetySecurity.Migrations
{
    [MigrationNumber( 2, "1.7.0" )]
    public class RemoveLegacySupport : Migration
    {
        /// <summary>
        /// The commands to run to migrate plugin to the specific version
        /// </summary>
        public override void Up()
        {
            DropColumn( "_church_ccv_SafetySecurity_VolunteerScreening", "Type" );
            DropColumn( "_church_ccv_SafetySecurity_VolunteerScreening", "Legacy_Application_DocFileId" );
            DropColumn( "_church_ccv_SafetySecurity_VolunteerScreening", "Legacy_CharacterReference1_DocFileId" );
            DropColumn( "_church_ccv_SafetySecurity_VolunteerScreening", "Legacy_CharacterReference2_DocFileId" );
            DropColumn( "_church_ccv_SafetySecurity_VolunteerScreening", "Legacy_CharacterReference3_DocFileId" );
        }

        /// <summary>
        /// The commands to undo a migration from a specific version
        /// </summary>
        public override void Down()
        {
            AddColumn( "_church_ccv_SafetySecurity_VolunteerScreening", "Type", c => c.Int() );
            AddColumn( "_church_ccv_SafetySecurity_VolunteerScreening", "Legacy_Application_DocFileId", c => c.Int() );
            AddColumn( "_church_ccv_SafetySecurity_VolunteerScreening", "Legacy_CharacterReference1_DocFileId", c => c.Int() );
            AddColumn( "_church_ccv_SafetySecurity_VolunteerScreening", "Legacy_CharacterReference2_DocFileId", c => c.Int() );
            AddColumn( "_church_ccv_SafetySecurity_VolunteerScreening", "Legacy_CharacterReference3_DocFileId", c => c.Int() );
        }
    }
}
