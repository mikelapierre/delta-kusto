using DeltaKustoLib.CommandModel;
using System;
using System.Linq;
using Xunit;

namespace DeltaKustoUnitTest.CommandParsing
{
    public class AlterMergeTableColumnDocStringsTest : ParsingTestBase
    {
        [Fact]
        public void OneColumn()
        {
            var tableName = "t1";
            var columns = new[]
            {
                (name: "TimeStamp", docString: "Time of day")
            };
            var command = ParseOneCommand(
                $".alter-merge table {tableName} column-docstring "
                + $"({string.Join(", ", columns.Select(c => $"{c.name}:\"{c.docString}\""))})");

            ValidateColumnCommand(command, tableName, columns);
        }

        [Fact]
        public void TwoColumns()
        {
            var tableName = "t2";
            var columns = new[]
            {
                (name: "TimeStamp", docString: "Time of day"),
                (name: "ac", docString: "acceleration")
            };
            var command = ParseOneCommand(
                $".alter-merge table {tableName} column-docstring "
                + $"({string.Join(", ", columns.Select(c => $"{c.name}:\"{c.docString}\""))})");

            ValidateColumnCommand(command, tableName, columns);
        }

        private static void ValidateColumnCommand(
            CommandBase command,
            string tableName,
            (string name, string docString)[] columns)
        {
            Assert.IsType<CreateTableCommand>(command);

            var alterColumnCommand = (AlterMergeTableColumnDocStringsCommand)command;

            Assert.Equal(tableName, alterColumnCommand.TableName);
            Assert.Equal(columns.Length, alterColumnCommand.Columns.Count);
            for (int i = 0; i != columns.Length; ++i)
            {
                Assert.Equal(columns[i].name, alterColumnCommand.Columns[i].ColumnName);
                Assert.Equal(columns[i].docString, alterColumnCommand.Columns[i].DocString);
            }
        }
    }
}