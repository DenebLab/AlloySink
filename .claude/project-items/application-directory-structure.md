# Application Directory Structure

## Execution Context-Based Directory Layout

The RobeNova application uses different root directories based on the execution context:

### Directory Roots

- `/dev/app.vs/` - Root application directory when running from Visual Studio
- `/dev/app.test/` - Root application directory when running tests

### Standard Directory Structure

Each root directory contains the following subdirectories:

- `config/` - Application configuration files
- `log/` - Application log files  
- `work/` - Working directory for application runtime files

### Example Structure

```
/dev/app.vs/
├── config/
│   ├── appsettings.json
│   ├── logging.config
│   └── browser.config
├── log/
│   ├── application.log
│   ├── error.log
│   └── debug.log
└── work/
    ├── temp/
    ├── cache/
    └── downloads/

/dev/app.test/
├── config/
│   ├── test.appsettings.json
│   └── test.logging.config
├── log/
│   ├── test.log
│   └── test-debug.log
└── work/
    ├── test-data/
    └── test-cache/
```

This structure ensures isolation between different execution environments and provides consistent organization for application resources.