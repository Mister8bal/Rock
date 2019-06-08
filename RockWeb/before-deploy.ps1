# This script is run by AppVeyor's deploy agent before the deploy
Import-Module WebAdministration

$rootfolder = "c:\webdata\rock.ccv.church"
$webroot = "$rootfolder\docs"

Write-Output "Running pre-deploy script"
Write-Output "--------------------------------------------------"
Write-Output "Root folder: $rootfolder"
Write-Output "Web root folder: $webroot"
Write-Output "Running script as: $env:userdomain\$env:username"

# stop execution of the deploy if the moves fail
$ErrorActionPreference = "Stop"

# stop web publishing service - needed to allow the deploy to overwrite the sql server spatial types
Write-Host "Stopping Web Publishing Service"
stop-service -servicename w3svc

If (Test-Path "$rootfolder\temp\Content"){
    Write-Host "Cleaning up temp content folder"
	Remove-Item "$rootfolder\temp\Content" -Force -Confirm:$False -Recurse
}

If (Test-Path "$rootfolder\temp\Cache"){
    Write-Host "Cleaning up temp cache folder"
	Remove-Item "$rootfolder\temp\Cache" -Force -Confirm:$False -Recurse
}

Write-Host "Backing up web.config"
Copy-Item "$webroot\web.config" "$rootfolder\config" -Force -Confirm:$False

Write-Host "Backing up web.ConnectionStrings.config"
Copy-Item "$webroot\web.ConnectionStrings.config" "$rootFolder\config" -Force -Confirm:$False
 
Write-Host "Moving content folder to temp directory"
Move-Item "$webroot\Content" "$rootfolder\temp"


