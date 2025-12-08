using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BetterConsoleTables;

public static class Renderers
{
    public static string RenderMarkdownTable<T>(IEnumerable<T> rows,
                                                ObjectSchema<T> schema)
    {
        Table table = new(TableConfiguration.Markdown());
        table.AddColumns(columns: [.. schema.Fields.Select(c => c.Name)]);

        foreach (var row in rows)
        {
            var cells = schema.Fields.Select(col => col.Render(row));
            table.AddRow([.. cells]);
        }
        return table.ToString();
    }

    public static string RenderMarkdownDescription<T>(T obj,
                                                      ObjectSchema<T> schema)
    {
        StringBuilder result = new();
        bool first = true;
        foreach (var field in schema.Fields)
        {
            if (first) {
                result.Append("## ");
                first = false;
            }
            result.AppendLine($"{field.Name}: {field.Render(obj)}");
        }

        return result.ToString();
    }

    public static string RenderMarkdownList<T>(IEnumerable<T> rows,
                                               ObjectSchema<T> schema)
    {
        StringBuilder result = new();
        foreach (var row in rows)
        {
            var cells = schema.Fields.Select(col => col.Render(row));

            result.AppendLine(" - " + string.Join(", ", cells));
        }
        return result.ToString();
    }
}
