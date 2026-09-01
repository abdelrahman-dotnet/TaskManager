param(
	[string]$ProjectPath = "."
)

Push-Location $ProjectPath

# Initialize user-secrets for the project (no-op if already initialized)
dotnet user-secrets init

# Generate 32 cryptographically-strong random bytes and convert to base64
$bytes = New-Object byte[] 32
[System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
$secret = [Convert]::ToBase64String($bytes)

# Store into user-secrets under the JWT:Key configuration path
dotnet user-secrets set "JWT:Key" $secret

Write-Host "JWT:Key set to a 32-byte base64 secret."

Pop-Location
