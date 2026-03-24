# Changelog

## [Unreleased]

### Breaking Changes

- **`dgrep query` verb removed.** The `dgrep query` command has been replaced by
  `dgrep saved run`. Update any scripts or workflows that invoke `dgrep query` to
  use `dgrep saved run <name>` instead. See `dgrep saved --help` for usage.

### Changed

- Migrated from Kusto query model (`Cluster`/`Database`) to DGrep SDK model
  (`Endpoint`/`Namespace`/`Event`/`QueryType`). Config files using
  `defaultCluster` or `defaultDatabase` will silently ignore those fields.
- `dgrep auth test` now validates Azure CLI authentication against
  `management.azure.com`. A warning is printed in the output noting that DGrep
  SDK uses dSTS for authentication, which is validated separately at query time.
