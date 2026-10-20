[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$testRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../obj/package-verifier-tests'))
$workspace = Join-Path $testRoot ([Guid]::NewGuid().ToString('N'))
$publish = Join-Path $workspace 'publish'
$package = Join-Path $workspace 'apifunctions.zip'
$verifier = Join-Path $PSScriptRoot 'Test-PublishPackage.ps1'
$cases = 0

function New-Metadata {
    @(
        @{ name = 'DailyRefresh'; language = 'dotnet-isolated'; bindings = @(@{ type = 'timerTrigger'; schedule = '0 1 6 * * *' }) }
        @{ name = 'ScheduledTasks'; language = 'dotnet-isolated'; bindings = @(@{ type = 'timerTrigger'; schedule = '0 10 6 * * *' }) }
        @{ name = 'ScheduledTasksV2'; language = 'dotnet-isolated'; bindings = @(@{ type = 'timerTrigger'; schedule = '0 0 * * * *' }) }
    )
}

function Set-Metadata([object[]]$Functions) {
    [System.IO.File]::WriteAllText((Join-Path $publish 'functions.metadata'), (ConvertTo-Json -InputObject $Functions -Depth 10))
}

function Assert-Rejected([string]$Name, [scriptblock]$Check, [string]$ExpectedMessage) {
    $rejected = $false
    try { & $Check }
    catch {
        if ($_.Exception.Message -notlike "*$ExpectedMessage*") { throw "$Name failed unexpectedly: $($_.Exception.Message)" }
        $rejected = $true
    }
    if (-not $rejected) { throw "$Name was accepted unexpectedly." }
    $script:cases++
}

try {
    [System.IO.Directory]::CreateDirectory((Join-Path $publish '.azurefunctions')) | Out-Null
    foreach ($name in @('host.json', 'ApiFunctions.dll', '.azurefunctions/extensions.json')) {
        [System.IO.File]::WriteAllText((Join-Path $publish $name), '{}')
    }
    [System.IO.File]::WriteAllText((Join-Path $publish 'worker.config.json'), '{"description":{"language":"dotnet-isolated"}}')
    [System.IO.File]::WriteAllText((Join-Path $publish 'ApiFunctions.runtimeconfig.json'), '{"runtimeOptions":{"tfm":"net10.0"}}')
    Set-Metadata (New-Metadata)
    & $verifier -PublishDirectory $publish
    $cases++
    [System.IO.Compression.ZipFile]::CreateFromDirectory($publish, $package)
    & $verifier -PublishDirectory $publish -PackagePath $package
    $cases++

    Set-Metadata @((New-Metadata) | Where-Object name -ne 'ScheduledTasksV2')
    Assert-Rejected 'Missing hourly function' { & $verifier -PublishDirectory $publish } 'Unexpected function count'
    foreach ($name in @('DailyRefresh', 'ScheduledTasks', 'ScheduledTasksV2')) {
        $functions = New-Metadata
        ($functions | Where-Object name -eq $name).bindings[0].schedule = '0 */2 * * * *'
        Set-Metadata $functions
        Assert-Rejected "Changed schedule $name" { & $verifier -PublishDirectory $publish } "Unexpected worker model or schedule for $name"
    }
    $functions = New-Metadata
    $functions[2].name = 'ScheduledTasks'
    Set-Metadata $functions
    Assert-Rejected 'Duplicate function name' { & $verifier -PublishDirectory $publish } 'Expected exactly one function named'
    Set-Metadata @((New-Metadata) + @{ name = 'Unexpected' })
    Assert-Rejected 'Unexpected extra function' { & $verifier -PublishDirectory $publish } 'Unexpected function count'
    $functions = New-Metadata
    $functions[2].language = 'dotnet'
    Set-Metadata $functions
    Assert-Rejected 'Wrong hourly worker' { & $verifier -PublishDirectory $publish } 'Unexpected worker model or schedule'

    # A valid publish folder must not hide invalid metadata in a different ZIP.
    Set-Metadata @((New-Metadata) | Where-Object name -ne 'ScheduledTasksV2')
    $badPackage = Join-Path $workspace 'bad.zip'
    [System.IO.Compression.ZipFile]::CreateFromDirectory($publish, $badPackage)
    Set-Metadata (New-Metadata)
    Assert-Rejected 'ZIP missing hourly function' { & $verifier -PublishDirectory $publish -PackagePath $badPackage } 'Unexpected function count'
    [System.IO.File]::WriteAllText((Join-Path $publish 'local.settings.json'), '{}')
    Assert-Rejected 'Local secrets in publish output' { & $verifier -PublishDirectory $publish } 'Local settings or publish profiles'
    Write-Host "Package verifier regression checks passed: $cases."
}
finally {
    $resolved = [System.IO.Path]::GetFullPath($workspace)
    if (-not $resolved.StartsWith($testRoot + [System.IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Refusing cleanup outside the generated verifier test directory.'
    }
    if (Test-Path -LiteralPath $resolved) { Remove-Item -LiteralPath $resolved -Recurse -Force }
}
