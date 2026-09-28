# PatrolAI Nebius Connectivity

## Purpose

Phase 2 provides an isolated Nebius Token Factory connectivity layer for PatrolAI. It reads credentials from the process environment and uses an authenticated `GET /v1/models` request to verify connectivity and parse the returned model list.

This layer is not connected to OpenCV, `PatrolObservation`, `PatrolDecision`, scenes, controllers, UI, inspector movement, re-observation, or patrol behavior.

## Credential Setup

Use the `NEBIUS_API_KEY` environment variable. Never paste a real key into project files.

For the current Windows PowerShell session:

```powershell
$env:NEBIUS_API_KEY="YOUR_KEY_HERE"
```

To create a persistent Windows user environment variable:

```powershell
[Environment]::SetEnvironmentVariable(
    "NEBIUS_API_KEY",
    "YOUR_KEY_HERE",
    "User"
)
```

Restart Unity and any open terminal after creating or changing the persistent variable so the new process can read it.

## Security

- Never commit API keys.
- Never print API keys or Authorization headers in logs.
- Never include API keys in screenshots.
- Never store API keys in public repository files.
- Use placeholders such as `YOUR_KEY_HERE`, never real or real-looking keys, in documentation.

`NebiusConnectionResult` redacts the configured key if a remote response or exception unexpectedly contains it.

## API

```text
Base: https://api.tokenfactory.nebius.com
Connectivity endpoint: /v1/models
Authorization: Bearer token supplied from NEBIUS_API_KEY
```

`NebiusConfig` allows the base URL, environment-variable name, finite timeout, and future model ID to be configured without changing the client implementation.

## Isolated Usage

The client does not run automatically. A later authorized integration or local test harness can call it explicitly:

```csharp
using (var client = new PatrolAI.Nebius.NebiusClient())
{
    PatrolAI.Nebius.NebiusConnectionResult result =
        await client.TestConnectionAsync();
}
```

For a credentialed manual endpoint check after setting the current-shell variable, run this single PowerShell command:

```powershell
Invoke-RestMethod -Method Get -Uri "https://api.tokenfactory.nebius.com/v1/models" -Headers @{ Authorization = "Bearer $env:NEBIUS_API_KEY" }
```

The command references the environment variable without printing its value. Do not capture the command's process environment or Authorization header in screenshots or logs.
