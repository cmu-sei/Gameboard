using System;
using System.Collections.Generic;
using System.Linq;

namespace Gameboard.Api.Common;

public sealed class PagedEnumerable<T>
{
    public required IEnumerable<T> Items { get; set; }
    public required PagingResults Paging { get; set; }
}

public sealed class PagingResults
{
    public required int ItemCount { get; set; }
    public required int? PageNumber { get; set; }
    public required int? PageSize { get; set; }
}

public sealed class PagingArgs
{
    public int? PageNumber { get; set; } = null;
    public int? PageSize { get; set; } = null;
}

public interface IPagingService
{
    PagedEnumerable<T> Page<T>(IEnumerable<T> items, PagingArgs pagingArgs = null);
}

internal class PagingService : IPagingService
{
    private static readonly int DEFAULT_PAGE_SIZE = 5;

    public PagedEnumerable<T> Page<T>(IEnumerable<T> items, PagingArgs pagingArgs = null)
    {
        var itemCount = items.Count();
        var finalItems = items;
        var isPaging = false;

        if (pagingArgs != null)
        {
            if ((pagingArgs.PageNumber == null || pagingArgs.PageSize == null) && pagingArgs.PageNumber != pagingArgs.PageSize)
                throw new ArgumentException($"If either of {nameof(pagingArgs.PageNumber)} or {nameof(pagingArgs.PageSize)} is specified when calling {nameof(Page)}, both most be specified.");
            else if (pagingArgs.PageNumber != null && pagingArgs.PageSize != null && pagingArgs.PageNumber.Value * pagingArgs.PageSize.Value > itemCount)
            {
                pagingArgs.PageNumber = 0;
                pagingArgs.PageSize = DEFAULT_PAGE_SIZE;

            }

            isPaging = pagingArgs.PageNumber != null;
        }

        if (isPaging)
        {
            finalItems = finalItems
                .Skip(pagingArgs.PageNumber.Value * pagingArgs.PageSize.Value)
                .Take(pagingArgs.PageSize.Value);
        }

        return new PagedEnumerable<T>
        {
            Items = finalItems,
            Paging = !isPaging ? null : new PagingResults
            {
                ItemCount = itemCount,
                PageNumber = pagingArgs.PageNumber,
                PageSize = pagingArgs.PageSize
            }
        };
    }
}
