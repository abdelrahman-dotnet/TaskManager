Usage
-----

These scripts initialize dotnet user-secrets for the TaskManager.API project and store a cryptographically-strong 32-byte JWT key under the configuration path `JWT:Key`.

PowerShell (Windows / PowerShell Core):

	pwsh TaskManager.API/scripts/set-user-secrets.ps1

Bash (Linux / macOS):

	bash TaskManager.API/scripts/set-user-secrets.sh

Notes
-----
- Run from the repository root or pass the project folder as the first argument.
- Do NOT commit secrets to source control.
- For production, provide signing material via environment variables (e.g., `JWT__Key`) or a secure secret store such as Azure Key Vault.
