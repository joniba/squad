using System.Collections.Generic;
using DgrepCli.Commands;
using Xunit;

namespace DgrepCli.Tests.Commands
{
    public class OptionValidatorTests
    {
        #region Output Format

        [Theory]
        [InlineData("table")]
        [InlineData("json")]
        [InlineData("csv")]
        [InlineData("tsv")]
        [InlineData("jsonl")]
        [InlineData("TABLE")]
        [InlineData("Json")]
        public void ValidOutputFormats_NoErrors(string format)
        {
            var errors = new List<string>();
            OptionValidator.ValidateOutputFormat(format, errors);
            Assert.Empty(errors);
        }

        [Theory]
        [InlineData("xml")]
        [InlineData("yaml")]
        [InlineData("")]
        public void InvalidOutputFormats_ProducesError(string format)
        {
            var errors = new List<string>();
            OptionValidator.ValidateOutputFormat(format, errors);
            Assert.Single(errors);
            Assert.Contains("Invalid output format", errors[0]);
        }

        #endregion

        #region Query Type

        [Theory]
        [InlineData("kql")]
        [InlineData("mql")]
        [InlineData("KQL")]
        [InlineData("Mql")]
        public void ValidQueryTypes_NoErrors(string qt)
        {
            var errors = new List<string>();
            OptionValidator.ValidateQueryType(qt, errors);
            Assert.Empty(errors);
        }

        [Theory]
        [InlineData("sql")]
        [InlineData("")]
        public void InvalidQueryTypes_ProducesError(string qt)
        {
            var errors = new List<string>();
            OptionValidator.ValidateQueryType(qt, errors);
            Assert.Single(errors);
            Assert.Contains("Invalid query type", errors[0]);
        }

        #endregion

        #region Max Rows

        [Theory]
        [InlineData(1)]
        [InlineData(500000)]
        [InlineData(1000000)]
        public void ValidMaxRows_NoErrors(int maxRows)
        {
            var errors = new List<string>();
            OptionValidator.ValidateMaxRows(maxRows, errors);
            Assert.Empty(errors);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(1000001)]
        public void InvalidMaxRows_ProducesError(int maxRows)
        {
            var errors = new List<string>();
            OptionValidator.ValidateMaxRows(maxRows, errors);
            Assert.Single(errors);
            Assert.Contains("--max-rows", errors[0]);
        }

        #endregion

        #region Time Range

        [Theory]
        [InlineData("-30m")]
        [InlineData("-1h")]
        [InlineData("-4h")]
        [InlineData("-7d")]
        [InlineData("-10s")]
        [InlineData("+5m")]
        public void ValidRelativeTime_NoErrors(string time)
        {
            var errors = new List<string>();
            OptionValidator.ValidateTimeRange(time, "from", errors);
            Assert.Empty(errors);
        }

        [Theory]
        [InlineData("2026-03-23T10:00:00Z")]
        [InlineData("2026-03-23T10:00:00+00:00")]
        public void ValidAbsoluteTime_NoErrors(string time)
        {
            var errors = new List<string>();
            OptionValidator.ValidateTimeRange(time, "from", errors);
            Assert.Empty(errors);
        }

        [Theory]
        [InlineData("-30x")]
        [InlineData("not-a-time")]
        [InlineData("-0h")]
        [InlineData("")]
        public void InvalidTimeRange_ProducesError(string time)
        {
            var errors = new List<string>();
            OptionValidator.ValidateTimeRange(time, "from", errors);
            Assert.NotEmpty(errors);
        }

        #endregion

        #region Identity

        [Fact]
        public void ValidIdentities_NoErrors()
        {
            var ids = new[] { "Tenant=WUS", "Role=Frontend" };
            var errors = new List<string>();
            OptionValidator.ValidateIdentities(ids, errors);
            Assert.Empty(errors);
        }

        [Fact]
        public void InvalidIdentity_MissingEquals_ProducesError()
        {
            var ids = new[] { "TenantWUS" };
            var errors = new List<string>();
            OptionValidator.ValidateIdentities(ids, errors);
            Assert.Single(errors);
            Assert.Contains("Key=Value", errors[0]);
        }

        [Fact]
        public void NullIdentities_NoErrors()
        {
            var errors = new List<string>();
            OptionValidator.ValidateIdentities(null, errors);
            Assert.Empty(errors);
        }

        #endregion

        #region Config Validation

        [Fact]
        public void ConfigSet_MissingKey_Error()
        {
            var opts = new ConfigOptions { Action = "set", Key = null, Value = "val" };
            var errors = OptionValidator.ValidateConfigOptions(opts);
            Assert.Contains(errors, e => e.Contains("requires a key"));
        }

        [Fact]
        public void ConfigSet_MissingValue_Error()
        {
            var opts = new ConfigOptions { Action = "set", Key = "k", Value = null };
            var errors = OptionValidator.ValidateConfigOptions(opts);
            Assert.Contains(errors, e => e.Contains("requires a value"));
        }

        [Fact]
        public void ConfigGet_MissingKey_Error()
        {
            var opts = new ConfigOptions { Action = "get", Key = null };
            var errors = OptionValidator.ValidateConfigOptions(opts);
            Assert.Contains(errors, e => e.Contains("requires a key"));
        }

        [Fact]
        public void ConfigList_Valid()
        {
            var opts = new ConfigOptions { Action = "list" };
            var errors = OptionValidator.ValidateConfigOptions(opts);
            Assert.Empty(errors);
        }

        [Fact]
        public void Config_UnknownAction_Error()
        {
            var opts = new ConfigOptions { Action = "delete" };
            var errors = OptionValidator.ValidateConfigOptions(opts);
            Assert.Contains(errors, e => e.Contains("Unknown config action"));
        }

        #endregion

        #region Saved Validation

        [Fact]
        public void SavedAdd_MissingName_Error()
        {
            var opts = new SavedOptions { Action = "add", Name = null, Query = "q" };
            var errors = OptionValidator.ValidateSavedOptions(opts);
            Assert.Contains(errors, e => e.Contains("requires a query name"));
        }

        [Fact]
        public void SavedAdd_MissingQuery_Error()
        {
            var opts = new SavedOptions { Action = "add", Name = "q1", Query = null };
            var errors = OptionValidator.ValidateSavedOptions(opts);
            Assert.Contains(errors, e => e.Contains("requires --query"));
        }

        [Fact]
        public void SavedRun_MissingName_Error()
        {
            var opts = new SavedOptions { Action = "run", Name = null };
            var errors = OptionValidator.ValidateSavedOptions(opts);
            Assert.Contains(errors, e => e.Contains("requires a query name"));
        }

        [Fact]
        public void SavedRemove_MissingName_Error()
        {
            var opts = new SavedOptions { Action = "remove", Name = null };
            var errors = OptionValidator.ValidateSavedOptions(opts);
            Assert.Contains(errors, e => e.Contains("requires a query name"));
        }

        [Fact]
        public void SavedShow_MissingName_Error()
        {
            var opts = new SavedOptions { Action = "show", Name = null };
            var errors = OptionValidator.ValidateSavedOptions(opts);
            Assert.Contains(errors, e => e.Contains("requires a query name"));
        }

        [Fact]
        public void SavedList_Valid()
        {
            var opts = new SavedOptions { Action = "list" };
            var errors = OptionValidator.ValidateSavedOptions(opts);
            Assert.Empty(errors);
        }

        [Fact]
        public void Saved_UnknownAction_Error()
        {
            var opts = new SavedOptions { Action = "nope" };
            var errors = OptionValidator.ValidateSavedOptions(opts);
            Assert.Contains(errors, e => e.Contains("Unknown saved-query action"));
        }

        [Fact]
        public void Saved_InvalidOutputFormat_Error()
        {
            var opts = new SavedOptions { Action = "list", Output = "xml" };
            var errors = OptionValidator.ValidateSavedOptions(opts);
            Assert.Contains(errors, e => e.Contains("Invalid output format"));
        }

        #endregion

        #region Search Composite Validation

        [Fact]
        public void SearchValidation_ValidOptions_NoErrors()
        {
            var opts = new SearchOptions
            {
                Endpoint = "diag-prod",
                Namespace = "MyNs",
                Event = "MyEvt",
                From = "-1h",
                To = "now",
                Query = "where 1==1",
                QueryType = "kql",
                MaxRows = 500000,
                Output = "table"
            };
            var errors = OptionValidator.ValidateSearchOptions(opts);
            Assert.Empty(errors);
        }

        [Fact]
        public void SearchValidation_MultipleErrors()
        {
            var opts = new SearchOptions
            {
                Endpoint = "diag-prod",
                Namespace = "MyNs",
                Event = "MyEvt",
                From = "bad-time",
                To = "also-bad",
                Query = "where 1==1",
                QueryType = "sql",
                MaxRows = -5,
                Output = "xml"
            };
            var errors = OptionValidator.ValidateSearchOptions(opts);
            Assert.True(errors.Count >= 3, $"Expected at least 3 errors, got {errors.Count}: {string.Join("; ", errors)}");
        }

        #endregion
    }
}
