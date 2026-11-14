# Lists the pull requests merged between two refs that changed the app itself (not only docs, workflows or
# repository settings), oldest first, as "Title (#12)" lines for release notes. Dependabot's updates are
# folded into one "Updated libraries" line. Used by release.yml; needs git history and the gh CLI.
param(
    [string] $From,               # previous release tag; empty = everything up to $To
    [string] $To = 'HEAD',
    [string] $Repo = $env:GITHUB_REPOSITORY
)
$ErrorActionPreference = 'Stop'

# What the app is built from: build-test.yml's "Needs a build?" list, without that workflow itself.
$appFiles = '^(CallGuard[^/]*/|[^/]*\.slnx$|Directory\.(Packages|Build)\.(props|targets)$|global\.json$|dotnet-tools\.json$|[Nn]u[Gg]et\.config$)'

# GitHub's merge commits ("Merge pull request #12 from ...") and squash merges ("Title (#12)").
$range = if ($From) { "$From..$To" } else { $To }
$numbers = @(git log $range --first-parent --format=%s | ForEach-Object {
    if ($_ -match '^Merge pull request #(\d+) ') { $Matches[1] }
    elseif ($_ -match '\(#(\d+)\)$') { $Matches[1] }
})
[array]::Reverse($numbers)

$dependencyUpdates = 0
foreach ($number in $numbers) {
    $pr = gh pr view $number --repo $Repo --json title,author,files | ConvertFrom-Json
    if (-not ($pr.files.path -match $appFiles)) { continue }
    if ($pr.author.login -like '*dependabot*') { $dependencyUpdates++; continue }
    "$($pr.title) (#$number)"
}
if ($dependencyUpdates) { 'Updated libraries' }
