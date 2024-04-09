﻿using Receipts.ReadModel.QueriesFilters.PageFilters;

namespace Receipts.ReadModel.QueriesFilters
{
    public class CategoryFilters(IEnumerable<Guid>? categoryIds,
        IEnumerable<string>? categoryNames,
        short pageNumber,
        short pageSize) : PageFilter(pageNumber, pageSize)
    {
        public IEnumerable<Guid>? CategoryIds { get; set; } = categoryIds;

        public IEnumerable<string>? CategoryNames { get; set; } = categoryNames;
    }
}
