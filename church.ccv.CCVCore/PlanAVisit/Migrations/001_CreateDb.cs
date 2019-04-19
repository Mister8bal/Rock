using Rock.Plugin;

namespace church.ccv.CommandCenter.Migrations
{
    [MigrationNumber( 1, "1.7.6" )]
    public class CreateDb : Migration
    {
        public override void Up()
        {
            Sql( @"
    CREATE TABLE [dbo].[_church_ccv_PlanAVisit_Visit](
	    [Id] [int] IDENTITY(1,1) NOT NULL,
        [PersonAliasId] [int] NOT NULL,
        [FamilyId] [int] NOT NULL,
	    [CampusId] [int] NOT NULL,
	    [ScheduledDate] [datetime] NOT NULL,
        [AttendedDate] [datetime] NULL,
        [ServiceTimeScheduleId] [int] NOT NULL,
        [BringingSpouse] [bit] NOT NULL,
        [BringingChildren] [bit] NOT NULL,
	    [SurveyResponse] [nvarchar](100) NULL,
        [Guid] [uniqueidentifier] NOT NULL,
	    [CreatedDateTime] [datetime] NOT NULL,
	    [ModifiedDateTime] [datetime] NOT NULL,
	    [CreatedByPersonAliasId] [int] NOT NULL,
	    [ModifiedByPersonAliasId] [int] NOT NULL,
     CONSTRAINT [PK_dbo._church_ccv_PlanAVisit_Visit] PRIMARY KEY CLUSTERED 
    (
	    [Id] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
    ) ON [PRIMARY]

    ALTER TABLE [dbo].[_church_ccv_PlanAVisit_Visit]  WITH CHECK ADD  CONSTRAINT [FK_dbo._church_ccv_PlanAVisit_Visit_dbo.PersonAlias_PersonAliasId] FOREIGN KEY([PersonAliasId])
    REFERENCES [dbo].[PersonAlias] ([Id])

    ALTER TABLE [dbo].[_church_ccv_PlanAVisit_Visit] CHECK CONSTRAINT [FK_dbo._church_ccv_PlanAVisit_Visit_dbo.PersonAlias_PersonAliasId]

    ALTER TABLE [dbo].[_church_ccv_PlanAVisit_Visit]  WITH CHECK ADD  CONSTRAINT [FK_dbo._church_ccv_PlanAVisit_Visit_dbo.Group_FamilyId] FOREIGN KEY([FamilyId])
    REFERENCES [dbo].[Group] ([Id])

    ALTER TABLE [dbo].[_church_ccv_PlanAVisit_Visit] CHECK CONSTRAINT [FK_dbo._church_ccv_PlanAVisit_Visit_dbo.Group_FamilyId]

    ALTER TABLE [dbo].[_church_ccv_PlanAVisit_Visit]  WITH CHECK ADD  CONSTRAINT [FK_dbo._church_ccv_PlanAVisit_Visit_dbo.Campus_CampusId] FOREIGN KEY([CampusId])
    REFERENCES [dbo].[Campus] ([Id])

    ALTER TABLE [dbo].[_church_ccv_PlanAVisit_Visit] CHECK CONSTRAINT [FK_dbo._church_ccv_PlanAVisit_Visit_dbo.Campus_CampusId]

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
