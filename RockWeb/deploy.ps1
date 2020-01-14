 # This script is run by AppVeyor's deploy agent after the deploy
Import-Module WebAdministration
Import-Module IISAdministration

$rootFolder = "c:\webdata\rock.ccv.church"
$webRoot = "$rootFolder\docs"

Write-Output "Running post-deploy script"
Write-Output "--------------------------------------------------"
Write-Output "Root folder: $rootFolder"
Write-Output "Web root folder: $webRoot"
Write-Output "Running script as: $env:userdomain\$env:username"
 
Write-Host "Ensure that the compilation debug is false"
(Get-Content "$webRoot\web.Compilation.config").Replace('<compilation debug="true"', '<compilation debug="false"') | Set-Content "$webRoot\web.Compilation.config"

If (Test-Path "$webRoot\Content"){
    Write-Host "Deleting newly deployed content directory"
	Remove-Item "$webRoot\Content" -Force -Confirm:$False -Recurse
}
 
Write-Host "Moving Content folder back from temp directory"
Move-Item "$rootFolder\temp\Content" "$webRoot"

Write-Host "Copying web.config to web root"
Copy-Item "$rootFolder\config\web.config" $webRoot -Force -Confirm:$False

Write-Host "Copying web.ConnectionStrings.config to web root"
Copy-Item "$rootFolder\config\web.ConnectionStrings.config" $webRoot -Force -Confirm:$False

# create empty migration flag
New-Item "$webRoot\App_Data\Run.Migration" -type file -force

# if this is production maintain image assets for ccv and stars external theme
if ($env:APPVEYOR_PROJECT_NAME -like "*Production*") {
    $ccvThemeFolder = "church_ccv_External_v8"
    $starsThemeFolder = "com_ccvstars_External_v6"

    Write-Output "Production Deployment - move maintained assets back from temp directory"
    Write-Output "--------------------------------------------------"
    Write-Output "CCV Theme Folder: $ccvThemeFolder"
    Write-Output "Stars Theme Folder: $starsThemeFolder"

    if (Test-Path "$webRoot\Themes\$ccvThemeFolder\Assets\Images"){
        Write-Host "Deleting newly deployed ccv image assets directory"
        Remove-Item "$webRoot\Themes\$ccvThemeFolder\Assets\Images" -Force -Confirm:$False -Recurse
    }

    if (Test-Path "$webRoot\Themes\$starsThemeFolder\Assets\Images"){
        Write-Host "Deleting newly deployed stars image assets directory"
        Remove-Item "$webRoot\Themes\$starsThemeFolder\Assets\Images" -Force -Confirm:$False -Recurse
    }

    Write-Host "Moving ccv image assets back from temp directory"
    Move-Item "$rootFolder\temp\ThemeAssets\ccv\Images" "$webRoot\Themes\$ccvThemeFolder\Assets"

    Write-Host "Moving stars image assets back from temp directory"
    Move-Item "$rootFolder\temp\ThemeAssets\stars\Images" "$webRoot\Themes\$starsThemeFolder\Assets"

}

Write-Host "Adding modify permission for AppPool to site"

# get the IIS App Pool
$manager = Get-IISServerManager
$appPoolName = "RockRMS"
$appPoolSid = $manager.ApplicationPools["$appPoolName"].RawAttributes['applicationPoolSid']
$identifier = New-Object System.Security.Principal.SecurityIdentifier $appPoolSid
$user = $identifier.Translate([System.Security.Principal.NTAccount])

# set permissions
$acl = Get-ACL $webRoot
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule($user,"Modify, Synchronize","ContainerInherit, ObjectInherit","None","Allow")
$acl.SetAccessRule($accessRule)
$acl | Set-ACL $webRoot

Write-Host "Starting Web Publishing Service"
start-service -servicename w3svc


If (Test-Path "$webRoot\deploy.ps1"){
    Write-Host "Deleting deploy.ps1"
	Remove-Item "$webRoot\deploy.ps1"
}

If (Test-Path "$webRoot\before-deploy.ps1"){
    Write-Host "Deleting before-deploy.ps1"
	Remove-Item "$webRoot\before-deploy.ps1"
}

If (Test-Path c:\appveyor){
    Write-Host "Deleting app veyor cache"
	Remove-Item c:\appveyor -Force -Confirm:$False -Recurse
} 
