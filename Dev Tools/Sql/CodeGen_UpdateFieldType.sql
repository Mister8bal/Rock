/* Use this script to help write a migration that includes updates to a FieldType's Name and/or Description */

/* New Field Types*/
select CONCAT('RockMigrationHelper.UpdateFieldType( "' , [Name] , '", "' , [Description] , '", "' , [Assembly] , '", "' , [Class] , '", "' , [Guid] , '");') [New Field Types]
from FieldType
where IsSystem = 0
order by Class


/* All Field Types*/
select CONCAT('RockMigrationHelper.UpdateFieldType( "' , [Name] , '", "' , [Description] , '", "' , [Assembly] , '", "' , [Class] , '", "' , [Guid] , '");') [All Field Types]
from FieldType
order by Class

