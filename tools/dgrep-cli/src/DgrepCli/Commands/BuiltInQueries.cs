using System.Collections.Generic;
using DgrepCli.Config;

namespace DgrepCli.Commands
{
    /// <summary>
    /// Built-in saved query templates for common ICM investigation scenarios.
    /// Seeded into fresh configs on first use.
    /// </summary>
    public static class BuiltInQueries
    {
        public static Dictionary<string, SavedQuery> GetAll()
        {
            return new Dictionary<string, SavedQuery>
            {
                ["icm-errors"] = new SavedQuery
                {
                    Query =
@"let startTime = ago({{time_window}});
let endTime = now();
exceptions
| where timestamp between (startTime .. endTime)
| summarize ErrorCount = count() by ResourceType = tostring(customDimensions['ResourceType']),
    ErrorMessage = tostring(customDimensions['ErrorMessage'])
| order by ErrorCount desc
| take 50",
                    Description = "Error rates by resource type in a time window",
                    DefaultDatabase = "Diagnostics"
                },

                ["icm-latency"] = new SavedQuery
                {
                    Query =
@"let startTime = ago({{time_window}});
let endTime = now();
requests
| where timestamp between (startTime .. endTime)
| where operation_Name has '{{operation}}'
| summarize P50 = percentile(duration, 50),
    P95 = percentile(duration, 95),
    P99 = percentile(duration, 99),
    RequestCount = count()
    by bin(timestamp, 5m), operation_Name
| order by timestamp desc",
                    Description = "P50/P95/P99 latency by operation",
                    DefaultDatabase = "Diagnostics"
                },

                ["icm-throttling"] = new SavedQuery
                {
                    Query =
@"let startTime = ago({{time_window}});
let endTime = now();
requests
| where timestamp between (startTime .. endTime)
| where resultCode == '429'
| summarize ThrottledCount = count()
    by SubscriptionId = tostring(customDimensions['SubscriptionId']),
    ResourceType = tostring(customDimensions['ResourceType']),
    bin(timestamp, 15m)
| order by ThrottledCount desc
| take 100",
                    Description = "Throttled requests by subscription",
                    DefaultDatabase = "Diagnostics"
                }
            };
        }
    }
}
