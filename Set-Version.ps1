$VersionFile = "$PSScriptRoot/version.txt";
$outputFolder = "./out";
$versionEnvFile = "$outputFolder/version-info.env";

# Work out the version number
$nextVersion = Get-Content $VersionFile -ErrorAction Stop
if ($null -eq $nextVersion)
{
    Write-Error "The $VersionFile file is empty"
    Exit 1
}
if ($nextVersion.GetType().BaseType.Name -eq "Array")
{
    $nextVersion = $nextVersion[0]
    Write-Warning "$VersionFile contains more than one line of text. Using the first line."
}
if ($nextVersion -notmatch "^[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}$")
{
    Write-Error "The contents of $VersionFile (`"$nextVersion`") not recognised as a valid version number."
    Exit 2
}

if (-not (Test-Path $outputFolder))
{
    $null = New-Item $outputFolder -Type Directory;
}

$sha = $(git rev-list --tags --max-count=1);
$tag = $(git describe --tags $sha);
if ([string]::IsNullOrEmpty($tag))
{
    $tag = "v0.0.0-preview.0"
    Write-Host "No previous deployment tag found, assuming $tag";
}
else
{
    Write-Host "Last deployment was $tag";
}
$parts = $tag.Split("-preview.");

if ($parts.Length -eq 1)
{
    $runNumber = 1;
}
elseif ($parts.Length -eq 2)
{
    $runNumber = [int]$parts[1];
    if ($parts[0] -ne "v$nextVersion")
    {
        $runNumber = 1;
    }
    else
    {
        $runNumber += 1;
    }
}
else
{
    Write-Error "The last tag was not in a recognisable format. Expected v?.?.?-preview.?, but was $tag";
    Exit 3;
}

$suffix = "preview."
$suffix += $runNumber.ToString();

$previewVersion = "$nextVersion-$suffix";
$assemblyVersion = "$nextVersion.$runNumber"
$envContent = "STRAVAIG_PACKAGE_VERSION=$nextVersion" + [System.Environment]::NewLine +
"STRAVAIG_PACKAGE_VERSION_SUFFIX=$suffix" + [System.Environment]::NewLine +
"STRAVAIG_STABLE_VERSION=$nextVersion" + [System.Environment]::NewLine +
"STRAVAIG_PREVIEW_VERSION=$previewVersion" + [System.Environment]::NewLine +
"STRAVAIG_PREVIEW_ASSEMBLY_VERSION=$assemblyVersion" + [System.Environment]::NewLine +
"STRAVAIG_ASSEMBLY_VERSION=$nextVersion"

Write-Host $envContent;

$envContent | Out-File -FilePath $versionEnvFile -Encoding UTF8
$envContent | Out-File -FilePath $env:GITHUB_ENV -Encoding UTF8 -Append

Exit 0;
