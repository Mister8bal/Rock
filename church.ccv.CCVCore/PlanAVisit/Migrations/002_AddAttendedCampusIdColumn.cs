using Rock.Plugin;

namespace church.ccv.CommandCenter.Migrations
{
    [MigrationNumber( 2, "1.7.6" )]
    public class AddAttendedCampusIdColumn : Migration
    {
        public override void Up()
        {
            Sql( @"
    ALTER TABLE [dbo].[_church_ccv_PlanAVisit_Visit]
    ADD [AttendedCampusId] [int] NULL

    ALTER TABLE [dbo].[_church_ccv_PlanAVisit_Visit] WITH CHECK ADD CONSTRAINT [FK_dbo._church_ccv_PlanAVisit_Visit_dbo.Campus_AttendedCampusId] FOREIGN KEY([AttendedCampusId])
    REFERENCES [dbo].[Campus] ([Id])

    ALTER TABLE [dbo].[_church_ccv_PlanAVisit_Visit] CHECK CONSTRAINT [FK_dbo._church_ccv_PlanAVisit_Visit_dbo.Campus_AttendedCampusId]
" );
        }

        public override void Down()
        {
        }
    }
}
