using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BetterConsoleTables;

public static class Renderers
{
    public static string RenderMarkdownTable<T>(IEnumerable<T> rows,
                                                ReportSchema<T> schema)
    {
        Table table = new(TableConfiguration.Markdown());
        table.AddColumns(columns: [.. schema.Columns.Select(c => c.Header)]);

        foreach (var row in rows)
        {
            var cells = schema.Columns.Select(col => col.Render(row));
            table.AddRow([.. cells]);
        }
        return table.ToString();
    }
}
