$ErrorActionPreference = "SilentlyContinue"

$serviceProcessNames = @(
    "Matrix.Identity.Api",
    "Matrix.Economy.Api",
    "Matrix.Population.Api",
    "Matrix.Resources.Api",
    "Matrix.SimulationCore.Api",
    "Matrix.SimulationSystems.Api",
    "Matrix.ApiGateway"
)

$servicePorts = @(
    5227, 7256,
    5286, 7193,
    5017, 7297,
    5319, 7319,
    5138, 7207,
    5318, 7318,
    5204, 7155,
    5173
)

$pidsToStop = [System.Collections.Generic.HashSet[int]]::new()

foreach ($name in $serviceProcessNames) {
    foreach ($process in Get-Process -Name $name) {
        [void]$pidsToStop.Add($process.Id)
    }
}

foreach ($connection in Get-NetTCPConnection -State Listen) {
    if ($servicePorts -contains $connection.LocalPort -and $connection.OwningProcess) {
        [void]$pidsToStop.Add($connection.OwningProcess)
    }
}

foreach ($pid in $pidsToStop) {
    try {
        Stop-Process -Id $pid -Force -ErrorAction Stop
    }
    catch {
        continue
    }
}

# Give Windows a brief moment to release file handles and ports
# before the rest of the MultiLaunch session starts.
Start-Sleep -Milliseconds 750
