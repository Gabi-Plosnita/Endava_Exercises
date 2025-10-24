using System.Text;

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
            sb.AppendLine($"  {i + 1}. {Errors[i]}");
        }

        return sb.ToString().TrimEnd();
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
