param([Parameter(Mandatory)][string]$Executable)
$ErrorActionPreference = 'Stop'
$Executable = (Resolve-Path $Executable).Path

function Invoke-Query([string[]]$Arguments) {
    $start = [System.Diagnostics.ProcessStartInfo]::new($Executable)
    $start.UseShellExecute = $false
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    foreach ($argument in $Arguments) { $start.ArgumentList.Add($argument) }
    $process = [System.Diagnostics.Process]::Start($start)
    $stdout = $process.StandardOutput.ReadToEndAsync()
    $stderr = $process.StandardError.ReadToEndAsync()
    if (-not $process.WaitForExit(30000)) {
        $process.Kill($true)
        throw 'WslQuery timed out.'
    }
    $result = @{ Code = $process.ExitCode; Out = $stdout.GetAwaiter().GetResult(); Err = $stderr.GetAwaiter().GetResult() }
    $process.Dispose()
    return $result
}

$help = Invoke-Query -Arguments @('--help')
if ($help.Code -ne 0 -or $help.Out -notmatch '^Usage:' -or $help.Err) { throw 'Help smoke test failed.' }
$invalid = Invoke-Query -Arguments @('--unknown')
if ($invalid.Code -ne 2 -or $invalid.Out -or -not $invalid.Err) { throw 'Argument smoke test failed.' }

$query = Invoke-Query -Arguments @('--pretty')
if ($query.Code -eq 0) {
    if ($query.Err -or -not $query.Out.TrimStart().StartsWith('[')) { throw 'Invalid successful query output.' }
    $null = ConvertFrom-Json -InputObject $query.Out
    Write-Output 'Native AOT query succeeded. The runner may have no registered distributions.'
} elseif ($query.Code -eq 1 -and $query.Err.StartsWith('WslQuery:')) {
    if ($query.Out) { $null = ConvertFrom-Json -InputObject $query.Out }
    Write-Output "Native AOT returned a controlled WSL query failure: $($query.Err.Trim())"
} else {
    throw "Native AOT query failed unexpectedly ($($query.Code)): $($query.Err)"
}
Write-Output 'Native AOT help, argument handling, and query/error smoke tests passed.'
