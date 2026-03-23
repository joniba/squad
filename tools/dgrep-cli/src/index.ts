#!/usr/bin/env node

/**
 * dgrep-cli — Cross-platform CLI for Geneva DGrep log search
 */

import { Command } from 'commander';

const program = new Command();

program
  .name('dgrep')
  .description('Cross-platform CLI for querying Geneva DGrep (Distributed Grep) logs')
  .version('0.1.0');

// Commands will be registered here as they are implemented
// See PLAN.md Phase 1 items 1.2+

program.parse();
