using Rock.Plugin;

namespace church.ccv.CCVCore.Migrations
{
    [MigrationNumber( 2, "1.7.6" )]
    public class PAV_DropFamilyId : Migration
    {
        public override void Up()
        {
            Sql( @"
                ALTER TABLE [dbo].[_church_ccv_PlanAVisit_Visit]
                DROP CONSTRAINT IF EXISTS [FK_dbo._church_ccv_PlanAVisit_Visit_dbo.Group_FamilyId]

                ALTER TABLE [dbo].[_church_ccv_PlanAVisit_Visit]
                DROP COLUMN IF EXISTS [FamilyId]
            " );
        }

        public override void Down()
        {
        }
    }
}
