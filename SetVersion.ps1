﻿# 3 PRO Auto Increment version number - backend .net core (Multiple propertyGroups)
param (
    [int]$buildNumber = $(throw "-buildNumber is required."), # the build version, from VSTS build i.e. "974"
    [string]$filePath = $(throw "-filePath is required."), #$PSScriptRoot, # path to the file i.e. 'C:\Users\ben\Code\csproj powershell\MySmallLibrary.csproj'
    [string]$type = $(throw "-type is required. |csproj|nuspec|csprojnuspec|js|ts|") # path to the file i.e. 'C:\Users\ben\Code\csproj powershell\MySmallLibrary.nuspec'
)


function SetCsprojNuspecBuildVersion ([string]$currentFileType, [string]$currentFilePath)
{
    Write-Host "Starting process of generating new version number for the "$type
    Write-Host "New Build: "$buildNumber

    $xml=New-Object XML
    $xml.Load($currentFilePath)

    [string]$oldBuildString="";
    $propertyToSet=-1;
    if($currentFileType -eq 'csproj')
    {
        if($xml.Project.PropertyGroup -isnot [array] -or $xml.Project.PropertyGroup.count -le 1)
        {
            $oldBuildString = $xml.Project.PropertyGroup.Version
        }
        elseif($xml.Project.PropertyGroup.count -gt 1)
        {
            $propertyGroups = $xml.Project.PropertyGroup
            $myBuildNumber = "";
            foreach ($currentPropertyGroup in $propertyGroups)
            {
                $propertyToSet++;
	            if($currentPropertyGroup.Version)
                {
		            $oldBuildString = $currentPropertyGroup.Version;
		            break;
                }
            }
        }
        else
        {
            $(throw "Cannot find version property in csproj file: $filePath");
        }
    }
    elseif($currentFileType -eq 'nuspec')
    {
        $oldBuildString = $xml.package.metadata.version
        $propertyToSet=-2;
    }
    
    Write-Host "Current "$currentFileType" version: "$oldBuildString
    $oldSplitNumber = $oldBuildString.Split(".")
    $majorNumber = $oldSplitNumber[0]
    $minorNumber = $oldSplitNumber[1]
    $maintenanceNumber = $oldSplitNumber[2]
    $revisionNumber = $buildNumber
    $myBuildNumber = $majorNumber + "." + $minorNumber + "." + $maintenanceNumber + "." + $revisionNumber
    
    #Write-Host 'Property to set: '$propertyToSet;

    if($propertyToSet -eq -2)
        {$xml.package.metadata.version = $myBuildNumber;}
    elseif($propertyToSet -eq -1)
        {$xml.Project.PropertyGroup.Version=$myBuildNumber;}
    else
        {$xml.Project.PropertyGroup[$propertyToSet].Version=$myBuildNumber;}
    $xml.Save($currentFilePath)

    Write-Host "Updated "$currentFilePath" and set build to version: "$myBuildNumber
}

function SetJSTSBuildVersion ([string]$currentFileType, [string]$currentFilePath)
{
    Write-Host "Updating build version constant to "$buildNumber
    (Get-Content $currentFilePath).replace('--version--', $buildNumber) | Set-Content $currentFilePath
    Write-Host "Updated "$type" file "$currentFilePath" and set build version to "$buildNumber
}

#script execution
if($type -eq 'csprojnuspec')
{
    Write-Host "type: csprojnuspec";
    #ensuring first file is csproj
    $filePath = $filePath.replace('.nuspec','.csproj')
    SetCsprojNuspecBuildVersion 'csproj' $filePath;
    #ensuring second file is nuspec
    $secondFilePath = $filePath.replace('.csproj','.nuspec')
    SetCsprojNuspecBuildVersion 'nuspec' $secondFilePath;
}
elseif ($type -eq 'csproj' -or $type -eq 'nuspec')
{
    Write-Host "type: "$type;
    SetCsprojNuspecBuildVersion $type $filePath;
}
elseif ($type -eq 'js' -or $type -eq 'ts')
{
    Write-Host "type: "$type;
    SetJSTSBuildVersion $type $filePath;
}
else
{
    $(throw "Unknown -type parameter. |csproj|nuspec|csprojnuspec|js|ts|");
}
