using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace GraniteServer.Messaging;

/// <summary>
/// Utility class for deserializing MessageBusMessage objects from JSON.
/// Shared between server and client to handle polymorphic event deserialization.
/// </summary>
public static class MessageDeserializer
{
    /// <summary>
    /// Deserializes a JSON element into the appropriate MessageBusMessage type.
    /// </summary>
    /// <param name="payload">The JSON element containing the message</param>
    /// <param name="messageType">The type to deserialize to</param>
    /// <returns>The deserialized MessageBusMessage</returns>
    public static MessageBusMessage DeserializeMessage(JsonElement payload, Type messageType)
    {
        // Configure JSON options to handle property name case-insensitivity and nested types
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        var message =
            (MessageBusMessage?)
                JsonSerializer.Deserialize(payload.GetRawText(), messageType, options)
            ?? throw new InvalidOperationException("Failed to deserialize message");

        return message;
    }

    /// <summary>
    /// Finds a MessageBusMessage type by its name across all loaded assemblies.
    /// </summary>
    /// <param name="messageType">The name of the message type to find</param>
    /// <returns>The Type if found, null otherwise</returns>
    public static Type? FindMessageTypeByName(string messageType)
    {
        return AppDomain
            .CurrentDomain.GetAssemblies()
            .SelectMany(assembly =>
            {
                try
                {
                    return assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    return ex.Types.Where(t => t != null)!;
                }
            })
            .FirstOrDefault(t =>
                t != null
                && !t.IsAbstract
                && typeof(MessageBusMessage).IsAssignableFrom(t)
                && t.Name.Equals(messageType, StringComparison.OrdinalIgnoreCase)
            );
    }
}
