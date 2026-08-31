function Assert-FileWritten([string]$path, [int]$errorExitCode)
{
    if (-not (Test-Path $path -PathType Leaf))
    {
        Write-Error "The file `"$path`" was not created."
        Exit $errorExitCode;
    }

    $file = Get-Item $path;
    Write-Host "Written `"$($file.FullName)`" ($($file.Length) bytes)";

    if ($file.Length -eq 0)
    {
        Write-Error "The file `"$($file.FullName)`" is zero length."
        Exit $errorExitCode;
    }
}

$fullVersion = $Env:STRAVAIG_PACKAGE_VERSION
if ([string]::IsNullOrEmpty($fullVersion)){
    Write-Error "The Environment variable STRAVAIG_PACKAGE_FULL_VERSION is not set."
    Exit -1;
}

$currentReleaseNotes = Get-Content "$PSScriptRoot/release-notes/wip-release-notes.md";
$releaseTimestamp = (Get-Date).ToString("dddd, d MMMM, yyyy 'at' HH:mm:ss zzzz");

Write-Host "Writing release notes for version $fullVersion on $releaseTimestamp";
for($i = 0; $i -lt $currentReleaseNotes.Length; $i++)
{
    $line = $currentReleaseNotes[$i];
    if ($line -eq "## Version X")
    {
        $line = "## Version $fullVersion"
        $currentReleaseNotes[$i] = $line;
    }

    if ($line -eq "Date: ???")
    {
        $line = "Date: "+$releaseTimestamp
        $currentReleaseNotes[$i] = $line;
    }
}

Set-Content "$PSScriptRoot/release-notes/release-notes-$fullVersion.md" $currentReleaseNotes -Encoding UTF8 -Force
Assert-FileWritten "$PSScriptRoot/release-notes/release-notes-$fullVersion.md" 1

$fullReleaseNotes = Get-Content "$PSScriptRoot/release-notes/full-release-notes.md";

$preamble = $fullReleaseNotes[0..1];

$currentReleaseNotesExtractLength = $currentReleaseNotes.Length - 1;
$currentReleaseNotesExtract = $currentReleaseNotes[2..$currentReleaseNotesExtractLength]

$existingLength = $fullReleaseNotes.Length - 1;
$existing = $fullReleaseNotes[2..$existingLength];

$fullReleaseNotes = $preamble + $currentReleaseNotesExtract + $existing

Set-Content "$PSScriptRoot/release-notes/full-release-notes.md" $fullReleaseNotes -Encoding UTF8 -Force
Assert-FileWritten "$PSScriptRoot/release-notes/full-release-notes.md" 2


if (-not (Test-Path "$PSScriptRoot/contributors.md" -PathType Leaf))
{
    Write-Error "The file `"$PSScriptRoot/contributors.md`" does not exist."
    Exit 3;
}

$contributors = Get-Content "$PSScriptRoot/contributors.md";
$releaseBody = $currentReleaseNotes + @("", "---", "") + $contributors
Set-Content "$PSScriptRoot/release-body.md" $releaseBody -Encoding UTF8 -Force
Assert-FileWritten "$PSScriptRoot/release-body.md" 4
