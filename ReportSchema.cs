using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public interface IReportField<T>
{
    public string Name { get; }
    public string Render(T value);
}

public class ReportField<T, U> : IReportField<T>
{
    private Func<U, string> format;

    public string Name { get; }
    public Func<T, U> Accessor { get; }
    public Func<U, string> Format {
        get => format;
        set
        {
            format = value ?? (o => o.ToString());
        }
    }

    string IReportField<T>.Render(T value)
    {
        return Format(Accessor(value));
    }

    public ReportField(string header, Func<T, U> accessor, Func<U, string> format = null)
    {
        Name = header;
        Accessor = accessor;
        Format = format;
    }
}

/** A schema builder for tabular layouts. */
public class ObjectSchema<T>
{
    public List<IReportField<T>> Fields { get; } = new List<IReportField<T>>();

    // Fluent syntax for adding columns
    public ObjectSchema<T> AddField<U>(string header, Func<T, U> accessor)
    {
        Fields.Add(new ReportField<T, U>(header, accessor));
        return this;
    }

    public ObjectSchema<T> AddField<U>(string header, Func<T, U> accessor, Func<U, string> format)
    {
        Fields.Add(new ReportField<T, U>(header, accessor, format));
        return this;
    }

    public ObjectSchema<T> AddField<U>(string header, Func<T, U> accessor, string format)
    {
        Fields.Add(new ReportField<T, U>(header, accessor, o => string.Format("{0:" + format + "}", o)));
        return this;
    }
}
