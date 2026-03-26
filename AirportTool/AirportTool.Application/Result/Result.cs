using System.Text;

namespace AirportTool.Application;

public class Result
{
    public bool IsSuccessful => Errors.Count == 0;
    public bool IsFailure => !IsSuccessful;
    public List<Error> Errors { get; } = new();

    public Result()
    {
    }

    public Result(IEnumerable<Error> errors)
    {
        if (errors != null)
        {
            Errors.AddRange(errors);
        }
    }

    public void AddError(Error error)
    {
        if (error != null)
        {
            Errors.Add(error);
        }
    }

    public void AddErrors(IEnumerable<Error> errors)
    {
        if (errors != null)
        {
            Errors.AddRange(errors);
        }
    }

    public override string ToString()
    {
        if (Errors.Count == 0)
        {
            return "Success";
        }

        var sb = new StringBuilder();
        sb.AppendLine("Errors:");

        for (int i = 0; i < Errors.Count; i++)
        {
            sb.AppendLine($"  {i + 1}. {Errors[i].Message} (Type: {Errors[i].Type})");
        }

        return sb.ToString().TrimEnd();
    }
}

public class Result<T> : Result
{
    public T? Value { get; set; }

    public Result()
    {
    }

    public Result(T value)
    {
        Value = value;
    }

    public Result(T value, IEnumerable<Error> errors) : base(errors)
    {
        Value = value;
    }

    public Result(IEnumerable<Error> errors) : base(errors)
    {
    }
}

