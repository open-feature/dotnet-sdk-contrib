#!/usr/bin/env bash

# This script downloads the wasm file from the go-feature-flag repository and adds it to the build.

wasm_version="v1.45.0" # {{wasm_version}}

# Set the repository owner and name
repo_owner="thomaspoignant"
repo_name="go-feature-flag"
file_suffix=".wasi"
target_dir="./WasmModules"

# Function to find the download URL
find_download_url() {
  local release_tag=$1
  local file_suffix=$2

  # Get the assets for the specific release
  assets=$(curl -s "https://api.github.com/repos/$repo_owner/$repo_name/releases/tags/$wasm_version" | jq -r '.assets')

  if [ -z "$assets" ]; then
    echo "Error: No assets found for release $wasm_version"
    return 1
  fi

  # Find the asset that matches the file prefix
  download_url=$(echo "$assets" | jq -r ".[] | select(.name | endswith(\"$file_suffix\")) | .browser_download_url")

  if [ -z "$download_url" ]; then
    echo "Error: No asset found with prefix '$file_suffix' in release $wasm_version"
    return 1
  fi
  echo "$download_url"
}

# Function to download the file
download_file() {
  local url=$1
  local target_dir=$2

  if [ -z "$url" ]; then
    echo "Error: Download URL is empty."
    return 1
  fi

  if [ -z "$target_dir" ]; then
    echo "Error: Target directory is empty."
    return 1
  fi

  # Extract the filename from the URL
  local filename=$(basename "$url")

  # Check if the directory exists
  if [ ! -d "$target_dir" ]; then
    mkdir -p "$target_dir" # Create the directory if it doesn't exist
  fi

  # Use curl to download the file with progress
  echo "Downloading $filename to $target_dir..."
  curl -L -o "$target_dir/gofeatureflag-evaluation.wasi" "$url"
  if [ $? -ne 0 ]; then
    echo "Error: Download failed."
    return 1
  fi
  echo "Download successful!"
}

# Main script logic
download_url=$(find_download_url "$latest_release" "$file_suffix")
if [ $? -ne 0 ]; then
  echo "Error: Failed to find the download URL for release $latest_release."
  exit 1
fi

download_file "$download_url" "$target_dir"
if [ $? -ne 0 ]; then
  echo "Error: Failed to download the file. $download_url"
  exit 1
fi

sleep 10s

echo "Done."
