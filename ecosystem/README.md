# AntiScam Power Platform ecosystem

This folder is the starting point for turning the existing AntiScam APIs into a portfolio project for **Junior Software Developer (Power Platform & Copilot)**.

## What is already in the repository

- A Python/FastAPI risk engine with `POST /scan`.
- A C#/ASP.NET Core API platform with health, blog, gateway, and incident endpoints.
- SQLite storage and optional MongoDB storage for blocked submissions.
- Docker and Jenkins assets for the Python service.

## Target integration

```text
Copilot Studio agent ─┐
                      ├─ AntiScam custom connector ─ HTTPS ─ Python /scan API
Power Automate flow ──┘                         └─ C# incidents and blog API
                                                └─ Azure hosted services

MCP server (planned) ─ tools for scan_message and get_recent_incidents
```

The first Power Platform asset is [`power-platform/antiscam-connector.swagger.json`](power-platform/antiscam-connector.swagger.json), an OpenAPI 2.0 starter definition for a custom connector to the Python API. Replace `YOUR-API-HOST` with the deployed HTTPS hostname before importing it. The connector can then be used from Power Automate and added as a tool in Copilot Studio.

## Portfolio demo to build

1. Deploy the Python and C# APIs to Azure App Service (or an equivalent Azure hosting option) and configure secrets through application settings. Keep SQLite/MongoDB connection settings environment-based; do not commit credentials.
2. Import the OpenAPI file as a Power Platform custom connector and verify `ScanMessage` against the deployed API.
3. Build a Power Automate flow that accepts a message, calls `ScanMessage`, branches on the risk status, and sends a Teams/email alert for high-risk results. Include a human review step before any destructive or user-facing action.
4. Build a Copilot Studio agent that explains a scan result and can call the connector when asked to assess supplied text. Make clear that it gives a risk assessment, not a definitive verdict.
5. Add a small MCP server exposing narrowly scoped scan and incident lookup tools, with input validation and no secrets in tool output.
6. Document the Azure Portal deployment, connector setup, flow, agent, MCP transport, and a short end-to-end demo.

This repository can contain the backend, OpenAPI contract, deployment definitions, flow/agent setup notes, and demo script. The actual Azure deployment, Power Platform environment, connector connections, and published agent must be created and tested in an account with the required Microsoft access and licensing; repository files alone cannot establish that real tenant experience.

## Current status

Only the connector contract starter is present so far. The Azure deployment assets, Power Automate flow, Copilot Studio agent, and MCP server remain to be implemented and exercised in their respective environments.
