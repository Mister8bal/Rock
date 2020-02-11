using Rock.Plugin;

namespace church.ccv.CCVCore.Migrations
{
    [MigrationNumber( 1, "1.7.6" )]
    public class PAV_CreateDb : Migration
    {
        public override void Up()
        {
            Sql( @"
                CREATE TABLE [dbo].[_church_ccv_PlanAVisit_Visit](
	                    [Id] [int] IDENTITY(1,1) NOT NULL,
                        [AdultOnePersonAliasId] [int] NOT NULL,
                        [AdultTwoPersonAliasId] [int] NULL,
                        [FamilyId] [int] NOT NULL,
	                    [ScheduledCampusId] [int] NOT NULL,
	                    [ScheduledDate] [datetime] NOT NULL,
                        [ScheduledServiceScheduleId] [int] NOT NULL,
                        [BringingChildren] [bit] NOT NULL,
	                    [SurveyResponse] [nvarchar](100) NULL,
                        [AttendedCampusId] [int] NULL,
                        [AttendedDate] [datetime] NULL,
                        [AttendedServiceScheduleId] [int] NULL,
                        [Guid] [uniqueidentifier] NOT NULL,
	                    [CreatedDateTime] [datetime] NOT NULL,
	                    [ModifiedDateTime] [datetime] NOT NULL,
	                    [CreatedByPersonAliasId] [int] NOT NULL,
	                    [ModifiedByPersonAliasId] [int] NOT NULL,
                        [ForeignKey] [nvarchar](100) NULL,
                        [ForeignGuid] [uniqueidentifier] NULL,
                        [ForeignId] [int] NULL,
                    CONSTRAINT [PK_dbo._church_ccv_PlanAVisit_Visit] PRIMARY KEY CLUSTERED 
                    (
	                    [Id] ASC
                    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
                ) ON [PRIMARY]

                ALTER TABLE [dbo].[_church_ccv_PlanAVisit_Visit]  WITH CHECK ADD  CONSTRAINT [FK_dbo._church_ccv_PlanAVisit_Visit_dbo.PersonAlias_AdultOnePersonAliasId] FOREIGN KEY([AdultOnePersonAliasId])
                REFERENCES [dbo].[PersonAlias] ([Id])

                ALTER TABLE [dbo].[_church_ccv_PlanAVisit_Visit] CHECK CONSTRAINT [FK_dbo._church_ccv_PlanAVisit_Visit_dbo.PersonAlias_AdultOnePersonAliasId]

                ALTER TABLE [dbo].[_church_ccv_PlanAVisit_Visit]  WITH CHECK ADD  CONSTRAINT [FK_dbo._church_ccv_PlanAVisit_Visit_dbo.PersonAlias_AdultTwoPersonAliasId] FOREIGN KEY([AdultTwoPersonAliasId])
                REFERENCES [dbo].[PersonAlias] ([Id])

                ALTER TABLE [dbo].[_church_ccv_PlanAVisit_Visit] CHECK CONSTRAINT [FK_dbo._church_ccv_PlanAVisit_Visit_dbo.PersonAlias_AdultTwoPersonAliasId]

                ALTER TABLE [dbo].[_church_ccv_PlanAVisit_Visit]  WITH CHECK ADD  CONSTRAINT [FK_dbo._church_ccv_PlanAVisit_Visit_dbo.Group_FamilyId] FOREIGN KEY([FamilyId])
                REFERENCES [dbo].[Group] ([Id])

                ALTER TABLE [dbo].[_church_ccv_PlanAVisit_Visit] CHECK CONSTRAINT [FK_dbo._church_ccv_PlanAVisit_Visit_dbo.Group_FamilyId]

                ALTER TABLE [dbo].[_church_ccv_PlanAVisit_Visit]  WITH CHECK ADD  CONSTRAINT [FK_dbo._church_ccv_PlanAVisit_Visit_dbo.Campus_ScheduledCampusId] FOREIGN KEY([ScheduledCampusId])
                REFERENCES [dbo].[Campus] ([Id])

                ALTER TABLE [dbo].[_church_ccv_PlanAVisit_Visit] CHECK CONSTRAINT [FK_dbo._church_ccv_PlanAVisit_Visit_dbo.Campus_ScheduledCampusId]

                ALTER TABLE [dbo].[_church_ccv_PlanAVisit_Visit]  WITH CHECK ADD  CONSTRAINT [FK_dbo._church_ccv_PlanAVisit_Visit_dbo.Schedule_ScheduledServiceScheduleId] FOREIGN KEY([ScheduledServiceScheduleId])
                REFERENCES [dbo].[Schedule] ([Id])

                ALTER TABLE [dbo].[_church_ccv_PlanAVisit_Visit] CHECK CONSTRAINT [FK_dbo._church_ccv_PlanAVisit_Visit_dbo.Schedule_ScheduledServiceScheduleId]

                ALTER TABLE [dbo].[_church_ccv_PlanAVisit_Visit]  WITH CHECK ADD  CONSTRAINT [FK_dbo._church_ccv_PlanAVisit_Visit_dbo.Schedule_AttendedServiceScheduleId] FOREIGN KEY([AttendedServiceScheduleId])
                REFERENCES [dbo].[Schedule] ([Id])

                ALTER TABLE [dbo].[_church_ccv_PlanAVisit_Visit] WITH CHECK ADD CONSTRAINT [FK_dbo._church_ccv_PlanAVisit_Visit_dbo.Campus_AttendedCampusId] FOREIGN KEY([AttendedCampusId])
                REFERENCES [dbo].[Campus] ([Id])

                ALTER TABLE [dbo].[_church_ccv_PlanAVisit_Visit] CHECK CONSTRAINT [FK_dbo._church_ccv_PlanAVisit_Visit_dbo.Campus_AttendedCampusId]

                ALTER TABLE [dbo].[_church_ccv_PlanAVisit_Visit] CHECK CONSTRAINT [FK_dbo._church_ccv_PlanAVisit_Visit_dbo.Schedule_AttendedServiceScheduleId]

                ALTER TABLE [dbo].[_church_ccv_PlanAVisit_Visit]  WITH CHECK ADD  CONSTRAINT [FK_dbo._church_ccv_PlanAVisit_Visit_dbo.PersonAlias_CreatedByPersonAliasId] FOREIGN KEY([CreatedByPersonAliasId])
                REFERENCES [dbo].[PersonAlias] ([Id])

                ALTER TABLE [dbo].[_church_ccv_PlanAVisit_Visit] CHECK CONSTRAINT [FK_dbo._church_ccv_PlanAVisit_Visit_dbo.PersonAlias_CreatedByPersonAliasId]

                ALTER TABLE [dbo].[_church_ccv_PlanAVisit_Visit]  WITH CHECK ADD  CONSTRAINT [FK_dbo._church_ccv_PlanAVisit_Visit_dbo.PersonAlias_ModifiedByPersonAliasId] FOREIGN KEY([ModifiedByPersonAliasId])
                REFERENCES [dbo].[PersonAlias] ([Id])

                ALTER TABLE [dbo].[_church_ccv_PlanAVisit_Visit] CHECK CONSTRAINT [FK_dbo._church_ccv_PlanAVisit_Visit_dbo.PersonAlias_ModifiedByPersonAliasId]
            " );
        }

        public override void Down()
        {
        }
    }
}
