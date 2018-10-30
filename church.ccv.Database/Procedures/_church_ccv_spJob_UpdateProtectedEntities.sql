-- This will iterate over the ProtectedEntity table. For any entity with a newer modified date than what ProtectedEntity has,
-- an email alert will be sent out, and the modifed date in the ProtectedEntity table will be updated.
ALTER PROCEDURE [dbo].[_church_ccv_spJob_UpdateProtectedEntities]
	
AS
BEGIN

    SET NOCOUNT ON;

    -- VARIABLES
    DECLARE @ProtectedEntity_Id int
    DECLARE @Entity_TableName NVARCHAR(MAX)
    DECLARE @Entity_TableColumn NVARCHAR(MAX)
    DECLARE @Entity_EntityId NVARCHAR(MAX)
    DECLARE @Entity_LastModified NVARCHAR(MAX)
    DECLARE @Entity_ContactPersonAliasId NVARCHAR(MAX)
    DECLARE @Entity_Desc NVARCHAR(MAX)
    DECLARE @Entity_Url NVARCHAR(MAX)

    DECLARE c0 CURSOR 
    FOR
        SELECT [Id],
               [Entity_Table],
               [Entity_Column],
               [Entity_Id],
               [Entity_LastModified],
               [Entity_ContactPersonAliasId],
               [Entity_Description],
               [Entity_Url]
        FROM [dbo]._church_ccv_Protected_Entity

        OPEN c0

        -- Get the protected entity row
        FETCH NEXT
        FROM c0
        INTO @ProtectedEntity_Id,
             @Entity_TableName,
             @Entity_TableColumn,
             @Entity_EntityId,
             @Entity_LastModified,
             @Entity_ContactPersonAliasId,
             @Entity_Desc,
             @Entity_Url

        WHILE @@FETCH_STATUS = 0
        BEGIN

            -- Now build dynamic sql that will read that entity from its table,
            -- and tell us whether it's been updated since we last time stamp'd it.
            DECLARE @Source_ModifiedDateTime datetime = NULL
            DECLARE @DynamicSql NVARCHAR(MAX) = ''

            SELECT @DynamicSql = 
                'SELECT @SourceLastModified = t.ModifiedDateTime
                 FROM dbo.' + QUOTENAME( @Entity_TableName ) + ' t ' + 
                'WHERE t.' + QUOTENAME( @Entity_TableColumn ) + ' = ' + @Entity_EntityId + ' AND ' + 
                '(t.ModifiedDateTime IS NOT NULL AND CAST(t.ModifiedDateTime as varchar(100)) > ' + '''' + CAST(@Entity_LastModified as varchar(100)) + ''')'

            EXEC sp_executesql @DynamicSql, N'@SourceLastModified datetime out', @Source_ModifiedDateTime out


            -- If Source_ModifiedDateTime isn't null, then it was updated since our last timestamp.
            -- Therefore, update our record for this entity, and send an email address
            IF @Source_ModifiedDateTime IS NOT NULL
                BEGIN
                    UPDATE _church_ccv_Protected_Entity
                    SET _church_ccv_Protected_Entity.Entity_LastModified = @Source_ModifiedDateTime
                    WHERE _church_ccv_Protected_Entity.Id = @ProtectedEntity_Id

                    -- Generate a communication
			        DECLARE @HTMLMessage NVARCHAR(MAX) = 
                            --Email Header--
                            (SELECT [Value] FROM AttributeValue WHERE AttributeValue.AttributeId = 140) + 
                            'This is to alert you that <strong>' + @Entity_Desc + '</strong> has been altered.<br>' +
                            '<a href="' + @Entity_Url + '">Click here to view it.</a>' +
                            (SELECT [Value] FROM AttributeValue WHERE AttributeValue.AttributeId = 141)
		
			        -- Escape double quotes to prevent errors
			        SET @HTMLMessage = REPLACE(@HTMLMessage, '"', '\"')
                    
                    -- Generate JSON
			        DECLARE @EmailJson NVARCHAR(MAX) =
				        '{
					        "HtmlMessage": "' + ISNULL(@HTMLMessage, '') + '",
					        "FromName": "' + 'System' + '",
					        "FromAddress": "' + 'info@ccv.church' + '",
					        "ReplyTo": "' + 'info@ccv.church' + '",
					        "DefaultPlainText": ""
				        }'

                    --Insert email message into database
			        INSERT INTO Communication
			        (
				        [CreatedDateTime],
				        [CreatedByPersonAliasId],
				        [Guid],
				        [IsBulkCommunication],
				        [Status],
				        [Subject],
				        [SenderPersonAliasId],
				        [ReviewedDateTime],
				        [MediumDataJson],
						[CommunicationType],
						[SegmentCriteria]
			        )
			        VALUES
			        (
				        DATEADD(Minute, -31, GETDATE()),
				        1,--todo
				        NEWID(),
				        0,
				        3, --Approved
				        'Alert: ' + @Entity_Desc + ' has been altered.',
				        1,
				        DATEADD(Minute, -31, GETDATE()),
				        @EmailJson,
						1,
						0
			        )

                    --Grab the identity number from the last insert
			        DECLARE @CommunicationID INT = SCOPE_IDENTITY()

			        INSERT INTO CommunicationRecipient
			        (	
				        [CommunicationId],
				        [Status],
				        [Guid],
				        [CreatedDateTime],
				        [ModifiedDateTime],
				        [CreatedByPersonAliasId],
				        [PersonAliasId],
                        [AdditionalMergeValuesJson]
			        )
			        VALUES
			        (	
				        @CommunicationID,
				        0, --Pending
				        NEWID(),
				        DATEADD(Minute, -31, GETDATE()),
				        DATEADD(Minute, -31, GETDATE()),
				        1,
				        @Entity_ContactPersonAliasId,
                        '{}'
			        )
                END


            -- Get the next protected entity row
            FETCH NEXT
            FROM c0
            INTO @ProtectedEntity_Id,
                 @Entity_TableName,
                 @Entity_TableColumn,
                 @Entity_EntityId,
                 @Entity_LastModified,
                 @Entity_ContactPersonAliasId,
                 @Entity_Desc,
                 @Entity_Url
        
        END
        CLOSE c0
        DEALLOCATE c0
END
