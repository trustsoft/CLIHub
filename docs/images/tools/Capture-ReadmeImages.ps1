#Requires -Version 5.1
<#
.SYNOPSIS
    Regenerates the README image set (app captures, banner, workflow, social preview).

.DESCRIPTION
    Builds the Debug application, seeds a throwaway configuration and mock workspace, runs the
    debug-only in-process capture mode, applies the 12 px rounded-corner mask, renders the HTML
    images with headless Chrome, and restores the user's configuration and logo cache.

    The in-process capture renders at 144 DPI (1.5×), so it does not require a 150% display scale
    and works on a locked or headless desktop where screen-copy and PrintWindow return black frames.

.PARAMETER RepoRoot
    Repository root. Defaults to three levels above this script.

.PARAMETER MockWorkspaceRoot
    Root folder used by the illustrative projects and the watched directory. Defaults to
    D:\Projects\Acme.

.PARAMETER OutputDirectory
    Image directory. Defaults to docs/images under the repository root.

.PARAMETER CaptureDirectory
    Working directory for backups and raw captures. Defaults to a folder under %TEMP%.

.PARAMETER SkipBuild
    Reuse the existing Debug build instead of running dotnet build.

.PARAMETER SkipWebImages
    Keep banner.png, workflow.png, and social-preview.png unchanged.

.PARAMETER KeepMockWorkspace
    Do not delete the mock workspace when the script created it.
#>
[CmdletBinding()]
param(
    [string]$RepoRoot,
    [string]$MockWorkspaceRoot = 'D:\Projects\Acme',
    [string]$OutputDirectory,
    [string]$CaptureDirectory = (Join-Path $env:TEMP 'CLIHub-readme-capture'),
    [switch]$SkipBuild,
    [switch]$SkipWebImages,
    [switch]$KeepMockWorkspace
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

Add-Type -AssemblyName System.Drawing

if (-not $RepoRoot) {
    $scriptRoot = $PSScriptRoot
    if (-not $scriptRoot) {
        $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
    }

    $RepoRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..')).Path
}

if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $RepoRoot 'docs\images'
}

$repoFull = [System.IO.Path]::GetFullPath($RepoRoot)
$outputFull = [System.IO.Path]::GetFullPath($OutputDirectory)
if (-not $outputFull.StartsWith($repoFull, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "OutputDirectory must be inside the repository: $outputFull"
}

$RepoRoot = $repoFull
$OutputDirectory = $outputFull
$MockWorkspaceRoot = [System.IO.Path]::GetFullPath($MockWorkspaceRoot)
$CaptureDirectory = [System.IO.Path]::GetFullPath($CaptureDirectory)

if (-not (Test-Path (Join-Path $repoFull 'CLIHub.sln'))) {
    throw "CLIHub.sln was not found under $repoFull"
}

$existing = Get-Process -Name CLIHub -ErrorAction SilentlyContinue
if ($existing) {
    throw "Close CLIHub before running this harness: PID $($existing.Id -join ', ')."
}

$appDataRoot = Join-Path $env:APPDATA 'CLIHub'
if (-not $appDataRoot.EndsWith('CLIHub', [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Unexpected CLIHub data root: $appDataRoot"
}

$configPath = Join-Path $appDataRoot 'config.json'
$logoCachePath = Join-Path $appDataRoot 'cache\logos.json'
$exePath = Join-Path $repoFull 'artifacts\CLIHub\Debug\net10.0-windows\CLIHub.exe'
$defaultLogoPath = Join-Path $repoFull 'artifacts\CLIHub\Debug\net10.0-windows\default-logo.png'

New-Item -ItemType Directory -Path $CaptureDirectory -Force | Out-Null
$backupDirectory = Join-Path $CaptureDirectory 'backup'
New-Item -ItemType Directory -Path $backupDirectory -Force | Out-Null
$backupConfig = Join-Path $backupDirectory 'config.json'
$backupLogoCache = Join-Path $backupDirectory 'logos.json'
$hadConfig = Test-Path $configPath
$hadLogoCache = Test-Path $logoCachePath
if ($hadConfig) {
    Copy-Item $configPath $backupConfig -Force
}

if ($hadLogoCache) {
    Copy-Item $logoCachePath $backupLogoCache -Force
}

$workspaceExisted = Test-Path $MockWorkspaceRoot
$workspaceCreatedHere = -not $workspaceExisted

function Restore-File {
    param(
        [string]$Backup,
        [string]$Target,
        [bool]$Existed
    )

    if ($Existed) {
        Copy-Item $Backup $Target -Force
        return
    }

    Remove-Item $Target -Force -ErrorAction SilentlyContinue
}

function New-RoundedLetterLogo {
    param(
        [string]$Path,
        [string]$Letter,
        [string]$Color
    )

    $bitmap = [System.Drawing.Bitmap]::new(64, 64, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    try {
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        try {
            $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
            $graphics.Clear([System.Drawing.Color]::Transparent)

            $brush = [System.Drawing.SolidBrush]::new([System.Drawing.ColorTranslator]::FromHtml($Color))
            try {
                $shape = [System.Drawing.Drawing2D.GraphicsPath]::new()
                try {
                    $rect = [System.Drawing.Rectangle]::new(2, 2, 60, 60)
                    $radius = 14
                    $diameter = $radius * 2
                    $shape.AddArc($rect.X, $rect.Y, $diameter, $diameter, 180, 90)
                    $shape.AddArc($rect.Right - $diameter, $rect.Y, $diameter, $diameter, 270, 90)
                    $shape.AddArc($rect.Right - $diameter, $rect.Bottom - $diameter, $diameter, $diameter, 0, 90)
                    $shape.AddArc($rect.X, $rect.Bottom - $diameter, $diameter, $diameter, 90, 90)
                    $shape.CloseFigure()
                    $graphics.FillPath($brush, $shape)
                }
                finally {
                    $shape.Dispose()
                }
            }
            finally {
                $brush.Dispose()
            }

            $font = [System.Drawing.Font]::new('Segoe UI', 30, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
            try {
                $format = [System.Drawing.StringFormat]::new()
                try {
                    $format.Alignment = [System.Drawing.StringAlignment]::Center
                    $format.LineAlignment = [System.Drawing.StringAlignment]::Center
                    $graphics.DrawString(
                        $Letter,
                        $font,
                        [System.Drawing.Brushes]::White,
                        [System.Drawing.RectangleF]::new(0, 0, 64, 64),
                        $format)
                }
                finally {
                    $format.Dispose()
                }
            }
            finally {
                $font.Dispose()
            }
        }
        finally {
            $graphics.Dispose()
        }

        $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $bitmap.Dispose()
    }
}

function Add-RoundedCorners {
    param(
        [string]$InputPath,
        [string]$OutputPath,
        [int]$Radius = 12
    )

    $source = [System.Drawing.Bitmap]::FromFile($InputPath)
    try {
        $destination = [System.Drawing.Bitmap]::new(
            $source.Width,
            $source.Height,
            [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        try {
            $destination.SetResolution(96, 96)
            $graphics = [System.Drawing.Graphics]::FromImage($destination)
            try {
                $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
                $graphics.Clear([System.Drawing.Color]::Transparent)

                $shape = [System.Drawing.Drawing2D.GraphicsPath]::new()
                try {
                    $diameter = $Radius * 2
                    $width = $source.Width
                    $height = $source.Height
                    $shape.AddArc(0, 0, $diameter, $diameter, 180, 90)
                    $shape.AddArc($width - $diameter, 0, $diameter, $diameter, 270, 90)
                    $shape.AddArc($width - $diameter, $height - $diameter, $diameter, $diameter, 0, 90)
                    $shape.AddArc(0, $height - $diameter, $diameter, $diameter, 90, 90)
                    $shape.CloseFigure()
                    $graphics.SetClip($shape)
                }
                finally {
                    $shape.Dispose()
                }

                $graphics.DrawImage(
                    $source,
                    [System.Drawing.Rectangle]::new(0, 0, $source.Width, $source.Height))
            }
            finally {
                $graphics.Dispose()
            }

            $destination.Save($OutputPath, [System.Drawing.Imaging.ImageFormat]::Png)
        }
        finally {
            $destination.Dispose()
        }
    }
    finally {
        $source.Dispose()
    }
}

function Resize-Image {
    param(
        [string]$InputPath,
        [string]$OutputPath,
        [int]$Width,
        [int]$Height
    )

    $source = [System.Drawing.Image]::FromFile($InputPath)
    try {
        $destination = [System.Drawing.Bitmap]::new($Width, $Height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        try {
            $destination.SetResolution(96, 96)
            $graphics = [System.Drawing.Graphics]::FromImage($destination)
            try {
                $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
                $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
                $graphics.DrawImage($source, 0, 0, $Width, $Height)
            }
            finally {
                $graphics.Dispose()
            }

            $destination.Save($OutputPath, [System.Drawing.Imaging.ImageFormat]::Png)
        }
        finally {
            $destination.Dispose()
        }
    }
    finally {
        $source.Dispose()
    }
}

function Invoke-ChromeScreenshot {
    param(
        [string]$Chrome,
        [string]$HtmlPath,
        [string]$OutputPath,
        [int]$Width,
        [int]$Height,
        [double]$Scale
    )

    $url = 'file:///' + ($HtmlPath -replace '\\', '/')
    $previousErrorAction = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        & $Chrome `
            '--headless=new' `
            '--disable-gpu' `
            '--hide-scrollbars' `
            "--force-device-scale-factor=$Scale" `
            "--window-size=$Width,$Height" `
            '--virtual-time-budget=3000' `
            "--screenshot=$OutputPath" `
            $url 2>&1 | Out-Null
    }
    finally {
        $ErrorActionPreference = $previousErrorAction
    }

    if ($LASTEXITCODE -ne 0 -or -not (Test-Path $OutputPath)) {
        throw "Chrome failed to render $HtmlPath (exit $LASTEXITCODE)."
    }
}

function Assert-ImageSize {
    param(
        [string]$Path,
        [int]$Width,
        [int]$Height,
        [switch]$Exact
    )

    $image = [System.Drawing.Image]::FromFile($Path)
    try {
        if ($Exact) {
            if ($image.Width -ne $Width -or $image.Height -ne $Height) {
                throw "$Path is $($image.Width)x$($image.Height); expected ${Width}x${Height}."
            }
            return
        }

        if ($image.Width -lt $Width -or $image.Height -lt $Height) {
            throw "$Path is $($image.Width)x$($image.Height); expected at least ${Width}x${Height}."
        }
    }
    finally {
        $image.Dispose()
    }
}

$appProcess = $null
try {
    if (-not $SkipBuild) {
        Push-Location $repoFull
        try {
            & dotnet build 'CLIHub.sln' --nologo -v:minimal
            if ($LASTEXITCODE -ne 0) {
                throw 'dotnet build failed.'
            }
        }
        finally {
            Pop-Location
        }
    }

    if (-not (Test-Path $exePath)) {
        throw "Debug executable not found: $exePath"
    }

    if (-not (Test-Path $defaultLogoPath)) {
        throw "Default project logo not found: $defaultLogoPath"
    }

    $appVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($exePath).ProductVersion
    if (-not $appVersion) {
        $appVersion = '0.0.0'
    }

    if (-not $workspaceExisted) {
        New-Item -ItemType Directory -Path $MockWorkspaceRoot -Force | Out-Null
    }

    $projects = @(
        [pscustomobject]@{ Directory = 'orbit-dashboard'; Name = 'Orbit Dashboard'; Letter = 'O'; Color = '#3B82F6'; Favorite = $true; MinutesAgo = 2 },
        [pscustomobject]@{ Directory = 'billing-service'; Name = 'Billing Service'; Letter = $null; Color = $null; Favorite = $false; MinutesAgo = 8 },
        [pscustomobject]@{ Directory = 'design-system'; Name = 'Design System'; Letter = 'D'; Color = '#8B5CF6'; Favorite = $false; MinutesAgo = 14 },
        [pscustomobject]@{ Directory = 'search-api'; Name = 'Search API'; Letter = $null; Color = $null; Favorite = $false; MinutesAgo = 21 },
        [pscustomobject]@{ Directory = 'mobile-app'; Name = 'Mobile App'; Letter = $null; Color = $null; Favorite = $false; MinutesAgo = 37 },
        [pscustomobject]@{ Directory = 'data-pipeline'; Name = 'Data Pipeline'; Letter = $null; Color = $null; Favorite = $false; MinutesAgo = 52 }
    )

    $utf8 = [System.Text.UTF8Encoding]::new($false)
    $now = [DateTime]::UtcNow
    $projectDocuments = @()
    foreach ($project in $projects) {
        $projectPath = Join-Path $MockWorkspaceRoot $project.Directory
        New-Item -ItemType Directory -Path $projectPath -Force | Out-Null
        New-Item -ItemType Directory -Path (Join-Path $projectPath '.git') -Force | Out-Null
        New-Item -ItemType Directory -Path (Join-Path $projectPath '.opencode') -Force | Out-Null
        [System.IO.File]::WriteAllText((Join-Path $projectPath '.opencode\config.json'), '{}', $utf8)

        $logoPath = Join-Path $projectPath 'logo.png'
        if ($project.Letter) {
            New-RoundedLetterLogo -Path $logoPath -Letter $project.Letter -Color $project.Color
        }
        else {
            Copy-Item $defaultLogoPath $logoPath -Force
        }

        $projectDocuments += [pscustomobject]@{
            id = [guid]::NewGuid().ToString('N')
            name = $project.Name
            path = $projectPath
            isFavorite = $project.Favorite
            lastUsed = $now.AddMinutes(-$project.MinutesAgo).ToString('o')
            logoPath = $logoPath
            source = 'discovered'
        }
    }

    $preferences = [pscustomobject]@{
        startWithWindows = $false
        hotkey = 'Ctrl+Shift+Space'
        terminalExecutable = 'wt.exe'
        defaultRuntime = 'ps'
        logLevel = 'Information'
        logRetentionDays = 7
        logFileSizeLimitMb = 0
        showOnlyProjectAgents = $false
        agentProbeTtlMinutes = $null
        agentProbeTimeoutSeconds = $null
        checkForUpdatesOnStartup = $false
        autoDownloadUpdates = $false
        notifyStableUpdates = $true
        notifyPrereleaseUpdates = $false
        showWindowOnStartup = $true
        startMinimized = $false
        pinLaunchWindow = $true
        popupScreenPosition = 'center'
        appTheme = 'dark'
        accentColor = '#4CC2FF'
        pathDisplayStyle = 'leftTrim'
        lastSeenReleaseNotesVersion = $appVersion
        watchedDirectories = @($MockWorkspaceRoot)
        watchedDirectorySettings = @(
            [pscustomobject]@{
                path = $MockWorkspaceRoot
                scanDepth = 2
                discoveryMode = 'both'
            }
        )
        scanDepth = 2
        ignoredPatterns = @(
            'bin', 'obj', 'build', 'out', '.vs', '.idea', '.vscode', 'packages', 'vendor',
            '__pycache__', '.venv', 'venv', 'artifacts'
        )
        discoveryMode = 'both'
        defaultAgentId = 'opencode'
    }

    $configuration = [pscustomobject]@{
        schemaVersion = 2
        projects = $projectDocuments
        preferences = $preferences
        currentProjectId = $projectDocuments[0].id
    }

    New-Item -ItemType Directory -Path (Split-Path $configPath -Parent) -Force | Out-Null
    [System.IO.File]::WriteAllText($configPath, ($configuration | ConvertTo-Json -Depth 10), $utf8)

    $renderedDirectory = Join-Path $CaptureDirectory 'rendered'
    if (Test-Path $renderedDirectory) {
        Remove-Item $renderedDirectory -Recurse -Force
    }

    New-Item -ItemType Directory -Path $renderedDirectory -Force | Out-Null

    $env:CLIHUB_CAPTURE_DIR = $renderedDirectory
    try {
        $appProcess = Start-Process -FilePath $exePath -PassThru
        if (-not $appProcess.WaitForExit(120000)) {
            $appProcess.Kill()
            throw 'The capture run did not finish within 120 seconds.'
        }

        if ($appProcess.ExitCode -ne 0) {
            throw "The capture run exited with code $($appProcess.ExitCode)."
        }
    }
    finally {
        Remove-Item Env:\CLIHUB_CAPTURE_DIR -ErrorAction SilentlyContinue
    }

    $captureError = Join-Path $renderedDirectory 'capture-error.txt'
    if (Test-Path $captureError) {
        throw "Capture failed:`n$(Get-Content $captureError -Raw)"
    }

    $captures = @(
        'launch-window.png',
        'settings-appearance.png',
        'settings-discovery.png',
        'settings-agents.png'
    )

    foreach ($capture in $captures) {
        $sourcePath = Join-Path $renderedDirectory $capture
        if (-not (Test-Path $sourcePath)) {
            throw "Missing capture: $sourcePath"
        }

        $destinationPath = Join-Path $OutputDirectory $capture
        Add-RoundedCorners -InputPath $sourcePath -OutputPath $destinationPath -Radius 12
    }

    Assert-ImageSize (Join-Path $OutputDirectory 'launch-window.png') -Width 800 -Height 600
    Assert-ImageSize (Join-Path $OutputDirectory 'settings-appearance.png') -Width 1200 -Height 900
    Assert-ImageSize (Join-Path $OutputDirectory 'settings-discovery.png') -Width 1200 -Height 900
    Assert-ImageSize (Join-Path $OutputDirectory 'settings-agents.png') -Width 1200 -Height 900

    if (-not $SkipWebImages) {
        $chromeCandidates = @(
            'C:\Program Files\Google\Chrome\Application\chrome.exe',
            'C:\Program Files (x86)\Google\Chrome\Application\chrome.exe',
            (Join-Path $env:LOCALAPPDATA 'Google\Chrome\Application\chrome.exe')
        )
        $chrome = $chromeCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
        if (-not $chrome) {
            throw 'Chrome was not found. Pass -SkipWebImages or install Chrome.'
        }

        $bannerPath = Join-Path $OutputDirectory 'banner.png'
        Invoke-ChromeScreenshot `
            -Chrome $chrome `
            -HtmlPath (Join-Path $OutputDirectory 'src\banner.html') `
            -OutputPath $bannerPath `
            -Width 1280 `
            -Height 360 `
            -Scale 1.5
        Assert-ImageSize $bannerPath -Width 1920 -Height 540 -Exact

        $workflowPath = Join-Path $OutputDirectory 'workflow.png'
        Invoke-ChromeScreenshot `
            -Chrome $chrome `
            -HtmlPath (Join-Path $OutputDirectory 'src\workflow.html') `
            -OutputPath $workflowPath `
            -Width 1200 `
            -Height 272 `
            -Scale 1.5
        Assert-ImageSize $workflowPath -Width 1800 -Height 408 -Exact

        $socialPreview2x = Join-Path $CaptureDirectory 'social-preview-2x.png'
        Invoke-ChromeScreenshot `
            -Chrome $chrome `
            -HtmlPath (Join-Path $OutputDirectory 'src\social-preview.html') `
            -OutputPath $socialPreview2x `
            -Width 1280 `
            -Height 640 `
            -Scale 2
        $socialPreviewPath = Join-Path $OutputDirectory 'social-preview.png'
        Resize-Image -InputPath $socialPreview2x -OutputPath $socialPreviewPath -Width 1280 -Height 640
        Assert-ImageSize $socialPreviewPath -Width 1280 -Height 640 -Exact
    }

    Write-Host "README images updated in $OutputDirectory"
    Write-Host "Raw captures and backups: $CaptureDirectory"
}
finally {
    if ($appProcess -and -not $appProcess.HasExited) {
        $appProcess.Kill()
        $appProcess.WaitForExit(5000) | Out-Null
    }

    Restore-File -Backup $backupConfig -Target $configPath -Existed $hadConfig
    Restore-File -Backup $backupLogoCache -Target $logoCachePath -Existed $hadLogoCache

    if ($workspaceCreatedHere -and -not $KeepMockWorkspace) {
        Remove-Item $MockWorkspaceRoot -Recurse -Force -ErrorAction SilentlyContinue
    }

    Remove-Item Env:\CLIHUB_CAPTURE_DIR -ErrorAction SilentlyContinue
}
