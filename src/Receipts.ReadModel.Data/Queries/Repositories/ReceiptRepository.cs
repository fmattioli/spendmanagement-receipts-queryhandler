﻿using Receipts.ReadModel.Interfaces;
using Receipts.ReadModel.Queries.GetReceipts;
using Receipts.ReadModel.QueriesFilters;
using Receipts.ReadModel.QueriesFilters.PageFilters;
using MongoDB.Bson;
using MongoDB.Driver;
using Receipts.ReadModel.Data.Queries.PipelineStages;
using Receipts.ReadModel.Data.Queries.PipelineStages.Receipt;
using Receipts.ReadModel.Data.Queries.PipelineStages.RecurringReceipt;
using Receipts.ReadModel.Entities;

namespace Receipts.ReadModel.Data.Queries.Repositories
{
    public class ReceiptRepository(IMongoDatabase mongoDb) : IReceiptRepository
    {
        private readonly IMongoCollection<Receipt> receiptCollection = mongoDb.GetCollection<Receipt>("Receipts");
        private readonly IMongoCollection<RecurringReceipt> recurringReceiptCollection = mongoDb.GetCollection<RecurringReceipt>("RecurringReceipts");

        public async Task<PagedResultFilter<Receipt>> GetVariableReceiptsAsync(ReceiptFilters queryFilter)
        {
            var filteredResults = await FindVariableReceiptsResultsAsync(queryFilter);
            var totaResults = await GetTotalResultsCountAsync(queryFilter);
            var receiptsTotal = await GetReceiptsTotalAmount(queryFilter);

            var orderedResults = filteredResults.OrderByDescending(x => x.ReceiptDate);

            return new PagedResultFilter<Receipt>
            {
                PageSize = queryFilter.PageSize,
                Results = orderedResults,
                ReceiptsTotalAmount = receiptsTotal,
                TotalResults = (int)totaResults
            };
        }

        public async Task<PagedResultFilter<RecurringReceipt>> GetRecurringReceiptsAsync(RecurringReceiptFilters queryFilter)
        {
            var filteredResults = await FindRecurringReceiptsResultsAsync(queryFilter);
            var totaResults = await GetTotalResultsCountAsync(queryFilter);

            var orderedResults = filteredResults.OrderByDescending(x => x.DateInitialRecurrence);
            var aggregateCountResult = totaResults?.Count ?? 0;

            return new PagedResultFilter<RecurringReceipt>
            {
                PageSize = queryFilter.PageSize,
                Results = orderedResults,
                TotalResults = (int)aggregateCountResult
            };
        }

        private async Task<IEnumerable<RecurringReceipt>> FindRecurringReceiptsResultsAsync(RecurringReceiptFilters queryFilter)
        {
            var pipelineDefinition = PipelineDefinitionBuilder
                .For<RecurringReceipt>()
                .As<RecurringReceipt, RecurringReceipt, BsonDocument>()
                .FilterRecurringReceipts(queryFilter)
                .Paginate(queryFilter.PageSize, queryFilter.PageNumber);
                
            var resultsPipeline = pipelineDefinition.As<RecurringReceipt, BsonDocument, RecurringReceipt>();

            var aggregation = await recurringReceiptCollection.AggregateAsync(
                resultsPipeline,
                new AggregateOptions { AllowDiskUse = true, MaxTime = Timeout.InfiniteTimeSpan, });

            return await aggregation.ToListAsync();
        }

        protected async Task<AggregateCountResult> GetTotalResultsCountAsync(RecurringReceiptFilters queryFilter)
        {
            var pipelineDefinition = PipelineDefinitionBuilder
                .For<RecurringReceipt>()
                .As<RecurringReceipt, RecurringReceipt, BsonDocument>()
                .FilterRecurringReceipts(queryFilter);

            PipelineDefinition<RecurringReceipt, AggregateCountResult> totalResultsCountPipeline;

            totalResultsCountPipeline = pipelineDefinition.Count();

            var aggregation = await this.recurringReceiptCollection.AggregateAsync(
                totalResultsCountPipeline,
                new AggregateOptions { AllowDiskUse = true });

            return await aggregation.FirstOrDefaultAsync();
        }

        private async Task<IEnumerable<Receipt>> FindVariableReceiptsResultsAsync(ReceiptFilters queryFilter)
        {
            var pipelineDefinition = PipelineDefinitionBuilder
                            .For<Receipt>()
                            .As<Receipt, Receipt, BsonDocument>()
                            .FilterReceipts(queryFilter)
                            .FilterReceiptItems(queryFilter)
                            .Paginate(queryFilter.PageSize, queryFilter.PageNumber);

            var resultsPipeline = pipelineDefinition.As<Receipt, BsonDocument, Receipt>();

            var aggregation = await receiptCollection.AggregateAsync(
                                  resultsPipeline,
                                  new AggregateOptions { AllowDiskUse = true, MaxTime = Timeout.InfiniteTimeSpan, });

            return await aggregation.ToListAsync();
        }

        private async Task<long> GetTotalResultsCountAsync(ReceiptFilters queryFilter)
        {
            var pipelineDefinition = PipelineDefinitionBuilder
                .For<Receipt>()
                .As<Receipt, Receipt, BsonDocument>()
                .FilterReceipts(queryFilter)
                .FilterReceiptItems(queryFilter);


            PipelineDefinition<Receipt, AggregateCountResult> totalResultsCountPipeline;

            totalResultsCountPipeline = pipelineDefinition.Count();

            var aggregation = await this.receiptCollection.AggregateAsync(
                                  totalResultsCountPipeline,
                                  new AggregateOptions { AllowDiskUse = true });

            var totaResults = await aggregation.FirstOrDefaultAsync();
            return totaResults?.Count ?? 0;
        }

        private async Task<decimal> GetReceiptsTotalAmount(ReceiptFilters queryFilter)
        {
            var pipelineDefinition = PipelineDefinitionBuilder
                .For<Receipt>()
                .As<Receipt, Receipt, BsonDocument>()
                .FilterReceipts(queryFilter)
                .FilterReceiptItems(queryFilter)
                .MakeSumTotalReceipts();

            var aggregateOptions = new AggregateOptions { AllowDiskUse = true };

            var aggregation = await receiptCollection.AggregateAsync(pipelineDefinition, aggregateOptions);

            var document = await aggregation.FirstOrDefaultAsync();

            if (document != null && document.Contains("total"))
                return document["total"].AsDecimal;

            return 0;
        }
    }
}
