#!/usr/bin/env pwsh
$ErrorActionPreference = "Stop"

$bmpResolutions = @(16, 20, 24, 30, 32, 36, 40, 48, 60, 64, 72, 80, 96)
$pngResolutions = @(256)
$optipng = 'D:\Tools\optipng-0.7.7-win32\optipng.exe'

function Write-Image {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory)]
        [string]$source,
        [Parameter(Mandatory)]
        [string]$resolution,
        [Parameter(Mandatory)]
        [string]$extension
    )
    $filename = "icon${resolution}.${extension}"
    Write-Host "Generating icons for $filename"
    magick convert -background none -size ${resolution}x${resolution} $source $filename | Out-null
    if ($extension -eq 'png') {
        & $optipng -o7 $filename
    }
}

function Generate-Icon {
   
}

foreach ($resolution in $bmpResolutions) {
    Write-Image -source icon.svg -resolution $resolution -extension 'bmp'
}

foreach ($resolution in $pngResolutions) {
    Write-Image -source icon.svg -resolution $resolution -extension 'png'
}

Generate-Icon