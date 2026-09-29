namespace Its.Application.Common;

public class Result<T>
{
    public bool Succeeded { get; private set; }
    public T? Data { get; private set; }
    public string? Message { get; private set; }
    public string? Code { get; private set; }
    public IDictionary<string, string[]>? Errors { get; private set; }

    protected Result(bool succeeded, T? data, string? message, string? code, IDictionary<string, string[]>? errors)
    {
        Succeeded = succeeded;
        Data = data;
        Message = message;
        Code = code;
        Errors = errors;
    }

    public static Result<T> Success(T data, string? message = null)
    {
        return new Result<T>(true, data, message, null, null);
    }

    public static Result<T> Failure(string message, string code = "ERROR", IDictionary<string, string[]>? errors = null)
    {
        return new Result<T>(false, default, message, code, errors);
    }
}

public class Result : Result<object>
{
    protected Result(bool succeeded, string? message, string? code, IDictionary<string, string[]>? errors)
        : base(succeeded, null, message, code, errors)
    {
    }

    public static Result Success(string? message = null)
    {
        return new Result(true, message, null, null);
    }

    public static new Result Failure(string message, string code = "ERROR", IDictionary<string, string[]>? errors = null)
    {
        return new Result(false, message, code, errors);
    }
}

public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = new List<T>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / (PageSize > 0 ? PageSize : 10));
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public PagedResult() { }

    public PagedResult(IEnumerable<T> items, int totalCount, int pageNumber, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }
}

public class QueryParameters
{
    private const int MaxPageSize = 100;
    private int _pageSize = 10;

    public int PageNumber { get; set; } = 1;

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : (value <= 0 ? 10 : value);
    }

    public string? Search { get; set; }
    public string? SortBy { get; set; }
    public bool IsDescending { get; set; } = false;
}
