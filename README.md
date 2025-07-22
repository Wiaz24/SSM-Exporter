# SSM Exporter

[![License](https://img.shields.io/github/license/Wiaz24/CLI-Exporter)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-9.0-blue)](https://dotnet.microsoft.com/)

[//]: # ([![Platform]&#40;https://img.shields.io/badge/platform-linux--x64%20%7C%20win--x64-lightgrey&#41;]&#40;&#41;)

**SSM Exporter** is an open-source CLI tool to automatically manage parameters and secrets in AWS SSM Parameter store. It scans .NET configuration files (`appsettings.*json`) for values tagged with `ssm-parameter` or `ssm-secret`, and automatically generates Terraform code or executes AWS CLI commands.

---

