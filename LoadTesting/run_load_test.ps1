# Script thực thi Load Testing tự động cho AsusLaptop System
Param(
    [string]$TargetUrl = "http://localhost:5000"
)

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "   ASUSLAPTOP SYSTEM - AUTOMATED LOAD TESTING RUNNER (k6)" -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "Target Endpoint: $TargetUrl" -ForegroundColor Yellow

$k6Path = Get-Command k6 -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Path

if ($k6Path) {
    Write-Host "[+] Executing k6 Load Test Suite..." -ForegroundColor Green
    & k6 run --env TARGET_URL=$TargetUrl "$PSScriptRoot\load_test.js"
} else {
    Write-Host "[!] k6 command not found on machine PATH." -ForegroundColor Yellow
    Write-Host "[+] Running HTTP Benchmark simulation script via Invoke-WebRequest..." -ForegroundColor Green
    
    $successCount = 0
    $failCount = 0
    $totalTimeMs = 0
    $requests = 50

    for ($i = 1; $i -le $requests; $i++) {
        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        try {
            $res = Invoke-WebRequest -Uri "$TargetUrl/" -UseBasicParsing -TimeoutSec 5
            $sw.Stop()
            if ($res.StatusCode -eq 200) {
                $successCount++
                $totalTimeMs += $sw.ElapsedMilliseconds
            } else {
                $failCount++
            }
        } catch {
            $sw.Stop()
            $failCount++
        }
    }

    $avgTime = [Math]::Round($totalTimeMs / [Math]::Max(1, $successCount), 2)
    Write-Host "-----------------------------------------------------------------" -ForegroundColor Gray
    Write-Host "SIMULATED BENCHMARK RESULTS SUMMARY:" -ForegroundColor Green
    Write-Host "Total Requests Sent: $requests" -ForegroundColor White
    Write-Host "Successful Responses: $successCount (200 OK)" -ForegroundColor Green
    Write-Host "Failed Responses:     $failCount" -ForegroundColor Red
    Write-Host "Average Response Time: $avgTime ms" -ForegroundColor Cyan
    Write-Host "-----------------------------------------------------------------" -ForegroundColor Gray
}
