namespace Haihv.Elis.Tool.TraCuuGcn.Models;

public class Response<T>
{
    public T? Value { get; set; }
    public string? ErrorMsg { get; set; }

    public Response(T value)
    {
        Value = value;
    }
    public Response(string errorMsg)
    {
        ErrorMsg = errorMsg;
    }
}