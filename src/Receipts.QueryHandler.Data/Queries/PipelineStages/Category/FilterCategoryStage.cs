﻿using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.RegularExpressions;
using Receipts.QueryHandler.Domain.QueriesFilters;
using Receipts.QueryHandler.Data.Constants;

namespace Receipts.QueryHandler.Data.Queries.PipelineStages.Category
{
    internal static class FilterCategoryStage
    {
        internal static PipelineDefinition<Domain.Entities.Category, BsonDocument> FilterCategories(
            this PipelineDefinition<Domain.Entities.Category, BsonDocument> pipelineDefinition,
            CategoryFilters queryFilter)
        {
            var matchFilter = BuildMatchFilter(queryFilter);

            if (matchFilter != FilterDefinition<BsonDocument>.Empty)
            {
                pipelineDefinition = pipelineDefinition.Match(matchFilter);
            }

            return pipelineDefinition;
        }

        private static FilterDefinition<BsonDocument> BuildMatchFilter(CategoryFilters queryFilter)
        {
            var filters = new List<FilterDefinition<BsonDocument>>
            {
                MatchByUser(queryFilter.TenantId, queryFilter.UserId),
                MatchByTenant(queryFilter.TenantId),
                MatchByCategoriesIds(queryFilter),
                MatchByCategoryNames(queryFilter),
            };

            filters.RemoveAll(x => x == FilterDefinition<BsonDocument>.Empty);

            if (filters.Count == 0)
            {
                return FilterDefinition<BsonDocument>.Empty;
            }

            return filters.Count == 1 ? filters[0] : Builders<BsonDocument>.Filter.And(filters);
        }

        private static FilterDefinition<BsonDocument> MatchByUser(int tenantId, Guid userId)
        {
            if (tenantId == DataConstants.DefaultTenant)
            {

                var filter = new BsonDocument(
                    "UserId",
                    new BsonDocument("$eq", new BsonBinaryData(userId, GuidRepresentation.Standard)));

                return new BsonDocumentFilterDefinition<BsonDocument>(filter);
            }

            return FilterDefinition<BsonDocument>.Empty;
        }

        private static FilterDefinition<BsonDocument> MatchByTenant(int tenantId)
        {
            var filter = new BsonDocument(
                "Tenant.Number",
                new BsonDocument("$eq", tenantId));

            return new BsonDocumentFilterDefinition<BsonDocument>(filter);
        }

        private static FilterDefinition<BsonDocument> MatchByCategoriesIds(
            CategoryFilters queryFilter)
        {
            if (!(queryFilter?.CategoryIds?.Any() ?? false))
            {
                return FilterDefinition<BsonDocument>.Empty;
            }

            var categoriesIds = queryFilter!.CategoryIds!
                .Select(x => new BsonBinaryData(x, GuidRepresentation.Standard));

            var categories = new BsonDocument(
                "_id",
                new BsonDocument("$in", new BsonArray(categoriesIds)));

            return new BsonDocumentFilterDefinition<BsonDocument>(categories);
        }

        private static FilterDefinition<BsonDocument> MatchByCategoryNames(
            CategoryFilters queryFilter)
        {
            if (!(queryFilter?.CategoryNames?.Any() ?? false))
            {
                return FilterDefinition<BsonDocument>.Empty;
            }

            var categoryNames = queryFilter!.CategoryNames!
                .Select(categoryName => new BsonRegularExpression(new Regex(categoryName.Trim(), RegexOptions.IgnoreCase)));

            var filter = new BsonDocument(
                "Name",
                new BsonDocument("$in", BsonArray.Create(categoryNames)));

            return new BsonDocumentFilterDefinition<BsonDocument>(filter);
        }
    }
}
