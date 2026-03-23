/**
 * Core DGrep types — derived from the DGrep .NET SDK documentation.
 * See docs/research/geneva-dgrep-research.md for source material.
 */

/** Query language type */
export type QueryType = 'KQL' | 'MQL';

/** Output format for CLI results */
export type OutputFormat = 'table' | 'json' | 'csv' | 'tsv' | 'jsonl';

/** Authentication method */
export type AuthMethod = 'default' | 'device-code' | 'certificate' | 'managed-identity';

/** Event filter — matches namespaces/events via regex */
export interface EventFilter {
  namespaceRegex: string;
  nameRegex: string;
  versionRegex?: string;
}

/** Identity column scoping (e.g., Tenant=WUS, Role=Frontend) */
export interface IdentityColumn {
  key: string;
  values: string[];
}

/** Query input — maps to the .NET SDK's QueryInput */
export interface QueryInput {
  mdsEndpoint: string;
  eventFilters: EventFilter[];
  identityColumns: IdentityColumn[];
  startTime: string; // ISO 8601
  endTime: string;   // ISO 8601
  serverQuery: string;
  serverQueryType: QueryType;
  clientQuery?: string;
  clientQueryType?: QueryType;
  maxRowCount?: number; // default 500000, max 1000000
}

/** A single result row — column name to value */
export type ResultRow = Record<string, unknown>;

/** Query completion status */
export type QueryStatus = 'Running' | 'Completed' | 'Failed' | 'Cancelled';

/** Row set result — maps to the SDK's RowSetResult */
export interface RowSetResult {
  rows: ResultRow[];
  columns: string[];
  queryStatus: {
    status: QueryStatus;
    message?: string;
    rowsReturned: number;
    rowsMatched: number;
  };
}

/** CSV export result */
export interface CsvResult {
  csvUri: string; // SAS URI, valid 6 hours
  queryStatus: {
    status: QueryStatus;
    rowsReturned: number;
  };
}

/** Known MDS endpoints */
export interface MdsEndpoint {
  name: string;
  url: string;
  environment: string;
  description?: string;
}

/** Saved query definition */
export interface SavedQuery {
  name: string;
  endpoint: string;
  namespace: string;
  event: string;
  identities?: IdentityColumn[];
  query: string;
  queryType: QueryType;
  clientQuery?: string;
  description?: string;
  createdAt: string;
  updatedAt: string;
}

/** CLI config schema */
export interface DgrepConfig {
  endpoints: Record<string, string>; // alias → MDS endpoint URL
  defaultEndpoint?: string;
  defaultNamespace?: string;
  defaultQueryType?: QueryType;
  defaultOutput?: OutputFormat;
  maxRowCount?: number;
}

/** DGrep v2 frontend endpoint */
export const DGREP_FRONTEND = 'https://dgrepv2-frontend-prod.trafficmanager.net';

/** Well-known MDS endpoints */
export const KNOWN_ENDPOINTS: MdsEndpoint[] = [
  {
    name: 'firstparty-prod',
    url: 'https://firstparty.monitoring.windows.net/',
    environment: 'FirstParty PROD',
    description: 'First-party production monitoring',
  },
  {
    name: 'diag-prod',
    url: 'https://production.diagnostics.monitoring.core.windows.net/',
    environment: 'Diagnostics PROD',
    description: 'Diagnostics production monitoring',
  },
];

/** Rate limit constants */
export const RATE_LIMITS = {
  maxConcurrentRequests: 5,
  defaultMaxRowCount: 500_000,
  absoluteMaxRowCount: 1_000_000,
  maxQueryRangeDays: 7,
  csvUriValidityHours: 6,
  defaultQueryTimeoutMinutes: 5,
} as const;
