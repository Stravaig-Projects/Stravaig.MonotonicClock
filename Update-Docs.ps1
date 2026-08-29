# Fix up the contributions for the docs.
Write-Host "& `"$PSscriptRoot/list-contributors.ps1`" -HideAKAs;"
& "$PSscriptRoot/list-contributors.ps1" -HideAKAs;
$contrib = Get-Content "$PSScriptRoot/contributors.md"
for ($i = 0; $i -lt $contrib.Count; $i++) {
    $line = $contrib[$i];
    if ($line.Contains(":octocat:")) { $line = $line.Replace(":octocat:", "* ")}
    if ($line.Contains(":date:")) { $line = $line.Replace(":date:", "* ")}

    $contrib[$i] = $line
}
$frontMatter = @(
"---"
"layout: default"
"title: Contributors"
"---"
""
);

$contrib = $frontMatter + $contrib;

Set-Content -Path "$PSScriptRoot/docs/contributors.md" -Value $contrib -Encoding UTF8 -Force

# Set up the release notes docs.
$releaseNotesOriginal = "$PSScriptRoot/release-notes/release-notes-*.md"
$releaseNotesDest = "$PSScriptRoot/docs/release-notes"
if (-not (Test-Path $releaseNotesDest))
{
    New-Item $releaseNotesDest -ItemType Directory
}

$releaseNotesIndexFile = @(
"---"
"layout: default"
"title: Release Notes"
"---"
""
"# Release Notes"
""
"The releases on this package most recent first."
""
);

Get-ChildItem "$releaseNotesOriginal" |
    Sort-Object -Descending -Property Name |
    ForEach-Object -Process {
        $name = $_.Name;
        $releaseNoteFile = Get-Content $_.FullName;
        $releaseDate = ($releaseNoteFile |
          Where-Object {$_.StartsWith("Date: ")}).Substring(6)

        $null = $name -match "release-notes-(?<version>[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3})\.md";
        $version = $Matches.version;
        $releaseNotesIndexFile += "- **[v$version]($name) released on $releaseDate**";
        $releaseNotesIndexFile += "  - [GitHub Release](https://github.com/Stravaig-Projects/$($ENV:REPO_NAME)/releases/tag/v$version)"
        
        $frontMatter = @(
            "---"
            "layout: default"
            "title: Release Notes v$version"
            "---"
            ""
            );
        $releaseNoteFile = $frontMatter + $releaseNoteFile;
        $destFileName = "$releaseNotesDest/$($_.Name)";
        Set-Content $destFileName $releaseNoteFile -Encoding UTF8 -Force
    }

Set-Content "$PSScriptRoot/docs/release-notes/index.md" $releaseNotesIndexFile -Encoding UTF8 -Force