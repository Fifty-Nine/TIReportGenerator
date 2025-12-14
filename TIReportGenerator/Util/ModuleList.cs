using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
};