# SSM Exporter

[![License](https://img.shields.io/github/license/your-org/ssm-exporter)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-9.0-blue)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/platform-linux--x64%20%7C%20win--x64-lightgrey)]()

**SSM Exporter** is an open-source CLI tool to automatically manage parameters and secrets in AWS SSM Parameter store. It scans .NET configuration files (`appsettings.*json`) for values tagged with `ssm-parameter` or `ssm-secret`, and automatically generates Terraform code or executes AWS CLI commands.

---

