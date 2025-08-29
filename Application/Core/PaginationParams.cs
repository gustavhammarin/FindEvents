using System;

namespace Application.Activities.Core;

public class PaginationParams<TCursor>
{
    private const int MaxPageSize = 50;
    public TCursor? Cursor { get; set; }
    private int _pagesize = 16;
    public int PageSize
    {
        get => _pagesize;
        set => _pagesize = (value > MaxPageSize) ? MaxPageSize : value;
    }
}
