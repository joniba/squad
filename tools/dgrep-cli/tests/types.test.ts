import { describe, it, expect } from 'vitest';
import { KNOWN_ENDPOINTS, RATE_LIMITS, DGREP_FRONTEND } from '../src/types/index.js';
import type { QueryInput, EventFilter, IdentityColumn, RowSetResult } from '../src/types/index.js';

describe('DGrep types and constants', () => {
  describe('KNOWN_ENDPOINTS', () => {
    it('should include firstparty-prod and diag-prod', () => {
      const names = KNOWN_ENDPOINTS.map(e => e.name);
      expect(names).toContain('firstparty-prod');
      expect(names).toContain('diag-prod');
    });

    it('should have valid URLs', () => {
      for (const endpoint of KNOWN_ENDPOINTS) {
        expect(endpoint.url).toMatch(/^https:\/\//);
      }
    });
  });

  describe('RATE_LIMITS', () => {
    it('should enforce 5 concurrent requests', () => {
      expect(RATE_LIMITS.maxConcurrentRequests).toBe(5);
    });

    it('should have correct max row counts', () => {
      expect(RATE_LIMITS.defaultMaxRowCount).toBe(500_000);
      expect(RATE_LIMITS.absoluteMaxRowCount).toBe(1_000_000);
    });

    it('should limit query range to 7 days', () => {
      expect(RATE_LIMITS.maxQueryRangeDays).toBe(7);
    });
  });

  describe('DGREP_FRONTEND', () => {
    it('should point to the v2 frontend', () => {
      expect(DGREP_FRONTEND).toBe('https://dgrepv2-frontend-prod.trafficmanager.net');
    });
  });

  describe('type contracts', () => {
    it('should allow constructing a valid QueryInput', () => {
      const input: QueryInput = {
        mdsEndpoint: 'https://production.diagnostics.monitoring.core.windows.net/',
        eventFilters: [{ namespaceRegex: '^TestNS$', nameRegex: '^TestEvent$' }],
        identityColumns: [{ key: 'Tenant', values: ['WUS'] }],
        startTime: '2026-03-23T10:00:00Z',
        endTime: '2026-03-23T11:00:00Z',
        serverQuery: 'source | where Level <= 2',
        serverQueryType: 'KQL',
      };
      expect(input.mdsEndpoint).toBeDefined();
      expect(input.eventFilters).toHaveLength(1);
      expect(input.serverQueryType).toBe('KQL');
    });

    it('should allow constructing a valid RowSetResult', () => {
      const result: RowSetResult = {
        rows: [{ PreciseTimeStamp: '2026-03-23T10:30:00Z', Message: 'test error', Level: 2 }],
        columns: ['PreciseTimeStamp', 'Message', 'Level'],
        queryStatus: { status: 'Completed', rowsReturned: 1, rowsMatched: 1 },
      };
      expect(result.rows).toHaveLength(1);
      expect(result.queryStatus.status).toBe('Completed');
    });
  });
});
