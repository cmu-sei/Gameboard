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
    private static readonly int DEFAULT_PAGE_SIZE = 25;

    public PagedEnumerable<T> Page<T>(IEnumerable<T> items, PagingArgs pagingArgs = null)
    {
        var finalItems = items ?? [];
        var itemCount = finalItems.Count();

        if (pagingArgs is null)
        {
            pagingArgs = new()
            {
                PageNumber = 0,
                PageSize = DEFAULT_PAGE_SIZE,
            };
        }
        else
        {
            pagingArgs.PageSize ??= DEFAULT_PAGE_SIZE;
            pagingArgs.PageNumber ??= 0;
        }

        finalItems = finalItems
            .Skip(pagingArgs.PageNumber.Value * pagingArgs.PageSize.Value)
            .Take(pagingArgs.PageSize.Value);

        return new PagedEnumerable<T>
        {
            Items = finalItems,
            Paging = new PagingResults
            {
                ItemCount = itemCount,
                PageNumber = pagingArgs.PageNumber,
                PageSize = pagingArgs.PageSize
            }
        };
    }
}
