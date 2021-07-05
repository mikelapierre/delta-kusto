using DeltaKustoLib.CommandModel;
using DeltaKustoLib.CommandModel.Policies;
using System;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace DeltaKustoUnitTest.CommandParsing.Policies
{
    public class AlterIngestionBatchingPolicyTest : ParsingTestBase
    {
        [Fact]
        public void SimpleTable()
        {
            TestIngestionBatchingPolicy(EntityType.Table, "A", TimeSpan.FromDays(3), 2000, 200);
        }

        [Fact]
        public void FunkyTable()
        {
            TestIngestionBatchingPolicy(EntityType.Table, "A- 1", TimeSpan.FromDays(30), 1500, 150);
        }

        [Fact]
        public void DbComposedTableName()
        {
            var command = ParseOneCommand(
                ".alter table mydb.mytable policy ingestionbatching "
                + "@'{\"MaximumBatchingTimeSpan\":\"90.00:00:00\"}'");

            Assert.IsType<AlterIngestionBatchingCommand>(command);

            var realCommand = (AlterIngestionBatchingCommand)command;

            Assert.Equal(EntityType.Table, realCommand.EntityType);
            Assert.Equal("mytable", realCommand.EntityName.Name);
        }

        [Fact]
        public void ClusterComposedTableName()
        {
            var command = ParseOneCommand(
                ".alter table mycluster.['my db'].mytable policy ingestionbatching  "
                + "@'{\"MaximumBatchingTimeSpan\":\"90.00:00:00\"}'");

            Assert.IsType<AlterIngestionBatchingCommand>(command);

            var realCommand = (AlterIngestionBatchingCommand)command;

            Assert.Equal(EntityType.Table, realCommand.EntityType);
            Assert.Equal("mytable", realCommand.EntityName.Name);
        }

        [Fact]
        public void SimpleDatabase()
        {
            TestIngestionBatchingPolicy(EntityType.Database, "Db", TimeSpan.FromDays(42), 1200, 300);
        }

        [Fact]
        public void FunkyDatabase()
        {
            TestIngestionBatchingPolicy(EntityType.Database, "db.mine", TimeSpan.FromDays(300), 3000, 400);
        }

        private void TestIngestionBatchingPolicy(
            EntityType type,
            string name,
            TimeSpan maximumBatchingTimeSpan,
            int maximumNumberOfItems,
            int maximumRawDataSizeMb)
        {
            var commandText = new AlterIngestionBatchingCommand(
                type,
                new EntityName(name),
                maximumBatchingTimeSpan,
                maximumNumberOfItems,
                maximumRawDataSizeMb)
                .ToScript(null);
            var command = ParseOneCommand(commandText);

            Assert.IsType<AlterIngestionBatchingCommand>(command);

            var realCommand = (AlterIngestionBatchingCommand)command;

            Assert.Equal(type, realCommand.EntityType);
            Assert.Equal(name, realCommand.EntityName.Name);
            Assert.Equal(maximumBatchingTimeSpan, realCommand.MaximumBatchingTimeSpan);
            Assert.Equal(maximumNumberOfItems, realCommand.MaximumNumberOfItems);
            Assert.Equal(maximumRawDataSizeMb, realCommand.MaximumRawDataSizeMb);
        }
    }
}