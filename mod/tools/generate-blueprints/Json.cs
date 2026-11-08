using System.Globalization;
using System.Text;

namespace SEE.Tools.GenerateBlueprints;

/// <summary>
/// Minimal ordered JSON tree. The generated files are hashed, so the exact byte layout is part of
/// the contract: property order is insertion order, indentation is two spaces, line endings are LF
/// and numbers are formatted with the invariant culture (no runtime-dependent last digits).
/// </summary>
internal abstract class JsonValue
{
    /// <summary>Appends the value at the given indentation depth.</summary>
    public abstract void WriteTo(StringBuilder builder, int indent);
}

/// <summary>JSON object with stable, insertion-ordered members.</summary>
internal sealed class JsonObject : JsonValue
{
    private readonly List<KeyValuePair<string, JsonValue>> _members = new();

    /// <summary>Reads one member by name (diagnostics and the self-verification pass).</summary>
    public bool TryGet(string name, out JsonValue value)
    {
        foreach (var pair in _members)
        {
            if (string.Equals(pair.Key, name, StringComparison.Ordinal))
            {
                value = pair.Value;
                return true;
            }
        }

        value = null!;
        return false;
    }

    /// <summary>Number of members.</summary>
    public int Count => _members.Count;

    /// <summary>Adds one member; duplicate names are rejected so a typo cannot silently win.</summary>
    public JsonObject Add(string name, JsonValue value)
    {
        if (_members.Exists(pair => string.Equals(pair.Key, name, StringComparison.Ordinal)))
        {
            throw new GeneratorException("internal: duplicate JSON member '" + name + "'");
        }

        _members.Add(new KeyValuePair<string, JsonValue>(name, value));
        return this;
    }

    /// <inheritdoc />
    public override void WriteTo(StringBuilder builder, int indent)
    {
        if (_members.Count == 0)
        {
            builder.Append("{}");
            return;
        }

        builder.Append("{\n");
        for (var index = 0; index < _members.Count; index++)
        {
            builder.Append(' ', (indent + 1) * 2);
            AppendString(builder, _members[index].Key);
            builder.Append(": ");
            _members[index].Value.WriteTo(builder, indent + 1);
            if (index < _members.Count - 1)
            {
                builder.Append(',');
            }

            builder.Append('\n');
        }

        builder.Append(' ', indent * 2);
        builder.Append('}');
    }

    internal static void AppendString(StringBuilder builder, string value)
    {
        builder.Append('"');
        foreach (var ch in value)
        {
            switch (ch)
            {
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                default:
                    if (ch < 0x20)
                    {
                        builder.Append("\\u").Append(((int)ch).ToString("x4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        builder.Append(ch);
                    }

                    break;
            }
        }

        builder.Append('"');
    }
}

/// <summary>JSON array with insertion-ordered items.</summary>
internal sealed class JsonArray : JsonValue
{
    private readonly List<JsonValue> _items = new();

    /// <summary>Appends one item.</summary>
    public JsonArray Add(JsonValue value)
    {
        _items.Add(value);
        return this;
    }

    /// <summary>Appends every item of a string sequence as JSON strings.</summary>
    public JsonArray AddStrings(IEnumerable<string> values)
    {
        foreach (var value in values)
        {
            _items.Add(new JsonString(value));
        }

        return this;
    }

    /// <summary>Read-only view of the items (diagnostics and the self-verification pass).</summary>
    public IReadOnlyList<JsonValue> Items => _items;

    /// <inheritdoc />
    public override void WriteTo(StringBuilder builder, int indent)
    {
        if (_items.Count == 0)
        {
            builder.Append("[]");
            return;
        }

        builder.Append("[\n");
        for (var index = 0; index < _items.Count; index++)
        {
            builder.Append(' ', (indent + 1) * 2);
            _items[index].WriteTo(builder, indent + 1);
            if (index < _items.Count - 1)
            {
                builder.Append(',');
            }

            builder.Append('\n');
        }

        builder.Append(' ', indent * 2);
        builder.Append(']');
    }
}

/// <summary>JSON string.</summary>
internal sealed class JsonString : JsonValue
{
    private readonly string _value;

    public JsonString(string value) => _value = value;

    /// <summary>The raw string value.</summary>
    public string Value => _value;

    /// <inheritdoc />
    public override void WriteTo(StringBuilder builder, int indent) => JsonObject.AppendString(builder, _value);
}

/// <summary>Prefixed JSON number token (already formatted with the invariant culture).</summary>
internal sealed class JsonNumber : JsonValue
{
    private readonly string _text;

    public JsonNumber(string text) => _text = text;

    /// <summary>Parses the token back to a decimal (self-verification and diagnostics).</summary>
    public decimal Value => decimal.Parse(_text, NumberStyles.Number, CultureInfo.InvariantCulture);

    /// <inheritdoc />
    public override void WriteTo(StringBuilder builder, int indent) => builder.Append(_text);
}

/// <summary>JSON boolean.</summary>
internal sealed class JsonBool : JsonValue
{
    private readonly bool _value;

    public JsonBool(bool value) => _value = value;

    /// <summary>The raw boolean value.</summary>
    public bool Value => _value;

    /// <inheritdoc />
    public override void WriteTo(StringBuilder builder, int indent) => builder.Append(_value ? "true" : "false");
}

/// <summary>Number token helpers: invariant culture, no binary-float noise, deterministic bytes.</summary>
internal static class Numbers
{
    /// <summary>Integer token.</summary>
    public static JsonNumber Integer(int value) => new(value.ToString(CultureInfo.InvariantCulture));

    /// <summary>Decimal token: trailing fractional zeros are trimmed ("0.12", "8", "1.0" stays "1").</summary>
    public static JsonNumber Decimal(decimal value) => new(TrimZeros(
        value.ToString("0.############################", CultureInfo.InvariantCulture)));

    /// <summary>Float/double token, formatted with up to nine fractional digits and trimmed.</summary>
    public static JsonNumber Approximation(double value) => new(TrimZeros(
        value.ToString("0.#########", CultureInfo.InvariantCulture)));

    /// <summary>Renders a decimal as the same token text the JSON tree would emit (diagnostics only).</summary>
    public static string DecimalText(decimal value) => TrimZeros(
        value.ToString("0.############################", CultureInfo.InvariantCulture));

    private static string TrimZeros(string text)
    {
        if (!text.Contains('.', StringComparison.Ordinal))
        {
            return text;
        }

        var trimmed = text.TrimEnd('0');
        return trimmed.EndsWith('.') ? trimmed[..^1] : trimmed;
    }
}

/// <summary>Serializes a JSON tree to the canonical text used for every generated file.</summary>
internal static class JsonText
{
    /// <summary>Renders the tree with LF endings and a trailing newline.</summary>
    public static string Serialize(JsonValue value)
    {
        var builder = new StringBuilder(4096);
        value.WriteTo(builder, 0);
        builder.Append('\n');
        return builder.ToString();
    }
}

/// <summary>Raised for any expected generator failure (structure, derivation, gate or diff).</summary>
internal sealed class GeneratorException : Exception
{
    public GeneratorException(string message)
        : base(message)
    {
    }
}
