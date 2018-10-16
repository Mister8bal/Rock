ALTER PROCEDURE [dbo].[_church_ccv_spJob_SendParentResources]
	@ScheduleId INT

AS
BEGIN
	DECLARE @CommunicationId INT

	-- Only run on weekends to prevent accidents :) (Saturday: 7, Sunday: 1)
	if DATEPART(dw, GETDATE()) = 7 OR DATEPART(dw, GETDATE()) = 1
	BEGIN 
		-- Create Communication
		INSERT INTO Communication (
			[Subject],
			[Status],
			[ReviewedDateTime],
			[CommunicationType],
			[SegmentCriteria],
			[Guid],
			[CreatedDateTime],
			[IsBulkCommunication],
			[MediumDataJson],
			[SenderPersonAliasId]
		)
		SELECT
			'Parent Resource Notification' + ISNULL((SELECT ' (' + Name + ')' FROM Schedule WHERE Id = @ScheduleId), '') AS [Subject],
			3 AS [Status], -- Approved
			DATEADD(minute, -31, GETDATE()) AS [ReviewedDateTime],
			1,
			0,
			NEWID() AS [Guid],
			DATEADD(minute, -31, GETDATE()) AS [CreatedDateTime],
			0 AS [IsBulkCommunication],
			'{
				"FromValue": "12134",
				"Message": "Thanks for coming to CCV Kids today! See what {{ Kids }} learned in service: http://my.ccv.church/parent-kid-resources To opt out, text STOP"
			 }' AS [MediumDataJson],
			 NULL

		-- Original Message
		-- "Message": "Thanks for coming to CCV Kids today! See what {{ Kids }} learned in service: http://my.ccv.church/parent-kid-resources To opt out, text STOP"

		SET @CommunicationID = SCOPE_IDENTITY()

		-- Get all early/later kids and group them by earliest attendance schedule.
		;WITH Kids AS (
			SELECT DISTINCT 
				P.NickName,
				MIN(A.ScheduleId) AS [ScheduleId],
				F.Id AS [FamilyId]
			FROM Person P
			INNER JOIN PersonAlias PA ON PA.PersonId = P.Id
			INNER JOIN Attendance A ON A.PersonAliasId = PA.Id
			INNER JOIN [Group] G ON G.Id = A.GroupId
			INNER JOIN GroupMember FM ON FM.PersonId = P.Id
			INNER JOIN [Group] F ON F.Id = FM.GroupId AND F.GroupTypeId = 10
			WHERE G.GroupTypeId IN (20,46,35,36)
				AND A.DidAttend = 1
				AND CAST(A.StartDateTime AS DATE) = CAST(GETDATE() AS DATE)
			GROUP BY NickName, F.Id
		),
		/*Take all the kids of a family and get their earliest attendance
		  This prevents issues where different kids in the same family
		   attend different times */
		KidsGroupSchedule AS (
			SELECT
				FamilyId,
				(STUFF((SELECT ', ' + NickName 
				 FROM Kids KK
				 WHERE KK.FamilyId = K.FamilyId
				 FOR XML PATH, TYPE).value('.[1]', 'nvarchar(max)'), 1, 2, '' )) AS [Kids],
				MIN(ScheduleId) AS [ScheduleId]
			FROM Kids K
			GROUP BY FamilyId
			HAVING MIN(ScheduleId) = @ScheduleId
		),
		/*Get the parents of these kids that have a cell phone and are opted in 
		  for receiving SMS messages. */
		Parents AS (
			SELECT
				P.Id, 
				dbo.ufnUtility_GetPrimaryPersonAliasId(P.Id) AS [PersonAliasId],
				P.LastName + ', ' + P.NickName AS [Name],
				(SELECT Kids FROM KidsGroupSchedule K WHERE K.FamilyId = G.Id) AS [Kids],
				PN.NumberFormatted,
				PN.IsMessagingEnabled
			FROM Person P
			INNER JOIN GroupMember GM ON GM.PersonId = P.Id
			INNER JOIN [Group] G ON G.Id = GM.GroupId 
				AND G.GroupTypeId = 10
			INNER JOIN PhoneNumber PN ON PN.PersonId = P.Id
				AND PN.NumberTypeValueId = 12
			WHERE GM.GroupRoleId = 3
				AND G.Id IN (SELECT FamilyId FROM KidsGroupSchedule)
				AND PN.IsMessagingEnabled = 1
		)

		--Create Communication Recipients
		INSERT INTO CommunicationRecipient (
			[CommunicationId],
			[Status],
			[AdditionalMergeValuesJson],
			[Guid],
			[CreatedDateTime],
			[PersonAliasId]
		)
		SELECT
			@CommunicationId AS [CommunicationId],
			0 AS [Status], -- Pending
			'{
				"Kids": "' + Left(P.[Kids], Len(P.[Kids]) - charindex(',', Reverse(P.[Kids]))) +
				replace(Right(P.[Kids], charindex(',', Reverse(P.[Kids]))), ',', ' &') + '"
			 }' AS [AdditionalMergeValuesJson], -- This will format the kids' names properly (Bob, Billy & Jane)
			NEWID() AS [Guid],
			DATEADD(minute, -31, GETDATE()) AS [CreatedDateTime],
			P.PersonAliasId AS [PersonAliasId]
		FROM Parents P
	END
END
