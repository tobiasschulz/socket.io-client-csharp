using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions.Equivalency;
using SocketIO.Core;
using SocketIO.Serializer.Core;
using SocketIO.Serializer.SystemTextJson.Tests.Models;

namespace SocketIO.Serializer.SystemTextJson.Tests;

public class SystemTextJsonSerializerTests
{
    private static IEnumerable<(
            string eventName,
            string ns,
            EngineIO eio,
            object[] data,
            IEnumerable<SerializedItem> expectedItems)>
        SerializeTupleCases =>
        new (string eventName, string ns, EngineIO eio, object[] data, IEnumerable<SerializedItem> expectedItems)[]
        {
            (
                "test",
                string.Empty,
                EngineIO.V3,
                Array.Empty<object>(),
                new SerializedItem[]
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text = "42[\"test\"]"
                    }
                }),
            (
                "test",
                string.Empty,
                EngineIO.V3,
                new object[1],
                new SerializedItem[]
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text = "42[\"test\",null]"
                    }
                }),
            (
                "test",
                "nsp",
                EngineIO.V3,
                Array.Empty<object>(),
                new SerializedItem[]
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text = "42nsp,[\"test\"]"
                    }
                }),
            (
                "test",
                "nsp",
                EngineIO.V3,
                new object[] { true },
                new SerializedItem[]
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text = "42nsp,[\"test\",true]"
                    }
                }),
            (
                "test",
                "nsp",
                EngineIO.V3,
                new object[] { true, false, 123 },
                new SerializedItem[]
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text = "42nsp,[\"test\",true,false,123]"
                    }
                }),
            (
                "test",
                string.Empty,
                EngineIO.V3,
                new object[]
                {
                    new byte[] { 1, 2, 3 }
                },
                new SerializedItem[]
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text = "451-[\"test\",{\"_placeholder\":true,\"num\":0}]"
                    },
                    new()
                    {
                        Type = SerializedMessageType.Binary,
                        Binary = new byte[] { 1, 2, 3 }
                    }
                }),
            (
                "test",
                "nsp",
                EngineIO.V3,
                new object[]
                {
                    new byte[] { 1, 2, 3 },
                    new byte[] { 4, 5, 6 }
                },
                new SerializedItem[]
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text = "452-nsp,[\"test\",{\"_placeholder\":true,\"num\":0},{\"_placeholder\":true,\"num\":1}]"
                    },
                    new()
                    {
                        Type = SerializedMessageType.Binary,
                        Binary = new byte[] { 1, 2, 3 }
                    },
                    new()
                    {
                        Type = SerializedMessageType.Binary,
                        Binary = new byte[] { 4, 5, 6 }
                    }
                }),
            (
                "test",
                string.Empty,
                EngineIO.V4,
                new object[]
                {
                    new byte[] { 1, 2, 3 }
                },
                new SerializedItem[]
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text = "451-[\"test\",{\"_placeholder\":true,\"num\":0}]"
                    },
                    new()
                    {
                        Type = SerializedMessageType.Binary,
                        Binary = new byte[] { 1, 2, 3 }
                    }
                }),
        };

    public static IEnumerable<object[]> SerializeCases => SerializeTupleCases
        .Select((x, caseId) => new object[]
        {
            caseId,
            x.eventName,
            x.ns,
            x.eio,
            x.data,
            x.expectedItems
        });

    [Theory]
    [MemberData(nameof(SerializeCases))]
    public void Should_serialize_given_event_message(
        int caseId,
        string eventName,
        string ns,
        EngineIO eio,
        object[] data,
        IEnumerable<SerializedItem> expectedItems)
    {
        var serializer = new SystemTextJsonSerializer();
        var items = serializer.Serialize(eventName, ns, data);
        items.Should().BeEquivalentTo(expectedItems, config => config.WithStrictOrdering());
    }

    private static IEnumerable<(
            string? ns,
            EngineIO eio,
            string? auth,
            IEnumerable<KeyValuePair<string, string>> queries,
            SerializedItem? expected)>
        SerializeConnectedTupleCases =>
        new (string? ns, EngineIO eio, string? auth, IEnumerable<KeyValuePair<string, string>> queries, SerializedItem?
            expected)[]
            {
                (null, EngineIO.V3, null, null, new SerializedItem())!,
                (null, EngineIO.V3, "{\"userId\":1}", null, new SerializedItem())!,
                (null, EngineIO.V3, "{\"userId\":1}", new Dictionary<string, string>
                {
                    ["hello"] = "world"
                }, new SerializedItem())!,
                ("/test", EngineIO.V3, null, null, new SerializedItem
                {
                    Text = "40/test,"
                })!,
                ("/test", EngineIO.V3, null,
                    new Dictionary<string, string>
                    {
                        ["key"] = "value"
                    },
                    new SerializedItem
                    {
                        Text = "40/test?key=value,"
                    })!,
                (null, EngineIO.V4, null, null, new SerializedItem
                {
                    Text = "40"
                })!,
                ("/test", EngineIO.V4, null, null, new SerializedItem
                {
                    Text = "40/test,"
                })!,
                (null, EngineIO.V4, "{\"userId\":1}", null, new SerializedItem
                {
                    Text = "40{\"userId\":1}"
                })!,
                ("/test", EngineIO.V4, "{\"userId\":1}", null, new SerializedItem
                {
                    Text = "40/test,{\"userId\":1}"
                })!,
                (null, EngineIO.V4, null,
                    new Dictionary<string, string>
                    {
                        ["key"] = "value"
                    },
                    new SerializedItem
                    {
                        Text = "40"
                    })!,
            };

    public static IEnumerable<object?[]> SerializeConnectedCases => SerializeConnectedTupleCases
        .Select((x, caseId) => new object?[]
        {
            caseId,
            x.ns,
            x.eio,
            x.auth,
            x.queries,
            x.expected
        });

    [Theory]
    [MemberData(nameof(SerializeConnectedCases))]
    public void Should_serialize_connected_messages(
        int caseId,
        string? ns,
        EngineIO eio,
        string? auth,
        IEnumerable<KeyValuePair<string, string>> queries,
        SerializedItem? expected)
    {
        var serializer = new SystemTextJsonSerializer();
        serializer.SerializeConnectedMessage(ns, eio, auth, queries)
            .Should().BeEquivalentTo(expected);
    }

    private static IEnumerable<(EngineIO eio, string? text, object? expected)> DeserializeEioTextTupleCases =>
        new (EngineIO eio, string? text, object? expected)[]
        {
            (EngineIO.V3, string.Empty, null),
            (EngineIO.V3, "hello", null),
            (EngineIO.V4, "2", new { Type = MessageType.Ping }),
            (EngineIO.V4, "3", new { Type = MessageType.Pong }),
            (
                EngineIO.V4,
                "0{\"sid\":\"wOuAvDB9Jj6yE0VrAL8N\",\"upgrades\":[\"websocket\"],\"pingInterval\":25000,\"pingTimeout\":30000}",
                new
                {
                    Type = MessageType.Opened,
                    Sid = "wOuAvDB9Jj6yE0VrAL8N",
                    PingInterval = 25000,
                    PingTimeout = 30000,
                    Upgrades = new List<string> { "websocket" }
                }),
            (EngineIO.V3, "40", new { Type = MessageType.Connected }),
            (EngineIO.V3, "40/test,", new { Type = MessageType.Connected, Namespace = "/test" }),
            (
                EngineIO.V3,
                "40/test?token=eio3,",
                new
                {
                    Type = MessageType.Connected,
                    Namespace = "/test"
                }),
            (
                EngineIO.V4,
                "40{\"sid\":\"aMA_EmVTuzpgR16PAc4w\"}",
                new
                {
                    Type = MessageType.Connected,
                    Sid = "aMA_EmVTuzpgR16PAc4w"
                }),
            (
                EngineIO.V4,
                "40/test,{\"sid\":\"aMA_EmVTuzpgR16PAc4w\"}",
                new
                {
                    Type = MessageType.Connected,
                    Sid = "aMA_EmVTuzpgR16PAc4w",
                    Namespace = "/test"
                }),
            (EngineIO.V4, "41", new { Type = MessageType.Disconnected }),
            (EngineIO.V4, "41/test,", new { Type = MessageType.Disconnected, Namespace = "/test" }),
            (
                EngineIO.V4,
                "42[\"hi\",\"V3: onAny\"]",
                new
                {
                    Type = MessageType.Event,
                    Event = "hi",
                    JsonArray = JsonNode.Parse("[\"V3: onAny\"]")!.AsArray()
                }),
            (
                EngineIO.V4,
                "42/test,[\"hi\",\"V3: onAny\"]",
                new
                {
                    Type = MessageType.Event,
                    Event = "hi",
                    Namespace = "/test",
                    JsonArray = JsonNode.Parse("[\"V3: onAny\"]")!.AsArray()
                }),
            (
                EngineIO.V4,
                "42/test,17[\"cool\"]",
                new
                {
                    Type = MessageType.Event,
                    Id = 17,
                    Namespace = "/test",
                    Event = "cool",
                    JsonArray = JsonNode.Parse("[]")!.AsArray()
                }),
            (
                EngineIO.V4,
                "431[\"nice\"]",
                new
                {
                    Type = MessageType.Ack,
                    Id = 1,
                    JsonArray = JsonNode.Parse("[\"nice\"]")!.AsArray()
                }),
            (
                EngineIO.V4,
                "43/test,1[\"nice\"]",
                new
                {
                    Type = MessageType.Ack,
                    Id = 1,
                    Namespace = "/test",
                    JsonArray = JsonNode.Parse("[\"nice\"]")!.AsArray()
                }),
            (
                EngineIO.V3,
                "44\"Authentication error2\"",
                new
                {
                    Type = MessageType.Error,
                    Error = "Authentication error2"
                }),
            (
                EngineIO.V4,
                "44{\"message\":\"Authentication error2\"}",
                new
                {
                    Type = MessageType.Error,
                    Error = "Authentication error2"
                }),
            (
                EngineIO.V4,
                "44/test,{\"message\":\"Authentication error2\"}",
                new
                {
                    Type = MessageType.Error,
                    Namespace = "/test",
                    Error = "Authentication error2"
                }),
            (
                EngineIO.V3,
                "451-[\"1 params\",{\"_placeholder\":true,\"num\":0}]",
                new
                {
                    Type = MessageType.Binary,
                    BinaryCount = 1,
                    Event = "1 params",
                    JsonArray = JsonNode.Parse("[{\"_placeholder\":true,\"num\":0}]")!.AsArray()
                }),
            (
                EngineIO.V4,
                "451-[\"1 params\",{\"_placeholder\":true,\"num\":0}]",
                new
                {
                    Type = MessageType.Binary,
                    BinaryCount = 1,
                    Event = "1 params",
                    JsonArray = JsonNode.Parse("[{\"_placeholder\":true,\"num\":0}]")!.AsArray()
                }),
            (
                EngineIO.V3,
                "451-/test,[\"1 params\",{\"_placeholder\":true,\"num\":0}]",
                new
                {
                    Type = MessageType.Binary,
                    Namespace = "/test",
                    BinaryCount = 1,
                    Event = "1 params",
                    JsonArray = JsonNode.Parse("[{\"_placeholder\":true,\"num\":0}]")!.AsArray()
                }),
            (
                EngineIO.V4,
                "451-/test,[\"1 params\",{\"_placeholder\":true,\"num\":0}]",
                new
                {
                    Type = MessageType.Binary,
                    Namespace = "/test",
                    BinaryCount = 1,
                    Event = "1 params",
                    JsonArray = JsonNode.Parse("[{\"_placeholder\":true,\"num\":0}]")!.AsArray()
                }),
            (
                EngineIO.V3,
                "451-30[\"1 params\",{\"_placeholder\":true,\"num\":0}]",
                new
                {
                    Type = MessageType.Binary,
                    BinaryCount = 1,
                    Id = 30,
                    Event = "1 params",
                    JsonArray = JsonNode.Parse("[{\"_placeholder\":true,\"num\":0}]")!.AsArray()
                }),
            (
                EngineIO.V4,
                "451-30[\"1 params\",{\"_placeholder\":true,\"num\":0}]",
                new
                {
                    Type = MessageType.Binary,
                    BinaryCount = 1,
                    Id = 30,
                    Event = "1 params",
                    JsonArray = JsonNode.Parse("[{\"_placeholder\":true,\"num\":0}]")!.AsArray()
                }),
            (
                EngineIO.V4,
                "451-/test,30[\"1 params\",{\"_placeholder\":true,\"num\":0}]",
                new
                {
                    Type = MessageType.Binary,
                    Namespace = "/test",
                    BinaryCount = 1,
                    Id = 30,
                    Event = "1 params",
                    JsonArray = JsonNode.Parse("[{\"_placeholder\":true,\"num\":0}]")!.AsArray()
                }),
            (
                EngineIO.V4,
                "461-6[{\"_placeholder\":true,\"num\":0}]",
                new
                {
                    Type = MessageType.BinaryAck,
                    BinaryCount = 1,
                    Id = 6,
                    JsonArray = JsonNode.Parse("[{\"_placeholder\":true,\"num\":0}]")!.AsArray()
                }),
            (
                EngineIO.V4,
                "461-/test,6[{\"_placeholder\":true,\"num\":0}]",
                new
                {
                    Type = MessageType.BinaryAck,
                    BinaryCount = 1,
                    Namespace = "/test",
                    Id = 6,
                    JsonArray = JsonNode.Parse("[{\"_placeholder\":true,\"num\":0}]")!.AsArray()
                }),
        };

    public static IEnumerable<object?[]> DeserializeEioTextCases => DeserializeEioTextTupleCases
        .Select((x, caseId) => new[]
        {
            caseId,
            x.eio,
            x.text,
            x.expected
        });

    [Theory]
    [MemberData(nameof(DeserializeEioTextCases))]
    public void Should_deserialize_eio_and_text(int caseId, EngineIO eio, string? text, object? expected)
    {
        var excludingProps = new[]
        {
            nameof(JsonMessage.JsonArray)
        };
        var serializer = new SystemTextJsonSerializer();
        serializer.Deserialize(eio, text)
            .Should().BeEquivalentTo(expected, options => options
                .Using<JsonArray>(x => x.Subject.ToJsonString().Should().Be(x.Expectation.ToJsonString()))
                .WhenTypeIs<JsonArray>());
    }

    private static IEnumerable<(
        IMessage2 message,
        int index,
        JsonSerializerOptions options,
        object expected)> DeserializeGenericMethodTupleCases =>
        new (IMessage2 message, int index, JsonSerializerOptions options, object expected)[]
        {
            (new JsonMessage(MessageType.Event)
            {
                ReceivedText = "[\"event\",1]"
            }, 0, null, 1)!,
            (new JsonMessage(MessageType.Event)
            {
                ReceivedText = "[\"event\",\"hello\"]"
            }, 0, null, "hello")!,
            (
                new JsonMessage(MessageType.Event)
                {
                    ReceivedText = "[\"event\",\"hello\",{\"user\":\"admin\",\"password\":\"test\"}]"
                }, 1, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                },
                new UserPasswordDto
                {
                    User = "admin",
                    Password = "test"
                })!,
            (
                new JsonMessage(MessageType.Event)
                {
                    ReceivedText =
                        "[\"event\",{\"_placeholder\":true,\"num\":0},{\"size\":2023,\"name\":\"test.txt\",\"bytes\":{\"_placeholder\":true,\"num\":1}}]",
                    ReceivedBinary = new List<byte[]> { "hello world!"u8.ToArray(), "🐮🍺"u8.ToArray() }
                }, 0, null, "hello world!"u8.ToArray())!,
            (
                new JsonMessage(MessageType.Event)
                {
                    ReceivedText =
                        "[\"event\",{\"_placeholder\":true,\"num\":0},{\"size\":2023,\"name\":\"test.txt\",\"bytes\":{\"_placeholder\":true,\"num\":1}}]",
                    ReceivedBinary = new List<byte[]> { "hello world!"u8.ToArray(), "🐮🍺"u8.ToArray() }
                }, 1, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }, new FileDto
                {
                    Size = 2023,
                    Name = "test.txt",
                    Bytes = "🐮🍺"u8.ToArray()
                })!,
        };

    public static IEnumerable<object?[]> DeserializeGenericMethodCases => DeserializeGenericMethodTupleCases
        .Select((x, caseId) => new[]
        {
            caseId,
            x.message,
            x.index,
            x.options,
            x.expected
        });

    [Theory]
    [MemberData(nameof(DeserializeGenericMethodCases))]
    public void Should_deserialize_generic_type_by_message_and_index(
        int caseId,
        IMessage2 message,
        int index,
        JsonSerializerOptions options,
        object expected)
    {
        var serializer = new SystemTextJsonSerializer(options);
        var actual = serializer.GetType()
            .GetMethod(
                nameof(SystemTextJsonSerializer.Deserialize),
                BindingFlags.Public | BindingFlags.Instance,
                new[] { typeof(IMessage2), typeof(int) })!
            .MakeGenericMethod(expected.GetType())
            .Invoke(serializer, new object?[] { message, index });
        actual.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [MemberData(nameof(DeserializeGenericMethodCases))]
    public void Should_deserialize_non_generic_type_by_message_and_index_and_type(
        int caseId,
        IMessage2 message,
        int index,
        JsonSerializerOptions options,
        object expected)
    {
        var serializer = new SystemTextJsonSerializer(options);
        var actual = serializer.Deserialize(message, index, expected.GetType());
        actual.Should().BeEquivalentTo(expected);
    }

    private static IEnumerable<(
        int packetId,
        string? ns,
        object?[] data,
        List<SerializedItem> expected)> SerializePacketIdNamespaceDataTupleCases =>
        new (int packetId, string? ns, object?[] data, List<SerializedItem> expected)[]
        {
            (0, null, new object?[] { "string", 1, true, null },
                new()
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text = "430[\"string\",1,true,null]"
                    }
                })!,
            (23, "/test", new object?[] { "string", 1, true, null },
                new()
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text = "43/test,23[\"string\",1,true,null]"
                    }
                })!,
            (8964, null, new object?[]
                {
                    123456.789,
                    new UserPasswordDto
                    {
                        User = "test",
                        Password = "hello"
                    },
                    new FileDto
                    {
                        Size = 2023,
                        Name = "test.txt",
                        Bytes = "🐮🍺"u8.ToArray()
                    }
                },
                new()
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text =
                            "461-8964[123456.789,{\"User\":\"test\",\"Password\":\"hello\"},{\"Size\":2023,\"Name\":\"test.txt\",\"Bytes\":{\"_placeholder\":true,\"num\":0}}]"
                    },
                    new()
                    {
                        Type = SerializedMessageType.Binary,
                        Binary = "🐮🍺"u8.ToArray()
                    }
                })!,
            (8964, "/test", new object?[]
                {
                    123456.789,
                    new UserPasswordDto
                    {
                        User = "test",
                        Password = "hello"
                    },
                    new FileDto
                    {
                        Size = 2023,
                        Name = "test.txt",
                        Bytes = "🐮🍺"u8.ToArray()
                    }
                },
                new()
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text =
                            "461-/test,8964[123456.789,{\"User\":\"test\",\"Password\":\"hello\"},{\"Size\":2023,\"Name\":\"test.txt\",\"Bytes\":{\"_placeholder\":true,\"num\":0}}]"
                    },
                    new()
                    {
                        Type = SerializedMessageType.Binary,
                        Binary = "🐮🍺"u8.ToArray()
                    }
                })!,
        };

    public static IEnumerable<object?[]> SerializePacketIdNamespaceDataCases => SerializePacketIdNamespaceDataTupleCases
        .Select((x, caseId) => new object?[]
        {
            caseId,
            x.packetId,
            x.ns,
            x.data,
            x.expected
        });

    [Theory]
    [MemberData(nameof(SerializePacketIdNamespaceDataCases))]
    public void Should_serialize_packet_id_and_namespace_and_data(
        int caseId,
        int packetId,
        string ns,
        object[] data,
        List<SerializedItem> expected)
    {
        var serializer = new SystemTextJsonSerializer();
        serializer.Serialize(packetId, ns, data)
            .Should().BeEquivalentTo(expected);
    }

    private static IEnumerable<(
        string eventName,
        int packetId,
        string? ns,
        object?[] data,
        List<SerializedItem> expected)> SerializeEventPacketIdNamespaceDataTupleCases =>
        new (string eventName, int packetId, string? ns, object?[] data, List<SerializedItem> expected)[]
        {
            ("event", 0, null, new object?[] { "string", 1, true, null },
                new()
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text = "420[\"event\",\"string\",1,true,null]"
                    }
                })!,
            ("event", 23, "/test", new object?[] { "string", 1, true, null },
                new()
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text = "42/test,23[\"event\",\"string\",1,true,null]"
                    }
                })!,
            ("event", 8964, null, new object?[]
                {
                    123456.789,
                    new UserPasswordDto
                    {
                        User = "test",
                        Password = "hello"
                    },
                    new FileDto
                    {
                        Size = 2023,
                        Name = "test.txt",
                        Bytes = "🐮🍺"u8.ToArray()
                    }
                },
                new()
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text =
                            "451-8964[\"event\",123456.789,{\"User\":\"test\",\"Password\":\"hello\"},{\"Size\":2023,\"Name\":\"test.txt\",\"Bytes\":{\"_placeholder\":true,\"num\":0}}]"
                    },
                    new()
                    {
                        Type = SerializedMessageType.Binary,
                        Binary = "🐮🍺"u8.ToArray()
                    }
                })!,
            ("event", 8964, "/test", new object?[]
                {
                    123456.789,
                    new UserPasswordDto
                    {
                        User = "test",
                        Password = "hello"
                    },
                    new FileDto
                    {
                        Size = 2023,
                        Name = "test.txt",
                        Bytes = "🐮🍺"u8.ToArray()
                    }
                },
                new()
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text =
                            "451-/test,8964[\"event\",123456.789,{\"User\":\"test\",\"Password\":\"hello\"},{\"Size\":2023,\"Name\":\"test.txt\",\"Bytes\":{\"_placeholder\":true,\"num\":0}}]"
                    },
                    new()
                    {
                        Type = SerializedMessageType.Binary,
                        Binary = "🐮🍺"u8.ToArray()
                    }
                })!,
        };

    public static IEnumerable<object?[]> SerializeEventPacketIdNamespaceDataCases =>
        SerializeEventPacketIdNamespaceDataTupleCases
            .Select((x, caseId) => new object?[]
            {
                caseId,
                x.eventName,
                x.packetId,
                x.ns,
                x.data,
                x.expected
            });

    [Theory]
    [MemberData(nameof(SerializeEventPacketIdNamespaceDataCases))]
    public void Should_serialize_event_packet_id_and_namespace_and_data(
        int caseId,
        string eventName,
        int packetId,
        string ns,
        object[] data,
        List<SerializedItem> expected)
    {
        var serializer = new SystemTextJsonSerializer();
        serializer.Serialize(eventName, packetId, ns, data)
            .Should().BeEquivalentTo(expected);
    }

    private static IEnumerable<(
        string eventName,
        string? ns,
        object?[] data,
        List<SerializedItem> expected)> SerializeEventNamespaceDataTupleCases =>
        new (string eventName, string? ns, object?[] data, List<SerializedItem> expected)[]
        {
            ("event", null, new object?[] { "string", 1, true, null },
                new()
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text = "42[\"event\",\"string\",1,true,null]"
                    }
                })!,
            ("event", "/test", new object?[] { "string", 1, true, null },
                new()
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text = "42/test,[\"event\",\"string\",1,true,null]"
                    }
                })!,
            ("event", null, new object?[]
                {
                    123456.789,
                    new UserPasswordDto
                    {
                        User = "test",
                        Password = "hello"
                    },
                    new FileDto
                    {
                        Size = 2023,
                        Name = "test.txt",
                        Bytes = "🐮🍺"u8.ToArray()
                    }
                },
                new()
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text =
                            "451-[\"event\",123456.789,{\"User\":\"test\",\"Password\":\"hello\"},{\"Size\":2023,\"Name\":\"test.txt\",\"Bytes\":{\"_placeholder\":true,\"num\":0}}]"
                    },
                    new()
                    {
                        Type = SerializedMessageType.Binary,
                        Binary = "🐮🍺"u8.ToArray()
                    }
                })!,
            ("event", "/test", new object?[]
                {
                    123456.789,
                    new UserPasswordDto
                    {
                        User = "test",
                        Password = "hello"
                    },
                    new FileDto
                    {
                        Size = 2023,
                        Name = "test.txt",
                        Bytes = "🐮🍺"u8.ToArray()
                    }
                },
                new()
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text =
                            "451-/test,[\"event\",123456.789,{\"User\":\"test\",\"Password\":\"hello\"},{\"Size\":2023,\"Name\":\"test.txt\",\"Bytes\":{\"_placeholder\":true,\"num\":0}}]"
                    },
                    new()
                    {
                        Type = SerializedMessageType.Binary,
                        Binary = "🐮🍺"u8.ToArray()
                    }
                })!,
        };

    public static IEnumerable<object?[]> SerializeEventNamespaceDataCases =>
        SerializeEventNamespaceDataTupleCases
            .Select((x, caseId) => new object?[]
            {
                caseId,
                x.eventName,
                x.ns,
                x.data,
                x.expected
            });

    [Theory]
    [MemberData(nameof(SerializeEventNamespaceDataCases))]
    public void Should_serialize_event_and_namespace_and_data(
        int caseId,
        string eventName,
        string ns,
        object[] data,
        List<SerializedItem> expected)
    {
        var serializer = new SystemTextJsonSerializer();
        serializer.Serialize(eventName, ns, data)
            .Should().BeEquivalentTo(expected);
    }

    private static IEnumerable<(
        JsonSerializerOptions? options,
        IMessage2 message,
        string expected)> MessageToJsonTupleCases =>
        new (JsonSerializerOptions? options, IMessage2 message, string expected)[]
        {
            (
                null,
                new JsonMessage(MessageType.Event)
                {
                    ReceivedText = "[\"event\",1]"
                },
                "[1]"),
            (
                new JsonSerializerOptions
                {
                    WriteIndented = true
                },
                new JsonMessage(MessageType.Event)
                {
                    ReceivedText = "[\"event\",\"hello\",{\"user\":\"admin\",\"password\":\"test\"}]"
                },
                @"[
  ""hello"",
  {
    ""user"": ""admin"",
    ""password"": ""test""
  }
]"),
            (
                null,
                new JsonMessage(MessageType.Ack)
                {
                    ReceivedText = "[\"event\",\"hello\",{\"user\":\"admin\",\"password\":\"test\"}]"
                },
                "[\"event\",\"hello\",{\"user\":\"admin\",\"password\":\"test\"}]"),
        };

    public static IEnumerable<object?[]> MessageToJsonCases => MessageToJsonTupleCases
        .Select((x, caseId) => new object?[]
        {
            caseId,
            x.options,
            x.message,
            x.expected
        });

    [Theory]
    [MemberData(nameof(MessageToJsonCases))]
    public void Message_should_be_able_to_json(
        int caseId,
        JsonSerializerOptions? options,
        IMessage2 message,
        string expected)
    {
        var serializer = new SystemTextJsonSerializer(options);
        serializer.MessageToJson(message)
            .Should().BeEquivalentTo(expected);
    }
}