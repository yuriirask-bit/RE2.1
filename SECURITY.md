# Security Policy

## Supported Versions

The following versions of RE2 are currently supported with security updates.

| Version | Supported          |
| ------- | ------------------ |
| 1.0.x   | :white_check_mark: |

RE2 targets **.NET 8 LTS** (EOL November 2026). Security patches will be applied promptly when upstream .NET or dependency vulnerabilities are disclosed.

## Reporting a Vulnerability

**Please do not report security vulnerabilities through public GitHub issues.**

Instead, report them by emailing the maintainers at **[security@re2-project.dev](mailto:security@re2-project.dev)**.

Please include:

- A description of the vulnerability
- Steps to reproduce or a proof-of-concept
- The affected component(s) (e.g., API, Functions, Web UI, CLI)
- The potential impact

### What to Expect

- **Acknowledgement** within **48 hours** of your report.
- **Status update** within **7 days** with an initial assessment and expected timeline.
- **Regular updates** at least every **14 days** until the issue is resolved or closed.

### If the Vulnerability Is Accepted

- A fix will be developed and tested privately.
- A security advisory will be published alongside the patch release.
- You will be credited in the advisory (unless you prefer to remain anonymous).

### If the Vulnerability Is Declined

- You will receive an explanation of why it was not accepted.
- If you disagree, you are welcome to provide additional information for reconsideration.

## Security Best Practices for Contributors

- Never commit secrets, credentials, connection strings, or API keys.
- Use Azure Managed Identity and Azure Key Vault for all credential management.
- Follow the principle of least privilege for all service configurations.
- Ensure all inputs are validated at system boundaries (API endpoints, CLI arguments).
