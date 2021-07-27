using Sniffer.Static.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Sniffer.Static
{
    public sealed class EveStaticDataProvider
    {
        private static EveStaticDataProvider _instance;

        private static readonly IDeserializer _deserializer;
        private static readonly ISerializer _serializer;

        static EveStaticDataProvider()
        {
            _deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            _serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
        }

        public static void Initialize()
        {
            Instance.SystemIds = LoadOrCreateLookup(Path.Combine("sde", "generated", "systemIds.yaml"), CalculateSystemIdLookup);
        }

        public static EveStaticDataProvider Instance => _instance ??= new EveStaticDataProvider();

        //public ImmutableDictionary<int, TypeID> TypeIDs => _typeIds ??= LoadYamlToImmutableDict<int, TypeID>(Path.Combine("sde", "fsd", "typeIDs.yaml"));

        private static ImmutableDictionary<TKey, TValue> LoadYamlToImmutableDict<TKey, TValue>(string path)
        {
            using TextReader reader = new StreamReader(path);

            var dict = _deserializer.Deserialize<Dictionary<TKey, TValue>>(reader);

            return dict.ToImmutableDictionary();
        }

        public ImmutableDictionary<int, string> SystemIds { get; private set; }

        private static ImmutableDictionary<TKey, TValue> LoadOrCreateLookup<TKey, TValue>(string path, Func<ImmutableDictionary<TKey, TValue>> create)
        {
            if (File.Exists(path))
            {
                return LoadYamlToImmutableDict<TKey, TValue>(path);
            }
            var lookup = create();

            Directory.CreateDirectory(Path.GetDirectoryName(path));

            using var stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write);
            using var text = new StreamWriter(stream);

            _serializer.Serialize(text, lookup);

            return lookup;
        }

        private static ImmutableDictionary<int, string> CalculateSystemIdLookup()
        {
            const int FIRST_SYSTEM_ID = 30000001;
            const int FIRST_WORMHOLE_ID = 31000001;

            var path = Path.Combine("sde", "bsd", "invNames.yaml");
            using TextReader reader = new StreamReader(path);

            var invNames = _deserializer
                .Deserialize<List<InvName>>(reader)
                .ToImmutableDictionary(k => k.ItemID, v => v.ItemName);

            var dict = new Dictionary<int, string>();

            foreach (var item in invNames)
            {
                if (item.Key >= FIRST_SYSTEM_ID && item.Key < FIRST_WORMHOLE_ID)
                {
                    dict.Add(item.Key, item.Value);
                }
                else if (item.Key >= FIRST_WORMHOLE_ID)
                {
                    break;
                }
            }

            return dict.ToImmutableDictionary();
        }

        public bool TryGetSystemIdByName(string systemName, out int systemId, out string actualSystemName)
        {
            foreach (var item in SystemIds)
            {
                if (item.Value.Equals(systemName, StringComparison.OrdinalIgnoreCase))
                {
                    systemId = item.Key;
                    actualSystemName = item.Value;
                    return true;
                }
            }

            systemId = default;
            actualSystemName = default;
            return false;
        }
    }
}
