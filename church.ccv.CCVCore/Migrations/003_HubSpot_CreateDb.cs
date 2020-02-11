using Rock.Plugin;

namespace church.ccv.CCVCore.Migrations
{
    [MigrationNumber( 3, "1.7.6" )]
    public class HubSpot_CreateDb : Migration
    {
        public override void Up()
        {
            Sql( @"
                CREATE TABLE [dbo]._church_ccv_HubSpot_HubSpotContact (
		                [Id] [int] IDENTITY(1,1) NOT NULL,
                        [PersonAliasId] [int] NOT NULL,
                        [HubSpotObjectId] [int] NOT NULL,
		                [LastSyncDateTime] [datetime] NULL,
	                    [Guid] [uniqueidentifier] NOT NULL,
	                    [CreatedDateTime] [datetime] NOT NULL,
	                    [ModifiedDateTime] [datetime] NOT NULL,
	                    [CreatedByPersonAliasId] [int] NULL,
	                    [ModifiedByPersonAliasId] [int] NULL,
                        [ForeignKey] [nvarchar](100) NULL,
                        [ForeignGuid] [uniqueidentifier] NULL,
                        [ForeignId] [int] NULL,
	                CONSTRAINT [PK_dbo._church_ccv_HubSpot_HubSpotContact] PRIMARY KEY CLUSTERED 
                    (
	                    [Id] ASC
                    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
                ) ON [PRIMARY]

	            ALTER TABLE [dbo].[_church_ccv_HubSpot_HubSpotContact] WITH CHECK ADD CONSTRAINT [FK_dbo._church_ccv_HubSpot_HubSpotContact_dbo.PersonAlias_PersonAliasId] FOREIGN KEY([PersonAliasId])
	            REFERENCES [dbo].[PersonAlias] ([Id])

	            ALTER TABLE [dbo].[_church_ccv_HubSpot_HubSpotContact] CHECK CONSTRAINT [FK_dbo._church_ccv_HubSpot_HubSpotContact_dbo.PersonAlias_PersonAliasId]
            " );
        }

        public override void Down()
        {
        }
    }
}
