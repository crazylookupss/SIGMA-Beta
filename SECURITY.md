# Security Policy

## Reporting a Vulnerability

If you discover a security vulnerability in SIGMA API, please report it privately.

**Do not** report security vulnerabilities through public GitHub issues.

Please send an email to **[YOUR_EMAIL_HERE]** with details of the vulnerability. We will work with you to resolve the issue promptly. Alternatively, you can open a private GitHub Security Advisory in this repository.

## Response Timeline

- **Acknowledgment** within 48 hours
- **Initial assessment** within 5 business days
- **Fix timeline** communicated based on severity

## Supported Versions

| Version | Supported |
|---------|-----------|
| Latest  | ✅        |

## Best Practices

- Use environment variables or Azure Key Vault for secrets (never commit them)
- Rotate client secrets regularly (every 6 months)
- Enable Managed Identity in Azure App Service instead of client secrets where possible
- Keep .NET SDK and NuGet packages up to date
- Review Entra ID app registration permissions regularly
- Use `SensitiveSelfServicePolicy` for write/delete operations
