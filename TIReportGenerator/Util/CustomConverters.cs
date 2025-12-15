using System;
using System.Text.RegularExpressions;
using TIReportGenerator.Protos;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace TIReportGenerator.Util
{
    public class ModuleListConverter : IYamlTypeConverter
    {
        private static readonly Regex MultiplierRegex = new(@"^([0-9]+)x\s+(.*)$");

        private (string, int) ParseCount(string text)
        {
            var match = MultiplierRegex.Match(text);
            if (!match.Success) throw new FormatException("Unrecognized ModuleList entry format.");

            return (match.Captures[2].Value, int.Parse(match.Captures[1].Value));
        }

        public bool Accepts(Type t)
        {
            return t == typeof(Protos.ModuleList);
        }


        public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
        {
            Protos.ModuleList result = new();
            parser.Consume<SequenceStart>();

            while (!parser.Accept<SequenceEnd>(out _))
            {
                var (name, count) = ParseCount(parser.Consume<Scalar>().Value);
                result.Modules.Add(name, count);
            }
            parser.Consume<SequenceEnd>();
            return result;
        }

        public void WriteYaml(IEmitter emitter, object value, Type type, ObjectSerializer serializer)
        {
            var modules = (Protos.ModuleList)value;

            emitter.Emit(new SequenceStart(anchor: null, tag: null, isImplicit: false, style: SequenceStyle.Any));

            foreach (var kvp in modules.Modules)
            {
                emitter.Emit(new Scalar($"{kvp.Value}x {kvp.Key}"));
            }

            emitter.Emit(new SequenceEnd());
        }
    };

    public class CapacityUseConverter : IYamlTypeConverter
    {
        private static readonly Regex UsageCapRegex = new(@"^([0-9]+)\s*/\s*([0-9]+)$");
        private (uint, uint) ParseCap(string text)
        {
            var match = UsageCapRegex.Match(text.Trim());
            if (!match.Success) throw new FormatException("Unrecognized capacity usage format.");

            return (uint.Parse(match.Captures[1].Value), uint.Parse(match.Captures[2].Value));
        }

        public bool Accepts(Type t)
        {
            return t == typeof(Protos.CapacityUse);
        }

        public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
        {
            var scalar = (Scalar)parser.Current;
            parser.MoveNext();

            var (use, cap) = ParseCap(scalar.Value);

            return new Protos.CapacityUse
            {
                Usage = use,
                Capacity = cap
            };
        }

        public void WriteYaml(IEmitter emitter, object value, Type type, ObjectSerializer serializer)
        {
            var cap = (Protos.CapacityUse)value;
            emitter.Emit(new Scalar($"{cap.Usage}/{cap.Capacity}"));
        }
    };

    public class PercentageConverter : IYamlTypeConverter
    {
        private static readonly Regex PercentageRegex = new(@"((\+|-)?([0-9]+)(\.[0-9]+))\s*%");

        public bool Accepts(Type type)
        {
            return type == typeof(Protos.Percentage);
        }

        public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
        {
            var scalar = (Scalar)parser.Current;
            parser.MoveNext();

            var match = PercentageRegex.Match(scalar.Value);
            if (!match.Success) throw new FormatException("Unexpected format for percentage value.");

            return new Protos.Percentage
            {
                Value = float.Parse(match.Captures[1].Value) / 100.0f
            };
        }

        public void WriteYaml(IEmitter emitter, object value, Type type, ObjectSerializer serializer)
        {
            var p = (Protos.Percentage)value;
            emitter.Emit(new Scalar($"{p.Value:P1}"));
        }
    }

    public class ResearchProgressConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type)
        {
            return type == typeof(Protos.ResearchProgress);
        }

        public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
        {
            throw new NotImplementedException();
        }

        public void WriteYaml(IEmitter emitter, object value, Type type, ObjectSerializer serializer)
        {
            var progress = (Protos.ResearchProgress)value;
            emitter.Emit(new Scalar($"{SIFormatter.ToString(progress.Progress)}/{SIFormatter.ToString(progress.Cost)}"));
        }
    };

    public class ResourceYieldConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type)
        {
            return type == typeof(Protos.ResourceYieldData);
        }

        public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
        {
            throw new NotImplementedException();
        }

        public void WriteYaml(IEmitter emitter, object value, Type type, ObjectSerializer serializer)
        {
            var yield = value as ResourceYieldData;
            emitter.Emit(new Scalar(yield.HasYield ? $"{yield.Yield:F1} ({yield.Grade})" : yield.Grade));
        }
    };
}