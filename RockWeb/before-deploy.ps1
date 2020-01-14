 # This script is run by AppVeyor's deploy agent before the deploy
Import-Module WebAdministration

$rootFolder = "c:\webdata\rock.ccv.church"
$webRoot = "$rootFolder\docs"

Write-Output "Running pre-deploy script"
Write-Output "--------------------------------------------------"
Write-Output "Root folder: $rootFolder"
Write-Output "Web root folder: $webRoot"
Write-Output "Running script as: $env:userdomain\$env:username"

# stop execution of the deploy if the moves fail
$ErrorActionPreference = "Stop"

# stop web publishing service - needed to allow the deploy to overwrite the sql server spatial types
Write-Host "Stopping Web Publishing Service"
stop-service -servicename w3svc

If (Test-Path "$rootFolder\temp\Content"){
    Write-Host "Cleaning up temp content folder"
	Remove-Item "$rootFolder\temp\Content" -Force -Confirm:$False -Recurse
}

Write-Host "Backing up web.config"
Copy-Item "$webRoot\web.config" "$rootFolder\config" -Force -Confirm:$False

Write-Host "Backing up web.ConnectionStrings.config"
Copy-Item "$webRoot\web.ConnectionStrings.config" "$rootFolder\config" -Force -Confirm:$False
 
Write-Host "Moving content folder to temp directory"
Move-Item "$webRoot\Content" "$rootFolder\temp"

# if this is production maintain image assets for ccv and stars external theme
if ($env:APPVEYOR_PROJECT_NAME -like "*Production*") {
    $ccvThemeFolder = "church_ccv_External_v8"
    $starsThemeFolder = "com_ccvstars_External_v6"

    Write-Output "Production Deployment - maintain image assets for ccv and stars"
    Write-Output "--------------------------------------------------"
    Write-Output "CCV Theme Folder: $ccvThemeFolder"
    Write-Output "Stars Theme Folder: $starsThemeFolder"

    If (Test-Path "$rootFolder\temp\ThemeAssets"){
        Write-Host "Cleaning up temp theme assets folder"
	    Remove-Item "$rootFolder\temp\ThemeAssets" -Force -Confirm:$False -Recurse
    }

    Write-Host "Moving CCV Theme Asset Images folder to temp directory"
    #ensure we have a folder ot move to
    if (!(Test-Path "$rootFolder\temp\ThemeAssets\ccv")){
        New-Item "$rootFolder\temp\ThemeAssets\ccv" -ItemType "directory" -Confirm:$False -Force
    }
    Move-Item "$webRoot\Themes\$ccvThemeFolder\Assets\Images" "$rootFolder\temp\ThemeAssets\ccv\" -Force -Confirm:$False

    Write-Host "Moving Stars Theme Asset Images folder to temp directory"
    if (!(Test-Path "$rootFolder\temp\ThemeAssets\stars")){
        New-Item "$rootFolder\temp\ThemeAssets\stars" -ItemType "directory" -Confirm:$False -Force
    }
    Move-Item "$webRoot\Themes\$starsThemeFolder\Assets\Images" "$rootFolder\temp\ThemeAssets\stars\" -Force -Confirm:$False
} 
