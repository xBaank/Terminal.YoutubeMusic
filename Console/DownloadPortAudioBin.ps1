param (
    [string]$platform,
    [string]$outputPath
)

$ErrorActionPreference = "Stop"

# Normalize platform identifier
$platform = $platform.ToLower().Replace("win-", "windows-")
$platform = $platform.ToLower().Replace("osx-x64", "osx-uni")
$platform = $platform.ToLower().Replace("osx-arm64", "osx-uni")

# Download the archive
Write-Host "Downloading Portaudio for $platform..."

# Check if already exists
if (Test-Path $outputPath) {
    Write-Host "Skipped downloading Portaudio, file already exists."
    exit
}

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$http = New-Object System.Net.WebClient
try {
    $http.DownloadFile("https://github.com/xBaank/PortAudioBin/releases/download/19.7/portaudio-$platform.zip", "$outputPath.zip")
} finally {
    $http.Dispose()
}

try {
    # Extract PortAudio
    Add-Type -Assembly System.IO.Compression.FileSystem
    $zip = [IO.Compression.ZipFile]::OpenRead("$outputPath.zip")
    try {
        $fileName = If ($platform.Contains("windows-")) { "bin/portaudio.dll" } ElseIf ($platform.Contains("linux-")) { "lib/libportaudio.so" } Else { "lib/libportaudio.dylib" } 
        [IO.Compression.ZipFileExtensions]::ExtractToFile($zip.GetEntry($fileName), $outputPath)
    } finally {
        $zip.Dispose()
    }

    Write-Host "Done downloading $fileName"
} finally {
    # Clean up
    Remove-Item "$outputPath.zip" -Force
}