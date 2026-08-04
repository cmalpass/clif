# Security Policy

## Security posture

CLIF can inspect and control desktop applications. Its CLI and MCP server can expose window metadata, UI text, keyboard input, screenshots, and process-launch behavior. Treat it as a trusted, local-development tool until a release explicitly documents stronger policy controls.

Do not connect the MCP server to untrusted prompts, users, or remote clients. Do not use it against applications containing credentials, private data, or critical production workflows unless you have independently assessed the risk and scoped the environment.

## Supported versions

Security fixes are applied to the latest development version. There is not yet a supported stable release line or security maintenance window.

## Reporting a vulnerability

Please report suspected vulnerabilities privately through [GitHub Security Advisories](https://github.com/cmalpass/clif/security/advisories/new). Include:

- a clear description and potential impact;
- reproducible steps or a minimal proof of concept;
- affected commit, version, and environment;
- whether the issue can access data, control another application, bypass an intended boundary, or cause denial of service.

Please do not disclose the issue publicly or include secrets, tokens, private screenshots, or customer data in the report. We will acknowledge a valid report, investigate it, and coordinate a disclosure timeline with the reporter when possible.

## Scope guidance

Potentially sensitive areas include MCP tool authorization, process launch and window ownership, screenshot/text extraction, script-file access, selector/reference integrity, JSON-RPC handling, package and workflow supply chain, and Windows UI Automation interactions.
