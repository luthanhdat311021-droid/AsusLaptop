Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass

$WEIGHTS = ".\weights"
if (!(Test-Path $WEIGHTS)) { New-Item -ItemType Directory -Path $WEIGHTS | Out-Null }

Write-Host "Dang tai face-api.min.js..." -ForegroundColor Cyan
Invoke-WebRequest "https://cdn.jsdelivr.net/npm/face-api.js@0.22.2/dist/face-api.min.js" -OutFile "face-api.min.js"
Write-Host "  OK: face-api.min.js" -ForegroundColor Green

$files = @(
    "tiny_face_detector_model-weights_manifest.json",
    "tiny_face_detector_model-shard1",
    "face_landmark_68_tiny_model-weights_manifest.json",
    "face_landmark_68_tiny_model-shard1",
    "face_recognition_model-weights_manifest.json",
    "face_recognition_model-shard1",
    "face_recognition_model-shard2"
)

foreach ($f in $files) {
    Write-Host "  Downloading $f..." -ForegroundColor Cyan
    $url = "https://github.com/justadudewhohacks/face-api.js/raw/master/weights/$f"
    Invoke-WebRequest $url -OutFile "$WEIGHTS\$f"
    Write-Host "  OK: $f" -ForegroundColor Green
}

Write-Host "Hoan thanh! Tat ca models da duoc tai ve." -ForegroundColor Green
