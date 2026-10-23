<#
.SYNOPSIS
    Verifies that the development version in Directory.Build.props is not behind the highest stable
    release tag.

.DESCRIPTION
    Reads the <Version> element from Directory.Build.props, enumerates stable git tags matching
    v<major>.<minor>[.<patch>], and fails when the props version is lower than the highest tag.
    Prerelease tags (for example v1.0.0-beta.1) are ignored because CLIHub publishes stable
    releases only.

    The script fails instead of passing silently when the tag history is unavailable: a shallow
    checkout hides release tags, so the CI workflow and the release test job must check out with
    fetch-depth: 0.

.PARAMETER PropsPath
    Props file to read. Defaults to Directory.Build.props under -RepoPath.

.PARAMETER RepoPath
    Git repository used for tag enumeration. Defaults to the current directory.

.PARAMETER MinVersion
    Version to compare against instead of the git-derived highest tag. Skips git entirely, so the
    comparison logic can be exercised locally without pushing tags.

.EXAMPLE
    pwsh ./.github/scripts/Check-DevVersion.ps1

.EXAMPLE
    pwsh ./.github/scripts/Check-DevVersion.ps1 -MinVersion 0.10.0
#>
[CmdletBinding()]
param(
    [Parameter(HelpMessage = 'Props file to read. Defaults to Directory.Build.props under -RepoPath.')]
    [string] $PropsPath,

    [Parameter(HelpMessage = 'Git repository used for tag enumeration. Defaults to the current directory.')]
    [string] $RepoPath = '.',

    [Parameter(HelpMessage = 'Version to compare against instead of the highest stable release tag.')]
    [string] $MinVersion
)

$missingPropsMessage = 'Directory.Build.props not found or version property is missing.'
$tagsUnavailableMessage = 'Cannot enumerate release tags. Ensure the repository has full tag history (fetch-depth: 0) and contains at least one stable release tag.'

# Writes the failure message to the log (plus a GitHub Actions annotation when running in CI) and
# terminates the script with a non-zero exit code.
function Write-CheckFailure {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Message
    )

    Write-Host $Message
    if ($env:GITHUB_ACTIONS -eq 'true') {
        Write-Host ('::error::' + $Message)
    }

    exit 1
}

# Converts a major.minor[.patch] version (optionally carrying a prerelease or build suffix) into a
# comparable [version] value, treating a missing patch as 0. Returns $null for unrecognized text.
function ConvertTo-ComparisonVersion {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Value
    )

    $pattern = '^(?<major>\d+)\.(?<minor>\d+)(\.(?<patch>\d+))?([-+][0-9A-Za-z.-]+)?$'
    $match = [regex]::Match($Value.Trim(), $pattern)
    if (-not $match.Success) {
        return $null
    }

    $patchText = '0'
    if ($match.Groups['patch'].Success) {
        $patchText = $match.Groups['patch'].Value
    }

    try {
        return [version]($match.Groups['major'].Value + '.' + $match.Groups['minor'].Value + '.' + $patchText)
    }
    catch {
        return $null
    }
}

# Runs git in the target repository and reports whether it succeeded. The result object keeps an
# empty but successful run distinguishable from a git failure.
function Invoke-GitCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string] $RepositoryPath,

        [Parameter(Mandatory = $true)]
        [string[]] $Arguments
    )

    $output = @()
    try {
        $output = @(& git -C $RepositoryPath @Arguments 2>$null)
        $succeeded = ($LASTEXITCODE -eq 0)
    }
    catch {
        $output = @()
        $succeeded = $false
    }

    return [pscustomobject]@{
        Succeeded = $succeeded
        Output    = $output
    }
}

if ([string]::IsNullOrWhiteSpace($RepoPath)) {
    $RepoPath = '.'
}

if ([string]::IsNullOrWhiteSpace($PropsPath)) {
    $PropsPath = Join-Path $RepoPath 'Directory.Build.props'
}

$propsVersionText = $null
if (Test-Path -LiteralPath $PropsPath -PathType Leaf) {
    try {
        $document = New-Object System.Xml.XmlDocument
        $document.Load((Resolve-Path -LiteralPath $PropsPath).ProviderPath)

        $versionNodes = $document.SelectNodes('/Project/PropertyGroup/Version')
        if ($null -ne $versionNodes) {
            foreach ($versionNode in $versionNodes) {
                if (-not [string]::IsNullOrWhiteSpace($versionNode.InnerText)) {
                    $propsVersionText = $versionNode.InnerText.Trim()
                    break
                }
            }
        }
    }
    catch {
        $propsVersionText = $null
    }
}

if ([string]::IsNullOrWhiteSpace($propsVersionText)) {
    Write-CheckFailure $missingPropsMessage
}

$propsVersion = ConvertTo-ComparisonVersion -Value $propsVersionText
if ($null -eq $propsVersion) {
    Write-CheckFailure ("Version '{0}' in Directory.Build.props is not a valid SemVer version." -f $propsVersionText)
}

$requiredVersion = $null
$requiredVersionText = $null
$requiredSourceText = $null

if ([string]::IsNullOrWhiteSpace($MinVersion)) {
    $tagResult = Invoke-GitCommand -RepositoryPath $RepoPath -Arguments @('tag', '--list', 'v*')
    if (-not $tagResult.Succeeded) {
        Write-CheckFailure $tagsUnavailableMessage
    }

    $tagPattern = '^v(?<major>\d+)\.(?<minor>\d+)(\.(?<patch>\d+))?$'
    $highestTagName = $null
    foreach ($tag in $tagResult.Output) {
        $tagText = ([string] $tag).Trim()
        if ($tagText -notmatch $tagPattern) {
            continue
        }

        $patchText = '0'
        if ($Matches['patch']) {
            $patchText = $Matches['patch']
        }
        $normalizedTagVersion = $Matches['major'] + '.' + $Matches['minor'] + '.' + $patchText

        $candidate = ConvertTo-ComparisonVersion -Value $normalizedTagVersion
        if ($null -eq $candidate) {
            continue
        }

        if (($null -eq $requiredVersion) -or ($candidate -gt $requiredVersion)) {
            $requiredVersion = $candidate
            $requiredVersionText = $normalizedTagVersion
            $highestTagName = $tagText
        }
    }

    if ($null -eq $requiredVersion) {
        $shallowResult = Invoke-GitCommand -RepositoryPath $RepoPath -Arguments @('rev-parse', '--is-shallow-repository')
        if (-not $shallowResult.Succeeded) {
            Write-CheckFailure $tagsUnavailableMessage
        }

        $shallowText = ($shallowResult.Output -join ' ').Trim().ToLowerInvariant()
        if ($shallowText -eq 'true') {
            Write-CheckFailure $tagsUnavailableMessage
        }

        $requiredVersion = [version]'0.0.0'
        $requiredVersionText = '0.0.0'
    }

    if ($null -ne $highestTagName) {
        $requiredSourceText = 'highest stable release tag: ' + $highestTagName
    }
    else {
        $requiredSourceText = 'no stable release tag; highest version defaults to 0.0.0'
    }
}
else {
    $requiredVersion = ConvertTo-ComparisonVersion -Value $MinVersion
    if ($null -eq $requiredVersion) {
        Write-CheckFailure ("Version '{0}' supplied to -MinVersion is not a valid SemVer version." -f $MinVersion)
    }

    $requiredVersionText = $requiredVersion.ToString(3)
    $requiredSourceText = 'required version: ' + $requiredVersionText + ' (-MinVersion)'
}

if ($propsVersion -lt $requiredVersion) {
    Write-CheckFailure ('Development version {0} in Directory.Build.props is behind the highest release tag {1}. Update Directory.Build.props to {1} or higher before merging.' -f $propsVersionText, $requiredVersionText)
}

Write-Host ('OK: Directory.Build.props version {0} is current ({1}).' -f $propsVersionText, $requiredSourceText)

exit 0
