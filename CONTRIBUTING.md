# Contributing to CLIF

Thank you for considering a contribution. CLIF automates desktop applications, so correctness, predictable failure reporting, and safe defaults matter as much as feature coverage.

## Before you start

- Read [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) and [SECURITY.md](SECURITY.md).
- Review [DEPENDENCY_MANAGEMENT.md](DEPENDENCY_MANAGEMENT.md) before changing
  package references or release metadata.
- Use a public issue to discuss substantial changes before investing in an implementation.
- Do not open public issues for vulnerabilities, secrets, or sensitive screenshots. Follow the private reporting process in `SECURITY.md`.
- Keep pull requests focused. Please avoid unrelated reformatting or generated-file changes.

## Development expectations

The production UI Automation backend is Windows-specific. WPF and real desktop/MCP validation should be performed on Windows. The Avalonia fixture is useful for provider-compatibility coverage, but it does not make the production backend cross-platform.

Run the relevant build and tests before opening a pull request. At minimum, run the affected project tests; for UI automation changes, include the Windows UI validation that exercises the changed behavior. State exactly what you ran and what could not be run in the pull request description.

When changing selectors, interactions, scripts, or MCP tools, add tests that verify observable UI effects and failure behavior. Prefer condition-based waits over fixed sleeps, preserve target identity, and make unsupported provider behavior explicit.

## Pull requests

1. Fork the repository and create a focused branch.
2. Add tests and documentation appropriate to the change.
3. Ensure no credentials, private UI data, or unnecessary screenshots are included.
4. Complete the pull-request template, including validation details.
5. Respond constructively to review feedback and keep commits easy to understand.

By contributing, you agree that your contributions are licensed under the repository's [MIT License](LICENSE).
