using System;
using System.Collections.Generic;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace TIReportGenerator.Util
{
    public static class UnitDefs
    {
        public class UnitDefinition(Protos.Unit unit, string abbrev)
        {
            public Protos.Unit Unit => unit;
            public string Abbreviation => abbrev;
        };

        public static readonly List<UnitDefinition> Units =
        [
            new(Protos.Unit.Meter, "m"),
            new(Protos.Unit.Gram, "g"),
            new(Protos.Unit.MetersPerSecond, "m/s"),
            new(Protos.Unit.Watt, "W"),
            new(Protos.Unit.Joule, "J"),
            new(Protos.Unit.Ton, "ton"),
            new(Protos.Unit.DegPerSecondSquared, "°/s²"),
            new(Protos.Unit.Gee, "gee"),
            new(Protos.Unit.Day, "day")
        ];

        public static string ToAbbreviation(this Protos.Unit unit)
        {
            return Units.Find(ud => ud.Unit == unit).Abbreviation;
        }

        public static Protos.Unit FromAbbreviation(string abbrev)
        {
            return Units.Find(ud => ud.Abbreviation == abbrev.Trim()).Unit;
        }
    };

    public static class Quantity
    {
        public static Protos.Quantity Get(float value, Protos.Unit unit)
        {
            return new Protos.Quantity {
                Value = value,
                Units = unit
            };
        }

        public static Protos.Quantity Get(float value, string abbrev)
        {
            return Get(value, UnitDefs.FromAbbreviation(abbrev));
        }
    };

    public class QuantityConverter : IYamlTypeConverter
    {
        public bool Accepts(Type t)
        {
            return t == typeof(Protos.Quantity);
        }

        public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
        {
            var scalar = (Scalar)parser.Current;
            parser.MoveNext();

            var (value, idx) = SIFormatter.FromString(scalar.Value);
            var unit = UnitDefs.FromAbbreviation(scalar.Value.Substring(idx));

            return Quantity.Get((float)value, unit);
        }

        public void WriteYaml(IEmitter emitter, object value, Type type, ObjectSerializer serializer)
        {
            var q = (Protos.Quantity)value;
            emitter.Emit(new Scalar($"{SIFormatter.ToString(q.Value)}{q.Units.ToAbbreviation()}"));
        }
    };
}