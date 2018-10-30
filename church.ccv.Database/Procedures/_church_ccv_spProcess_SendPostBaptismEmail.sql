/*
<doc>
	<summary>
 		This stored procedure sends baptism emails to people 
		who have a baptism date, baptism photo and have not already
		received this email.
	</summary>

	<returns></returns>
	<remarks>	
	</remarks>
	<code>
		EXEC [dbo].[_church_ccv_spProcess_SendPostBaptismEmail]
	</code>
</doc>
*/
ALTER PROCEDURE [dbo].[_church_ccv_spProcess_SendPostBaptismEmail]
AS
BEGIN
    SET NOCOUNT ON;

    -- SETTINGS -------------------------------------------
    DECLARE @EmailFromName VARCHAR(100) = 'Christ''s Church of the Valley'
    DECLARE @EmailFromAddress VARCHAR(100) = 'info@ccv.church'
    DECLARE @EmailSentAttributeId INT = 2519
    -- VARIABLES ------------------------------------------
    DECLARE @CommunicationID INT
    DECLARE @EmailMessage VARCHAR(MAX) = ''
    DECLARE @EmailMergeValues VARCHAR(MAX) = ''
    DECLARE @EmailJson VARCHAR(MAX) = ''
    DECLARE @RecipientPersonAliasID INT = 0
	 DECLARE @PersonID INT = 0
    DECLARE @PersonFirstName VARCHAR(100) = ''
    DECLARE @PersonLastName VARCHAR(100) = ''
    DECLARE @PersonEmail VARCHAR(100) = ''
    DECLARE @PersonAliasGuid VARCHAR(100) = ''
    DECLARE @PersonBaptismPhotoGuid VARCHAR(100) = ''
    DECLARE @IsChild INT = 0
    DECLARE @AssociatePastorFirstName VARCHAR(100) = ''
    DECLARE @AssociatePastorLastName VARCHAR(100) = ''
    DECLARE @AssociatePastorEmail VARCHAR(100) = ''
    DECLARE @AssociatePastorDirectLine VARCHAR(100) = ''

    DECLARE baptismphoto_cursor CURSOR FOR

    WITH CTE AS
	(

    -- First get all children that were baptised. We'll then look up their parent.
	SELECT PA.Id AS [PersonAliasId]
		  ,P.Id
        ,P.FirstName
        ,P.LastName
        ,P.Email
        ,PA.[Guid]
        ,BP.Value
		,DP.FamilyId
	FROM Person P 
	INNER JOIN _church_ccv_Datamart_Person DP ON DP.PersonId = P.Id
   INNER JOIN PersonAlias PA ON PA.Id = dbo.ufnUtility_GetPrimaryPersonAliasId(P.Id)
    INNER JOIN AttributeValue BP ON BP.EntityId = P.Id
        AND BP.AttributeId = 2627 -- Baptism Photo
    INNER JOIN AttributeValue BD ON BD.EntityId = P.Id
        AND BD.AttributeId = 174 -- Baptism Date
    LEFT OUTER JOIN AttributeValue BS ON BS.EntityId = P.Id
        AND BS.AttributeId = 2519 -- Sent Baptism Photo Email
	WHERE (
            BP.Value IS NOT NULL
            AND BP.Value != ''
            )
        AND (
            P.Email IS NOT NULL
            AND P.Email != ''
            )
        AND (BS.Value IS NULL OR BS.Value != 'True')
        AND BD.ValueAsDateTime BETWEEN DATEADD( DAY, -60, GETDATE() ) AND GETDATE() -- Don't email anyone that was baptised more than two months ago
        AND BD.ValueAsDateTime != '1900-01-01 00:00:00.000'
		AND DP.Age < 13
	)

	SELECT (SELECT TOP 1 Id FROM PersonAlias WHERE PersonId = DP.PersonId) AS [PersonAliasId] --Get the person alias Id of the parent
			,C.Id
			,C.FirstName
			,C.LastName
			,DP.Email
			,C.[Guid]
			,C.Value
         ,1 AS [IsChild]
	FROM _church_ccv_Datamart_Person DP
	INNER JOIN CTE C ON C.FamilyId = DP.FamilyId
	WHERE DP.FamilyRole = 'Adult'

	UNION

    -- Now look up all adults that were baptised
	SELECT PA.Id  AS [PersonAliasId]
		,P.Id
		,P.FirstName
		,P.LastName
		,P.Email
		,PA.[Guid]
		,BP.Value
      ,0 AS [IsChild]
	FROM Person P 
	INNER JOIN _church_ccv_Datamart_Person DP ON DP.PersonId = P.Id
   INNER JOIN PersonAlias PA ON PA.Id = dbo.ufnUtility_GetPrimaryPersonAliasId(P.Id)
	INNER JOIN AttributeValue BP ON BP.EntityId = P.Id
		AND BP.AttributeId = 2627 -- Baptism Photo
	INNER JOIN AttributeValue BD ON BD.EntityId = P.Id
		AND BD.AttributeId = 174 -- Baptism Date
	LEFT OUTER JOIN AttributeValue BS ON BS.EntityId = P.Id
		AND BS.AttributeId = 2519 -- Sent Baptism Photo Email
	WHERE (
				BP.Value IS NOT NULL
				AND BP.Value != ''
				)
			AND (
				P.Email IS NOT NULL
				AND P.Email != ''
				)
			AND (BS.Value IS NULL OR BS.Value != 'True')
			AND BD.ValueAsDateTime BETWEEN DATEADD( DAY, -60, GETDATE() ) AND GETDATE() -- Don't email anyone that was baptised more than two moonths ago
			AND BD.ValueAsDateTime != '1900-01-01 00:00:00.000'
			AND DP.Age >= 13

    OPEN baptismphoto_cursor

    FETCH NEXT
    FROM baptismphoto_cursor
    INTO @RecipientPersonAliasID
		,@PersonId
        ,@PersonFirstName
        ,@PersonLastName
        ,@PersonEmail
        ,@PersonAliasGuid
        ,@PersonBaptismPhotoGuid
        ,@IsChild

    WHILE @@FETCH_STATUS = 0
    BEGIN
        -- Grab email template

        -- If it's a child, grab the child version of the email
        IF @IsChild = 1
            BEGIN
              SET @EmailMessage = (
                      SELECT Body
                      FROM SystemEmail
                      WHERE Id = 43
                      )
              -- Escape double quotes to prevent errors
              SET @EmailMessage = REPLACE(@EmailMessage, '"', '\"')
              -- Generate merge fields
              SET @EmailMergeValues = '{
					      "NickName": "' + ISNULL(@PersonFirstName, '') + '",
					      "BaptismPhotoGuid": "' + ISNULL(@PersonBaptismPhotoGuid, '') + '",
					      "PersonAliasGuid": "' + ISNULL(@PersonAliasGuid, '') + '"
				      }'
              -- Generate JSON
              SET @EmailJson = '{
					      "HtmlMessage": "' + ISNULL(@EmailMessage, '') + '",
					      "FromName": "' + ISNULL(@EmailFromName, '') + '",
					      "FromAddress": "' + ISNULL(@EmailFromAddress, '') + '",
					      "ReplyTo": "' + ISNULL(@EmailFromAddress, '') + '",
					      "DefaultPlainText": ""
				      }'
            END
         -- If it's an adult, grab the adult version of the email
         ELSE
            BEGIN
               SET @EmailMessage = (
                         SELECT Body
                         FROM SystemEmail
                         WHERE Id = 16
                         )

               SELECT TOP 1 
                      @AssociatePastorFirstName = dp.Nickname,
                      @AssociatePastorLastName = dp.LastName,
                      @AssociatePastorEmail = dp.Email,
                      @AssociatePastorDirectLine = PN.NumberFormatted
               FROM [dbo]._church_ccv_Datamart_Person dp
               INNER JOIN [dbo].PhoneNumber PN ON PN.PersonId = dp.PersonId
               WHERE dp.PersonId = [dbo]._church_ccv_ufnGetAssociatePastorId_NoDatamart( @PersonId )
               AND PN.NumberTypeValueId = 613

               -- Escape double quotes to prevent errors
               SET @EmailMessage = REPLACE(@EmailMessage, '"', '\"')
               -- Generate merge fields
               SET @EmailMergeValues = '{
					        "NickName": "' + ISNULL(@PersonFirstName, '') + '",
					        "BaptismPhotoGuid": "' + ISNULL(@PersonBaptismPhotoGuid, '') + '",
                       "AssociatePastorFirstName": "' + ISNULL(@AssociatePastorFirstName, '') + '",
                       "AssociatePastorLastName": "' + ISNULL(@AssociatePastorLastName, '') + '",
                       "AssociatePastorEmail": "' + ISNULL(@AssociatePastorEmail, '') + '",
                       "AssociatePastorDirectLine": "' + ISNULL(@AssociatePastorDirectLine, '') + '",
					        "PersonAliasGuid": "' + ISNULL(@PersonAliasGuid, '') + '"
				       }'
               -- Generate JSON
               SET @EmailJson = '{
					        "HtmlMessage": "' + ISNULL(@EmailMessage, '') + '",
					        "FromName": "' + ISNULL(@EmailFromName, '') + '",
					        "FromAddress": "' + ISNULL(@EmailFromAddress, '') + '",
					        "ReplyTo": "' + ISNULL(@EmailFromAddress, '') + '",
					        "DefaultPlainText": ""
				       }'
               END

        -- Insert email message into database
        INSERT INTO Communication (
            [CreatedDateTime]
            ,[CreatedByPersonAliasId]
            ,[Guid]
            ,[IsBulkCommunication]
            ,[MediumDataJson]
            ,[Status]
            ,[Subject]
            ,[SenderPersonAliasId]
            ,[ReviewedDateTime]
			,[CommunicationType]
			,[SegmentCriteria]
            )
        VALUES (
            DATEADD(Minute, - 31, GETDATE())
            ,1
            ,NEWID()
            ,0
            ,@EmailJson
            ,3 --Approved
            ,ISNULL(@PersonFirstName, '') + ' ' + ISNULL(@PersonLastName, '') + ' Baptism'
            ,1
            ,DATEADD(Minute, - 31, GETDATE())
			,1
			,0
            )

        -- Grab the identity number from the last insert
        SET @CommunicationID = SCOPE_IDENTITY()

        INSERT INTO CommunicationRecipient (
            [CommunicationId]
            ,[Status]
            ,[Guid]
            ,[CreatedDateTime]
            ,[ModifiedDateTime]
            ,[PersonAliasId]
            ,[AdditionalMergeValuesJson]
            )
        VALUES (
            @CommunicationID
            ,0 --Pending
            ,NEWID()
            ,DATEADD(Minute, - 31, GETDATE())
            ,DATEADD(Minute, - 31, GETDATE())
            ,@RecipientPersonAliasID
            ,@EmailMergeValues
            )

        --Mark the email as sent
        IF EXISTS (
                SELECT *
                FROM AttributeValue
                WHERE AttributeId = @EmailSentAttributeId
                    AND EntityId = @PersonID
                )
        BEGIN
            UPDATE AttributeValue
            SET Value = 'True'
                ,CreatedDateTime = GETDATE()
            WHERE AttributeId = @EmailSentAttributeId
                AND EntityId = @PersonID
        END
        ELSE
        BEGIN
            INSERT INTO AttributeValue (
                [IsSystem]
                ,[AttributeId]
                ,[EntityId]
                ,[Value]
                ,[Guid]
                ,[CreatedDateTime]
                ,[ModifiedDateTime]
                )
            VALUES (
                0
                ,@EmailSentAttributeId
                ,@PersonID
                ,'True'
                ,NEWID()
                ,GETDATE()
                ,GETDATE()
                )
        END

        FETCH NEXT
        FROM baptismphoto_cursor
        INTO @RecipientPersonAliasID
			   ,@PersonId
            ,@PersonFirstName
            ,@PersonLastName
            ,@PersonEmail
            ,@PersonAliasGuid
            ,@PersonBaptismPhotoGuid
            ,@IsChild
    END

    CLOSE baptismphoto_cursor

    DEALLOCATE baptismphoto_cursor
END
