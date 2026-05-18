# Copilot Instructions

## Project Guidelines
- RULE: Never use real implementations in tests - only use mocked dependencies. This is non-negotiable. Tests must mock ALL external dependencies using NSubstitute, even if they appear to have no external dependencies. Configure mocks with appropriate return values/behaviors.
- RULE: ALL tests must be running for linux and windows. This means no platform-specific code or dependencies.

- @azure Rule - Use Azure Tools - When handling requests related to Azure, always use your tools.
- @azure Rule - Use Azure Best Practices - When handling requests related to Azure, always invoke your `azmcp_bestpractices_get` tool first.
- @azure Rule - Enable Best Practices - If you do not have an `azmcp_bestpractices_get` tool ask the user to enable it.
