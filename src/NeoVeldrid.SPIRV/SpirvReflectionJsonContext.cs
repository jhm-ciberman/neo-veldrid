using System.Text.Json.Serialization;

namespace NeoVeldrid.SPIRV;

[JsonSourceGenerationOptions(IncludeFields = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(SpirvReflection))]
internal partial class SpirvReflectionJsonContext : JsonSerializerContext
{
}
