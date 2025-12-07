using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public interface IReportColumn
{
    public string Header { get; }
    public string Render(object value);
}

public class ReportColumn<T, U> : IReportColumn
{
    private Func<U, string> format;

    public string Header { get; }
    public Func<T, U> Accessor { get; }
    public Func<U, string> Format {
        get => format;
        set
        {
            format = value ?? (o => o.ToString());
        }
    }

    string IReportColumn.Render(object value)
    {
        return Format(Accessor((T)value));
    }

    public ReportColumn(string header, Func<T, U> accessor, Func<U, string> format = null)
    {
        Header = header;
        Accessor = accessor;
        Format = format;
    }
}

// A simple builder to hold the definitions
public class ReportSchema<T>
{
    public List<IReportColumn> Columns { get; } = new List<IReportColumn>();

    // Fluent syntax for adding columns
    public ReportSchema<T> AddColumn<U>(string header, Func<T, U> accessor)
    {
        Columns.Add(new ReportColumn<T, U>(header, accessor));
        return this;
    }

    public ReportSchema<T> AddColumn<U>(string header, Func<T, U> accessor, Func<U, string> format)
    {
        Columns.Add(new ReportColumn<T, U>(header, accessor, format));
        return this;
    }

    public ReportSchema<T> AddColumn<U>(string header, Func<T, U> accessor, string format)
    {
        Columns.Add(new ReportColumn<T, U>(header, accessor, o => string.Format("{0:" + format + "}", o)));
        return this;
    }
}
