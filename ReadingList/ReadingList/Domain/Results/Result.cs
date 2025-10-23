namespace ReadingList.Domain;

public class Result
{
    public bool IsSuccessful => Errors.Count == 0;
    public bool IsFailure => !IsSuccessful;
    public List<string> Errors { get; } = new();

    public Result()
    {
    }

    public Result(IEnumerable<string> errors)
    {
        if (errors != null)
        {
            Errors.AddRange(errors);
        }
    }

    public void AddError(string error)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            Errors.Add(error);
        }
    }

    public void AddErrors(IEnumerable<string> errors)
    {
        if (errors != null)
        {
            Errors.AddRange(errors);
        }
    }
}

public class Result<T> : Result
{
    public T? Value { get; }

    public Result()
    {
    }

    public Result(T value)
    {
        Value = value;
    }

    public Result(T value, IEnumerable<string> errors) : base(errors)
    {
        Value = value;
    }

    public Result(IEnumerable<string> errors) : base(errors)
    {
    }
}
