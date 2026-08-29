Clear-Host;

$updateDocsScript = [System.IO.Path]::GetFullPath("$PSScriptRoot/../../Update-Docs.ps1");
Write-Host("& `"$updateDocsScript`";");
& "$updateDocsScript";

$docsPath = [System.IO.Path]::GetFullPath("$PSScriptRoot/../");
$sitePath = [System.IO.Path]::GetFullPath("$PSScriptRoot/../_site");

Write-Host "& jekyll build --source `"$docsPath`" --destination `"$sitePath`" --verbose;"
& jekyll build --source "$docsPath" --destination "$sitePath" --verbose;

Write-Host "& jekyll serve --source `"$docsPath`" --destination `"$sitePath`" --port $($ENV:JEKYLL_PORT);"
& jekyll serve --source "$docsPath" --destination "$sitePath" --port $ENV:JEKYLL_PORT;