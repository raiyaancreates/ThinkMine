$fonts = @(
    "inter", "space-grotesk", "quicksand", 
    "playfair-display", "merriweather", "cormorant-garamond", 
    "space-mono", "fira-code", "courier-prime", 
    "oswald", "syne"
)

$dest = "$env:TEMP\ThinkMineFonts"
if (Test-Path $dest) { Remove-Item $dest -Recurse -Force }
New-Item -ItemType Directory -Force -Path $dest | Out-Null

Write-Host "Requesting Admin privileges to install fonts..."
if (!([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Start-Process powershell.exe "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`"" -Verb RunAs
    exit
}

$webClient = New-Object System.Net.WebClient
$webClient.Headers.Add("User-Agent", "Mozilla/5.0")

$shell = New-Object -ComObject Shell.Application
$windowsFonts = $shell.Namespace(0x14)

foreach ($fontId in $fonts) {
    try {
        Write-Host "Processing $fontId..."
        $apiUrl = "https://gwfh.mranftl.com/api/fonts/$fontId"
        
        try {
            $json = $webClient.DownloadString($apiUrl)
        }
        catch {
            Write-Host "  -> API request failed (404?). Skipping."
            continue
        }
        
        $data = $json | ConvertFrom-Json
        
        # Try to find specific variants to ensure we get what we want
        # We want Regular, but also maybe a Bold or Italic if available? 
        # For now, let's just get the main "regular" or "400" one to keep it simple and working.
        # Windows usually handles "Bold" simulation if the specific file isn't there, 
        # but installing the family is better.
        # Let's install ALL variants found in the JSON to support Bold/Italic properly!
        
        foreach ($variant in $data.variants) {
            $ttfUrl = $variant.ttf
            if ($null -ne $ttfUrl) {
                $fileName = "$fontId-$($variant.id).ttf"
                $filePath = "$dest\$fileName"
                
                Write-Host "  -> Downloading $fileName..."
                $webClient.DownloadFile($ttfUrl, $filePath)
                
                Write-Host "  -> Installing..."
                $windowsFonts.CopyHere($filePath, 0x10)
            }
        }
    }
    catch {
        Write-Host "  -> Error: $_"
    }
}

Write-Host "`nDone! Please restart ThinkMine."
Read-Host "Press Enter to exit"
