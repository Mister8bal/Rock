USE [RockRMS_20160915]
GO
/****** Object:  UserDefinedFunction [dbo].[_church_ccv_ufnActions_Student_IsPeerLearning]    Script Date: 9/25/2016 4:52:35 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

--IsPeerLearning: Determines if the given person is in at least one group that counts as "Peer Learning".
--This means the group contains the person's "peers" whom they can learn from. Examples: Neighborhood Groups or Young Adult Groups.
--This is as opposed to a Next Step Group, where the person would be learning from a Coach/Mentor, not a peer.
ALTER FUNCTION [dbo].[_church_ccv_ufnActions_Student_IsPeerLearning](@PersonId int)
RETURNS @IsPeerLearningTable TABLE
(
	PersonId int NOT NULL,
	
    NextGen_IsPeerLearning bit, --True if they are, false if they're not
	NextGen_GroupIds varchar(MAX) NULL
)
AS
BEGIN

    -- NEXT GEN GROUP
    DECLARE @NextGen_Group_GroupTypeId int = 94
    DECLARE @NextGen_GroupIds varchar(MAX)
	
    -- Get all groups the person is peer learning in
    DECLARE @NextGen_GroupIdTable table( id int )
	INSERT INTO @NextGen_GroupIdTable( id )
	SELECT GroupId
	FROM [dbo].GroupMember gm
		INNER JOIN [dbo].[Group] g ON g.Id = gm.GroupId
		INNER JOIN [dbo].[GroupTypeRole] gtr ON gtr.Id = gm.GroupRoleId
	WHERE 
        g.GroupTypeId = @NextGen_Group_GroupTypeId AND
		gm.PersonId = @PersonId AND 
		gm.GroupMemberStatus != 0 AND --Make sure they aren't InActive. (pending OR active are fine)
		g.IsActive = 1 --Make sure the GROUP IS active

    -- build a comma delimited string with the groups
	SELECT @NextGen_GroupIds = COALESCE(@NextGen_GroupIds + ', ', '' ) + CONVERT(nvarchar(MAX), id)
	FROM @NextGen_GroupIdTable

    -- if there's at least one group, then the person's peer learning
    DECLARE @NextGen_IsPeerLearning bit
	
	IF LEN(@NextGen_GroupIds) > 0 
		SET @NextGen_IsPeerLearning = 1
	ELSE
		SET @NextGen_IsPeerLearning = 0
    ----------------------


	--Put the results into a single row we'll return
	INSERT INTO @IsPeerLearningTable( 
        PersonId, 
        
        NextGen_IsPeerLearning, 
        NextGen_GroupIds
    )
	SELECT 
		@PersonId, 
		
        @NextGen_IsPeerLearning, 
		@NextGen_GroupIds

	RETURN;
END
