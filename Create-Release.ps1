[CmdletBinding()]
param
(
    [parameter(Mandatory=$false)]
    [switch]$IsDraft,

    [parameter(Mandatory=$false)]
    [switch]$IsPrerelease,

    [ValidateScript({Test-Path $_ -PathType Leaf})]
    [parameter(Mandatory=$true)]
    [string]$NotesFile,

    [ValidateScript({Test-Path $_ -PathType Leaf})]
    [parameter(Mandatory=$false)]
    [string[]]$Assets
)

function Invoke-GitHub([string]$ghArgs)
{
    $pinfo = New-Object System.Diagnostics.ProcessStartInfo
    $pinfo.FileName = "gh"
    $pinfo.UseShellExecute = $false
    $pinfo.Arguments = $ghArgs
    Write-Host $pinfo.Arguments
    $ghProcess = New-Object System.Diagnostics.Process
    $ghProcess.StartInfo = $pinfo
    $null = $ghProcess.Start();
    $ghProcess.WaitForExit();
    return $ghProcess.ExitCode;
}

if ([string]::IsNullOrWhiteSpace($Env:GITHUB_TOKEN))
{
    Write-Error "GITHUB_TOKEN environment variable is missing."
    Exit 1;
}

if ([string]::IsNullOrWhiteSpace($Env:GITHUB_SHA))
{
    Write-Error "GITHUB_SHA is missing."
    Exit 2;
}
$Commitish = $Env:GITHUB_SHA;

if ([string]::IsNullOrWhiteSpace($Env:STRAVAIG_STABLE_VERSION))
{
    Write-Error "STRAVAIG_STABLE_VERSION is missing."
    Exit 3;
}

if ([string]::IsNullOrWhiteSpace($Env:STRAVAIG_PREVIEW_VERSION))
{
    Write-Error "STRAVAIG_PREVIEW_VERSION is missing."
    Exit 4;
}

if ($IsPrerelease)
{
    $TagName = "v" + $Env:STRAVAIG_PREVIEW_VERSION
}
else
{
    $TagName = "v" + $Env:STRAVAIG_STABLE_VERSION
}


$ghArgs = "release create `"$TagName`""
foreach($assetPath in $Assets)
{
    $specificAssets = Get-Item $assetPath;
    foreach($specificAsset in $specificAssets)
    {
        if (-not $specificAsset.PSIsContainer)
        {
            $fileName = $specificAsset.FullName;
            $ghArgs += " `"$fileName`""
        }
        else
        {
            Write-Verbose "Skipping `"$specificAsset`" as it refers to a directory. Must provide paths to files."
        }
    }
}

$ghArgs += " --title `"Release of $TagName`" --target $Commitish --notes-file `"$NotesFile`""
if ($IsDraft)
{
    $ghArgs += " --draft"
}
if ($IsPrerelease)
{
    $ghArgs += " --prerelease"
}
$exitCode = Invoke-GitHub $ghArgs
if ($exitCode -ne 0)
{
    Write-Error "Failed to create a release."
    Exit $exitCode;
}
