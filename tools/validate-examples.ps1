[CmdletBinding()]
param(
    [string] $InputPath = (Join-Path (Split-Path $PSScriptRoot -Parent) 'examples'),
    [switch] $CanonicalOnly
)

$ErrorActionPreference = 'Stop'

$supportedActions = @(
    'log', 'wait', 'screenshot', 'clear', 'type', 'click', 'focus', 'select',
    'setvalue', 'selecttab', 'selectrow', 'selectcell', 'expand', 'collapse',
    'getvalue', 'getselection', 'getstate', 'validate'
)
$actionsWithoutElement = @('log', 'wait', 'screenshot')
$legacyProperties = @(
    'targetProcess', 'selector', 'text', 'expectedValue', 'duration',
    'waitAfter', 'message', 'timeout', 'continueOnError', 'delayBetweenActions'
)

if (Test-Path -LiteralPath $InputPath -PathType Leaf) {
    $files = @(Get-Item -LiteralPath $InputPath)
}
elseif (Test-Path -LiteralPath $InputPath -PathType Container) {
    $files = @(Get-ChildItem -LiteralPath $InputPath -Filter '*.json' -File | Sort-Object Name)
}
else {
    throw "Input path does not exist: $InputPath"
}

if ($files.Count -eq 0) {
    throw "No JSON examples found under: $InputPath"
}

$failures = [System.Collections.Generic.List[string]]::new()
foreach ($file in $files) {
    try {
        $script = Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json
        if ([string]::IsNullOrWhiteSpace([string] $script.name)) {
            throw 'name is required'
        }
        if ($null -eq $script.target -and [string]::IsNullOrWhiteSpace([string] $script.targetProcess)) {
            throw 'target or legacy targetProcess is required'
        }
        if ($null -eq $script.steps) {
            throw 'steps must be an array'
        }

        $topLevel = @($script.PSObject.Properties.Name)
        foreach ($legacy in $legacyProperties) {
            if ($CanonicalOnly -and $topLevel -contains $legacy) {
                throw "legacy property '$legacy' is not allowed in canonical mode"
            }
        }

        $index = 0
        foreach ($step in @($script.steps)) {
            $index++
            $action = ([string] $step.action).ToLowerInvariant()
            if ([string]::IsNullOrWhiteSpace($action)) {
                throw "step $index requires action"
            }
            if ($supportedActions -notcontains $action) {
                throw "step $index uses unsupported action '$($step.action)'"
            }
            $hasElement = -not [string]::IsNullOrWhiteSpace([string] $step.element) -or
                -not [string]::IsNullOrWhiteSpace([string] $step.selector)
            if ($actionsWithoutElement -notcontains $action -and -not $hasElement) {
                throw "step $index action '$action' requires element or selector"
            }
            if ($null -ne $step.delayMs -and [int] $step.delayMs -lt 0) {
                throw "step $index delayMs must not be negative"
            }
            if ($CanonicalOnly) {
                foreach ($legacy in $legacyProperties) {
                    if (@($step.PSObject.Properties.Name) -contains $legacy) {
                        throw "step $index uses legacy property '$legacy'"
                    }
                }
            }
        }

        Write-Output ("PASS {0} ({1} steps)" -f $file.Name, @($script.steps).Count)
    }
    catch {
        $failures.Add(("FAIL {0}: {1}" -f $file.Name, $_.Exception.Message))
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Output ("Validated {0} example file(s)." -f $files.Count)
