# Builds a release-signed Treelore.apk into Android\release.
# Asks for the Treelore keystore password (hidden input). The password lives only in this process
# for the length of the build and is never written to disk.
$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

$keystore = Join-Path $env:USERPROFILE "Treelore signing\treelore.keystore"
if (-not (Test-Path $keystore)) { throw "No keystore at $keystore. Create it first (see README, Releasing)." }

# Find the Android SDK and JDK. They may live in the normal place, or inside a packaged app's private AppData
# (Windows redirects %LOCALAPPDATA% for packaged apps such as the Claude desktop app).
function Find-Local([string]$relative) {
	$direct = Join-Path $env:LOCALAPPDATA $relative
	if (Test-Path $direct) { return $direct }
	Get-ChildItem (Join-Path $env:LOCALAPPDATA "Packages") -Directory -ErrorAction SilentlyContinue |
		ForEach-Object { Join-Path $_.FullName "LocalCache\Local\$relative" } |
		Where-Object { Test-Path $_ } | Select-Object -First 1
}
$sdk = Find-Local "Android\Sdk"
if (-not $sdk) { throw "Android SDK not found." }
$jdk = Find-Local "Android\jdk"
$buildArgs = @("-p:AndroidSdkDirectory=$sdk")
if ($jdk) { $buildArgs += "-p:JavaSdkDirectory=$jdk" }
"Android SDK: $sdk"

$secure = Read-Host "Treelore keystore password" -AsSecureString
$env:TREELORE_KEYSTORE_PASS = [Runtime.InteropServices.Marshal]::PtrToStringBSTR([Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure))
try {
	# Clean build: incremental builds after manifest or signing changes can produce an APK that crashes on launch
	Remove-Item -Recurse -Force bin, obj -ErrorAction SilentlyContinue
	dotnet build -c Release -nologo -v q @buildArgs
	if ($LASTEXITCODE -ne 0) { throw "Build failed." }
}
finally {
	Remove-Item Env:\TREELORE_KEYSTORE_PASS -ErrorAction SilentlyContinue
}

$apk = Get-ChildItem bin\Release -Recurse -Filter "*-Signed.apk" | Select-Object -First 1
New-Item -ItemType Directory -Force release | Out-Null
Copy-Item $apk.FullName release\Treelore.apk -Force

# Confirm it's signed with the Treelore key, not the debug key
$tools = Join-Path $sdk "build-tools"
$apksigner = Join-Path $tools ((Get-ChildItem $tools | Sort-Object Name | Select-Object -Last 1).Name + "\apksigner.bat")
$certs = & $apksigner verify --print-certs release\Treelore.apk
if ($LASTEXITCODE -ne 0) { throw "Signature check failed." }
if ($certs -match "Android Debug") { throw "Signed with the DEBUG key. Check the password and keystore." }
$certs | Select-String "Signer #1 certificate DN|SHA-256"
"Built release\Treelore.apk ({0:N1} MB), signed with the Treelore key." -f ((Get-Item release\Treelore.apk).Length / 1MB)
