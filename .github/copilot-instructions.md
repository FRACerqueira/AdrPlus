# Copilot Instructions

## Project Guidelines
- RULE: Never use real implementations in tests - only use mocked dependencies. This is non-negotiable. Tests must mock ALL external dependencies using NSubstitute, even if they appear to have no external dependencies. Configure mocks with appropriate return values/behaviors.
- RULE: ALL tests must be running for linux and windows. This means no platform-specific code or dependencies.
