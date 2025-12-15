using System;
using System.Text.RegularExpressions;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace TIReportGenerator.Util
{
    internal static class SIFormatter
    {
        private static readonly string[] SizeSuffixes =
        [
            "f", "p", "n", "µ", "m", null, "k", "M", "G", "T", "P", "E"
        ];

        private static readonly int ZeroOffset = SizeSuffixes.IndexOf(null);

        private static double ScaleFactor(string suffix)
        {
            int idx = SizeSuffixes.IndexOf(suffix);
            if (idx == -1) throw new ArgumentException("Unrecognized SI prefix");

            return Math.Pow(1000, idx - ZeroOffset);
        }

        public static (string, string) ToStringWithSuffix(double value, bool allowNegativeExp)
        {
            if (value == 0.0) return ("0", null);

            double mag = Math.Abs(value);
            int exp = (int)Math.Floor(Math.Log10(mag) / 3);

            int idx = exp + ZeroOffset;

            if (idx < ZeroOffset && !allowNegativeExp) {
                idx = ZeroOffset;
                exp = 0;
            }

            if (idx < 0)
            {
                exp = -ZeroOffset;
                idx = 0;
            }
            else if (idx >= SizeSuffixes.Length)
            {
                exp = SizeSuffixes.Length - ZeroOffset - 1;
                idx = SizeSuffixes.Length - 1;
            }

            double scaled = mag * Math.Pow(1000, -exp);
            if (scaled > 1 && !allowNegativeExp) scaled = Math.Round(scaled, 1);
            if (scaled >= 1e3 && (idx < (SizeSuffixes.Length - 1)))
            {
                scaled /= 1e3;
                idx += 1;
            }

            var sign = value < 0 ? "-" : "";
            var suffix = SizeSuffixes[idx];
            return ($"{sign}{scaled:0.#}", suffix);
        }

        public static string ToString(double value, bool allowNegativeExp = false)
        {
            var (numText, suffix) = ToStringWithSuffix(value, allowNegativeExp);
            return numText + (suffix != null ? $" {suffix}" : "");
        }

        private static readonly Regex NumericRegex = new(@"(\+|-)?([0-9]+(\.[0-9]+)?)\s*([a-zµA-Z])");

        public static (double Value, int LastIdx) FromString(string text)
        {
            text = text.Trim();
            var match = NumericRegex.Match(text);

            if (!match.Success)
            {
                throw new FormatException("Numeric text did not match the expected format.");
            }

            var sign = match.Captures[1].Value;
            var numeric = match.Captures[2].Value;
            var suffix = match.Captures[4].Value;

            return (
                (sign == "-" ? -1.0 : 1.0) * double.Parse(numeric) * ScaleFactor(suffix),
                match.Length
            );
        }
    }

    public class SIConverter : IYamlTypeConverter
    {
        public bool Accepts(Type t)
        {
            return t == typeof(float);
        }

        public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
        {
            var scalar = (Scalar)parser.Current;
            parser.MoveNext();

            var (value, idx) = SIFormatter.FromString(scalar.Value);
            if (idx != scalar.Value.Length) {
                throw new FormatException("Unexpected extra data after end of number.");
            }

            return (float)value;
        }

        public void WriteYaml(IEmitter emitter, object value, Type type, ObjectSerializer serializer)
        {
            emitter.Emit(new Scalar(SIFormatter.ToString((float)value)));
        }
    };
}