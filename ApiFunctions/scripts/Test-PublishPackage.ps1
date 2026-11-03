[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$PublishDirectory,
    [string]$PackagePath
)

$ErrorActionPreference = 'Stop'
$requiredFiles = @('host.json', 'worker.config.json', 'functions.metadata',
    'ApiFunctions.dll', 'ApiFunctions.runtimeconfig.json')

foreach ($name in $requiredFiles) {
    if (-not (Test-Path -LiteralPath (Join-Path $PublishDirectory $name) -PathType Leaf)) {
        throw "Missing published file: $name"
    }
}

if (-not (Test-Path -LiteralPath (Join-Path $PublishDirectory '.azurefunctions') -PathType Container)) {
    throw 'The host extension directory is missing.'
}

$worker = Get-Content -LiteralPath (Join-Path $PublishDirectory 'worker.config.json') -Raw | ConvertFrom-Json
if ($worker.description.language -ne 'dotnet-isolated') {
    throw 'The published worker is not dotnet-isolated.'
}

$runtime = Get-Content -LiteralPath (Join-Path $PublishDirectory 'ApiFunctions.runtimeconfig.json') -Raw | ConvertFrom-Json
if ($runtime.runtimeOptions.tfm -ne 'net10.0') {
    throw 'The published worker does not target .NET 10.'
}

function Assert-FunctionMetadata([object[]]$Functions) {
    $schedules = @{
        DailyRefresh = '0 1 6 * * *'
        ScheduledTasks = '0 10 6 * * *'
        ScheduledTasksV2 = '0 0 * * * *'
    }
    if ($Functions.Count -ne $schedules.Count) { throw 'Unexpected function count.' }
    foreach ($name in $schedules.Keys) {
        $matches = @($Functions | Where-Object name -eq $name)
        if ($matches.Count -ne 1) { throw "Expected exactly one function named $name." }
        $timer = @($matches[0].bindings | Where-Object type -eq 'timerTrigger')
        if ($matches[0].language -ne 'dotnet-isolated' -or $timer.Count -ne 1 -or $timer[0].schedule -ne $schedules[$name]) {
            throw "Unexpected worker model or schedule for $name."
        }
    }
}
Assert-FunctionMetadata @(Get-Content -LiteralPath (Join-Path $PublishDirectory 'functions.metadata') -Raw | ConvertFrom-Json)

$localFiles = @(Get-ChildItem -LiteralPath $PublishDirectory -Recurse -Force -File |
    Where-Object { $_.Name -eq 'local.settings.json' -or $_.Name -like '*.pubxml' -or $_.Name -like '*.user' })
if ($localFiles.Count -gt 0) { throw 'Local settings or publish profiles are present in the publish output.' }

if ($PackagePath) {
    $archive = [System.IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $PackagePath).Path)
    try {
        foreach ($name in $requiredFiles) {
            if (-not $archive.GetEntry($name)) { throw "Missing file at ZIP root: $name" }
        }
        $reader = [System.IO.StreamReader]::new($archive.GetEntry('functions.metadata').Open())
        try { Assert-FunctionMetadata @($reader.ReadToEnd() | ConvertFrom-Json) }
        finally { $reader.Dispose() }
        $extensions = @($archive.Entries | Where-Object { $_.FullName.StartsWith('.azurefunctions/') })
        if ($extensions.Count -eq 0) { throw 'The ZIP omits the host extension directory.' }
        $localEntries = @($archive.Entries | Where-Object {
            $_.Name -eq 'local.settings.json' -or $_.Name -like '*.pubxml' -or $_.Name -like '*.user'
        })
        if ($localEntries.Count -gt 0) { throw 'The ZIP includes local configuration.' }
    }
    finally { $archive.Dispose() }
}

Write-Host 'Verified .NET 10 isolated worker, all three timer schedules, host extensions, and package contents.'
