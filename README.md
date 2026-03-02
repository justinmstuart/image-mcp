# Image MCP Server

[![CI](https://github.com/justinmstuart/image-mcp/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/justinmstuart/image-mcp/actions/workflows/ci.yml)

A Model Context Protocol (MCP) server for searching images using the Unsplash API.

## Prerequisites

- .NET 8.0 SDK or later
- An Unsplash API access key ([Get one here](https://unsplash.com/developers))

## Configuration

This project requires an Unsplash API client ID to function. The API key should **never be committed to source control**. You can configure it using one of the following methods:

### Method 1: User Secrets (Recommended for Local Development)

This is the most secure method for local development:

```bash
dotnet user-secrets set "ImageApi:ClientId" "your-unsplash-api-key"
```

### Method 2: Environment Variables (Recommended for Production/CI)

Set the environment variable using the double-underscore notation:

**Windows (PowerShell):**

```powershell
$env:ImageApi__ClientId="your-unsplash-api-key"
```

**Windows (Command Prompt):**

```cmd
set ImageApi__ClientId=your-unsplash-api-key
```

**Linux/macOS:**

```bash
export ImageApi__ClientId="your-unsplash-api-key"
```

### Method 3: appsettings.Development.json (Alternative for Local Development)

Create a file named `appsettings.Development.json` (this file is already in `.gitignore`):

```json
{
  "ImageApi": {
    "ClientId": "your-unsplash-api-key"
  }
}
```

### Method 4: launchSettings.json (For Visual Studio/VS Code)

Edit `Properties/launchSettings.json` (this file is already in `.gitignore`) and replace the placeholder:

```json
{
  "profiles": {
    "image-mcp": {
      "commandName": "Project",
      "environmentVariables": {
        "ImageApi__ClientId": "your-unsplash-api-key"
      }
    }
  }
}
```

## Configuration Structure

The application expects the following configuration:

```json
{
  "ImageApi": {
    "BaseUrl": "https://api.unsplash.com/",
    "ClientId": "your-api-key"
  }
}
```

- **BaseUrl**: The Unsplash API base URL (already configured in `appsettings.json`)
- **ClientId**: Your Unsplash API access key (must be configured using one of the methods above)

## Building and Running

1. Clone the repository
2. Configure your Unsplash API key using one of the methods above
3. Build the project:
   ```bash
   dotnet build
   ```
4. Run the server:
   ```bash
   dotnet run
   ```

## CLI Mode (Basic)

This project also supports a basic CLI command for one-shot searches.

```bash
dotnet run -- search "nature"
```

You can also run:

```bash
dotnet run -- --version
dotnet run -- --help
```

- Output is printed as JSON to stdout.
- Command errors are written to stderr and return a non-zero exit code.
- Running without arguments still starts the MCP server mode.

## Configuration Validation

The application validates that all required configuration values are present at startup. If the `ClientId` is missing or invalid, the application will fail to start with a clear error message.

## Files Not Committed to Source Control

The following files contain sensitive information and are excluded from version control via `.gitignore`:

- `appsettings.Development.json`
- `appsettings.*.json` (except `appsettings.example.json`)
- `Properties/launchSettings.json`

## Getting an Unsplash API Key

1. Go to [https://unsplash.com/developers](https://unsplash.com/developers)
2. Create a new application
3. Copy your "Access Key" and use it as your `ClientId` in the configuration

## Tools Available

- **search_images**: Search for images, photos, or pictures using the Unsplash API. Returns an array of image results with URLs and descriptions.

## Claude Tool Selection

Claude and other LLM clients choose which tool to invoke based on the tool's `Description`. To ensure Claude prefers the Image MCP server over its built-in web search when handling image-related requests, the `search_images` tool description explicitly states:

- When to use this tool (any request to find, fetch, show, or search for images, photos, or pictures)
- That it should be preferred over web search for image-related requests

If you observe Claude defaulting to web search instead of this tool, verify that the MCP server is connected and that the tool description is visible to the model via `tools/list`.
