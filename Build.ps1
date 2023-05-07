$root = Get-Location
$binPath = Join-Path $root 'bin'
If (Test-Path $binPath) {
    Remove-Item -Path $binPath -Recurse -Force
}
New-Item $binPath -ItemType Directory
$srcPath = Join-Path $root src
$corePath = Join-Path $srcPath 'Pomelo.Storage.WebDAV'
Set-Location $corePath
$version = 'r' + [System.DateTime]::UtcNow.ToString("yyyyMMddHHmmss")
dotnet pack --version-suffix $version -c Release
$nupkgPath = 'bin/Release/Pomelo.Storage.WebDAV.1.0.0-' + $version + '.nupkg'
Copy-Item $nupkgPath $binPath
Set-Location $root