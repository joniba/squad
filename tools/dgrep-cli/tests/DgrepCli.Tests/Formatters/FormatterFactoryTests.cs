using System;
using Xunit;
using DgrepCli.Formatters;

namespace DgrepCli.Tests.Formatters
{
    public class FormatterFactoryTests
    {
        [Theory]
        [InlineData("table")]
        [InlineData("TABLE")]
        [InlineData("Table")]
        public void Create_Table_ReturnsTableFormatter(string format)
        {
            var f = FormatterFactory.Create(format);
            Assert.IsType<TableFormatter>(f);
        }

        [Theory]
        [InlineData("json")]
        [InlineData("JSON")]
        public void Create_Json_ReturnsJsonFormatter(string format)
        {
            var f = FormatterFactory.Create(format);
            Assert.IsType<JsonFormatter>(f);
        }

        [Theory]
        [InlineData("csv")]
        [InlineData("CSV")]
        public void Create_Csv_ReturnsCsvFormatter(string format)
        {
            var f = FormatterFactory.Create(format);
            Assert.IsType<CsvFormatter>(f);
        }

        [Fact]
        public void Create_Null_DefaultsToTable()
        {
            var f = FormatterFactory.Create(null);
            Assert.IsType<TableFormatter>(f);
        }

        [Fact]
        public void Create_UnknownFormat_Throws()
        {
            Assert.Throws<ArgumentException>(() => FormatterFactory.Create("xml"));
        }
    }
}
