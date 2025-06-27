# This script downloads the wasm file from the go-feature-flag repository and adds it to the build.

# Define variables
$wasm_version = "v1.45.0" # {{wasm_version}}
$repo_owner = "thomaspoignant"
$repo_name = "go-feature-flag"
$file_suffix = ".wasi"
$target_dir = "./WasmModules"
$fileName = "gofeatureflag-evaluation.wasi" # The desired name for the downloaded file

# Function to find the download URL
function Find-DownloadUrl {
    param (
        [string]$ReleaseTag,
        [string]$FileSuffix
    )

    Write-Host "Attempting to find download URL for release $ReleaseTag with suffix '$FileSuffix'..."

    try {
        # Construct the API URL
        $apiUrl = "https://api.github.com/repos/$repo_owner/$repo_name/releases/tags/$ReleaseTag"

        # Get the release assets using Invoke-RestMethod
        # Invoke-RestMethod automatically parses JSON responses
        $releaseInfo = Invoke-RestMethod -Uri $apiUrl

        if (-not $releaseInfo.assets) {
            Write-Error "Error: No assets found for release $($ReleaseTag)."
            return $null
        }

        # Find the asset that matches the file suffix
        $downloadUrl = $releaseInfo.assets | Where-Object { $_.name.EndsWith($FileSuffix) } | Select-Object -ExpandProperty browser_download_url

        if (-not $downloadUrl) {
            Write-Error "Error: No asset found with suffix '$FileSuffix' in release $($ReleaseTag)."
            return $null
        }

        Write-Host "Found download URL: $($downloadUrl)"
        return $downloadUrl
    }
    catch {
        Write-Error "An error occurred while finding the download URL: $($_.Exception.Message)"
        return $null
    }
}

# Function to download the file
function Download-File {
    param (
        [string]$Url,
        [string]$TargetDirectory,
        [string]$OutputFileName
    )

    if ([string]::IsNullOrEmpty($Url)) {
        Write-Error "Error: Download URL is empty."
        return $false
    }

    if ([string]::IsNullOrEmpty($TargetDirectory)) {
        Write-Error "Error: Target directory is empty."
        return $false
    }

    if ([string]::IsNullOrEmpty($OutputFileName)) {
        Write-Error "Error: Output file name is empty."
        return $false
    }

    # Ensure the target directory exists
    if (-not (Test-Path -Path $TargetDirectory -PathType Container)) {
        Write-Host "Creating target directory: $TargetDirectory..."
        try {
            New-Item -ItemType Directory -Path $TargetDirectory | Out-Null
        }
        catch {
            Write-Error "Error: Failed to create directory $($TargetDirectory). $($_.Exception.Message)"
            return $false
        }
    }

    $outputPath = Join-Path -Path $TargetDirectory -ChildPath $OutputFileName
    Write-Host "Downloading $($OutputFileName) to $($outputPath)..."

    try {
        # Use Invoke-WebRequest to download the file
        # -OutFile specifies the path to save the downloaded content
        Invoke-WebRequest -Uri $Url -OutFile $outputPath -UseBasicParsing

        # Check if the file was downloaded successfully
        if (Test-Path -Path $outputPath) {
            $fileSize = (Get-Item $outputPath).Length
            if ($fileSize -gt 0) {
                Write-Host "Download successful! File size: $fileSize bytes"
                return $true
            } else {
                Write-Error "Error: Downloaded file is empty at $outputPath."
                return $false
            }
        } else {
            Write-Error "Error: File not found at $outputPath after download."
            return $false
        }
    }
    catch {
        Write-Error "An error occurred during download: $($_.Exception.Message)"
        return $false
    }
}

# Main script logic

# Find the download URL
$download_url = Find-DownloadUrl -ReleaseTag $wasm_version -FileSuffix $file_suffix
if (-not $download_url) {
    Write-Error "Error: Failed to find the download URL for release $($wasm_version)."
    exit 1
}

# Download the file
$download_success = Download-File -Url $download_url -TargetDirectory $target_dir -OutputFileName $fileName
if (-not $download_success) {
    Write-Error "Error: Failed to download the file from $($download_url)."
    exit 1
}

Write-Host "Done."
