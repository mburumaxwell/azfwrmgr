# azfwrmgr – Azure Firewall Rules Manager CLI Tool

[![NuGet](https://img.shields.io/nuget/v/azfwrmgr.svg)](https://www.nuget.org/packages/azfwrmgr/)
[![GitHub Workflow Status](https://github.com/mburumaxwell/azfwrmgr/actions/workflows/build.yml/badge.svg)](https://github.com/mburumaxwell/azfwrmgr/actions)
[![Release](https://img.shields.io/github/release/mburumaxwell/azfwrmgr.svg)](https://github.com/mburumaxwell/azfwrmgr/releases/latest)
[![license](https://img.shields.io/github/license/mburumaxwell/azfwrmgr.svg)](LICENSE)

A cross-platform .NET global tool that resolves friendly hostnames or CIDR networks into IP addresses and automatically synchronizes Azure PaaS firewall rules (SQL, PostgreSQL, CosmosDB-for-PostgreSQL, MongoDB, etc).

## ✅ Features

- Syncs Azure SQL Database server firewall rules
- Syncs Azure PostgreSQL Flexible Server firewall rules
- Syncs Azure CosmosDB-for-PostgreSQL cluster firewall rules
- Syncs Azure CosmosDB MongoDB-API cluster firewall rules
- Supports both FQDN-to-IP resolution and static CIDR networks
- Interactive mode (browser sign-in) or headless mode (config file)
- “Dry-run” option to preview changes without applying
- Extensible via provider model for additional PaaS services

## 🚀 CLI Usage

### 1. Interactive / Developer Mode

```bash
azfwrmgr --config ./config.json --interactive --dry-run
```

- Uses `DefaultAzureCredential` with interactive browser login allowed.
- Useful on dev laptops where `az login` has already been run.

### 2. Headless / Automated Mode

```bash
azfwrmgr --config ./config.json --dry-run
```

#### ⚙️ Config File Format (`config.json`)

```json
{
  "fqdns": [
    { "name": "MAXHOME", "fqdn": "office.maxwellweru.io" }
  ],
  "networks": [
    { "name": "OTHERHOME", "network": "216.198.79.1/32" }
  ],
  "subscription": [ "<subscription-id-or-name>" ],      // empty or null means all subscriptions
  "separator": "_",                                     // default: "-"
  "cosmosForPostgreSql": true,                          // default: true
  "mongoCluster": true,                                 // default: true
  "postgres": true,                                     // default: true
  "sql": true                                           // default: true
}
```

- **fqdns**: list of `{ name, fqdn }` entries to DNS-resolve.
- **networks**: list of `{ name, network }` CIDR blocks.
- **subscription**: which Azure subscriptions to target.
- **separator**: character between “name” and IP index in rule names.
- **\* flags**: enable/disable each provider (SQL, Postgres, CosmosDB-for-PostgreSQL, MongoDB).

## 🔐 Authentication Strategy

Leverages Azure.Identity’s `DefaultAzureCredential`, which tries in order:

1. Environment variables (`AZURE_TENANT_ID`, `AZURE_CLIENT_ID`, `AZURE_CLIENT_SECRET`)
2. Managed identity (when running in Azure)
3. Azure CLI / Visual Studio credentials

### Service Principal (Headless)

```bash
export AZURE_TENANT_ID=<tenant-id>
export AZURE_CLIENT_ID=<client-id>
export AZURE_CLIENT_SECRET=<client-secret>
azfwrmgr --config ~/.azfwrmgr/config.json
```

## 📥 Installation

### 🍎 macOS (Homebrew)

```bash
brew install mburumaxwell/tap/azfwrmgr
```

### 🐧 Linux (DEB, RPM, APK)

Download the appropriate `.deb`, `.rpm`, or `.apk` from [Releases](https://github.com/mburumaxwell/azfwrmgr/releases) and install via:

```bash
# Debian/Ubuntu
sudo dpkg -i azfwrmgr-<version>-linux-<arch>.deb

# RHEL/Fedora/AlmaLinux
sudo dnf install -y azfwrmgr-<version>-linux-<arch>.rpm

# Alpine
sudo apk add --allow-untrusted azfwrmgr-<version>-linux-<arch>.apk
```

### 🖥️ Windows (Scoop)

```bash
scoop bucket add mburumaxwell https://github.com/mburumaxwell/scoop-tools.git
scoop install azfwrmgr
```

### 🛠️ .NET Tool

```bash
dotnet tool install --global azfwrmgr
azfwrmgr --help
```

### 🐳 Docker

```bash
docker run --rm -it \
  --env AZURE_TENANT_ID=<tenant> \
  --env AZURE_CLIENT_ID=<client> \
  --env AZURE_CLIENT_SECRET=<secret> \
  -v ~/.azfwrmgr:/config \
  ghcr.io/mburumaxwell/azfwrmgr \
  --config /config/config.json --dry-run
```

## ☸️ Kubernetes Deployment

Apply the sample manifest:

```bash
kubectl apply -f k8s/cron.yml
kubectl logs -l app=azfwrmgr -f
```

## Alternatives

While there are scripts and runbooks for individual Azure PaaS services, **azfwrmgr** uniquely unifies DNS-driven syncing across SQL, PostgreSQL, CosmosDB-for-PostgreSQL, MongoDB, etc in one extensible CLI.

## License

This project is licensed under the MIT License; see [LICENSE](./LICENSE) for details.
