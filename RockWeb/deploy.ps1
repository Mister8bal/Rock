# This script is run by AppVeyor's deploy agent after the deploy
Import-Module WebAdministration
Import-Module IISAdministration


$rootfolder = "c:\webdata\rock.ccv.church"
$webroot = "$rootfolder\docs"

Write-Output "Running post-deploy script"
Write-Output "--------------------------------------------------"
Write-Output "Root folder: $rootfolder"
Write-Output "Web root folder: $webroot"
Write-Output "Running script as: $env:userdomain\$env:username"
 
Write-Host "Ensure that the compilation debug is false"
(Get-Content "$webroot\web.Compilation.config").Replace('<compilation debug="true"', '<compilation debug="false"') | Set-Content "$webroot\web.Compilation.config"

If (Test-Path "$webroot\Content"){
    Write-Host "Deleting newly deployed content directory"
	Remove-Item "$webroot\Content" -Force -Confirm:$False -Recurse
}
 
Write-Host "Moving Content folder back from temp directory"
Move-Item "$rootfolder\temp\Content" "$webroot"

Write-Host "Copying web.config to web root"
Copy-Item "$rootfolder\config\web.config" $webroot -Force -Confirm:$False

Write-Host "Copying web.ConnectionStrings.config to web root"
Copy-Item "$rootfolder\config\web.ConnectionStrings.config" $webroot -Force -Confirm:$False

# create empty migration flag
New-Item "$webroot\App_Data\Run.Migration" -type file -force

Write-Host "Adding modify permission for AppPool to site"

# get the IIS App Pool
$manager = Get-IISServerManager
$appPoolName = "RockRMS"
$appPoolSid = $manager.ApplicationPools["$appPoolName"].RawAttributes['applicationPoolSid']
$identifier = New-Object System.Security.Principal.SecurityIdentifier $appPoolSid
$user = $identifier.Translate([System.Security.Principal.NTAccount])

# set permissions
$path = "C:\webdata\rock.ccv.church\docs"
$acl = Get-ACL $path
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule($user,"Modify, Synchronize","ContainerInherit, ObjectInherit","None","Allow")
$acl.SetAccessRule($accessRule)
$acl | Set-ACL $path

Write-Host "Starting Web Publishing Service"
start-service -servicename w3svc


If (Test-Path "$webroot\deploy.ps1"){
    Write-Host "Deleting deploy.ps1"
	Remove-Item "$webroot\deploy.ps1"
}

If (Test-Path "$webroot\before-deploy.ps1"){
    Write-Host "Deleting before-deploy.ps1"
	Remove-Item "$webroot\before-deploy.ps1"
}

If (Test-Path c:\appveyor){
    Write-Host "Deleting app veyor cache"
	Remove-Item c:\appveyor -Force -Confirm:$False -Recurse
}