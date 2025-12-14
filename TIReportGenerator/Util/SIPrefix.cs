using System;
using System.Linq;
using System.Text.RegularExpressions;
namespace TIReportGenerator.Util
{
    internal static class SIFormatter
    {
        private static readonly string[] SizeSuffixes =
        [
            "f", "p", "n", "µ", "m", "", "k", "M", "G", "T", "P", "E"
        ];
        
        private static readonly int ZeroOffset = SizeSuffixes.IndexOf("");
        
        private static double ScaleFactor(string suffix)
        {
            int idx = SizeSuffixes.IndexOf(suffix);
            if (idx == -1) throw new ArgumentException("Unrecognized SI prefix");
            
            return Math.Pow(1000, idx - ZeroOffset);
        }
        
        public static string ToString(double value)
        {
            if (value == 0.0) return "0";
            
            double mag = Math.Abs(value);
            int exp = (int)Math.Floor(Math.Log10(mag) / 3);
            
            int idx = exp + ZeroOffset;
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
            
            double scaled = Math.Round(mag * Math.Pow(1000, -exp), 1); 
            if (scaled >= 1e3 && (idx < (SizeSuffixes.Length - 1)))
            {
                scaled /= 1e3;
                exp += 1;
                idx += 1;
            }
            
            var sign = value < 0 ? "-" : "";
            return $"{sign}{scaled:0.#} {SizeSuffixes[idx]}";
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
}