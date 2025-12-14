using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace ObjectPrinting;

public class PrintingConfig<TOwner> : IPrintingConfig<TOwner>
{
    private readonly HashSet<Type> _excludedTypes = [];
    private readonly HashSet<MemberInfo> _excludedMembers = [];
    private readonly Dictionary<Type, Func<object, string>> _typeSerializers = [];
    private readonly Dictionary<MemberInfo, Func<object, string>> _memberSerializers = [];
    private readonly Dictionary<Type, CultureInfo> _typeCultures = [];
    private readonly Dictionary<MemberInfo, CultureInfo> _memberCultures = [];
    private readonly Dictionary<MemberInfo, int> _memberStringTrims = [];
    private readonly Dictionary<Type, int> _typeStringTrims = [];

    internal void SetTypeSerializer(Type type, Func<object, string> serializer)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(serializer);

        _typeSerializers[type] = serializer;
    }

    internal void SetMemberSerializer(MemberInfo member, Func<object, string> serializer)
    {
        ArgumentNullException.ThrowIfNull(member);
        ArgumentNullException.ThrowIfNull(serializer);

        _memberSerializers[member] = serializer;
    }

    internal void SetTypeCulture(Type type, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(culture);

        _typeCultures[type] = culture;
    }

    internal void SetMemberCulture(MemberInfo member, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(member);
        ArgumentNullException.ThrowIfNull(culture);

        _memberCultures[member] = culture;
    }

    internal void SetMemberStringTrim(MemberInfo member, int maxLength)
    {
        ArgumentNullException.ThrowIfNull(member);
        _memberStringTrims[member] = maxLength;
    }

    internal void SetTypeStringTrim(Type type, int maxLength)
    {
        ArgumentNullException.ThrowIfNull(type);
        _typeStringTrims[type] = maxLength;
    }

    public IPrintingConfig<TOwner> Excluding<TPropType>()
    {
        _excludedTypes.Add(typeof(TPropType));
        return this;
    }

    public IPrintingConfig<TOwner> Excluding<TProp>(Expression<Func<TOwner, TProp>> memberSelector)
    {
        ArgumentNullException.ThrowIfNull(memberSelector);

        if (memberSelector.Body is not MemberExpression expression)
            throw new ArgumentException("Выражение должно указывать на поле или свойство.", nameof(memberSelector));

        _excludedMembers.Add(expression.Member);
        return this;
    }

    public IPropertyPrintingConfig<TOwner, TProp> Printing<TProp>()
    {
        return new PropertyPrintingConfig<TOwner, TProp>(this, typeof(TProp), null!);
    }

    public IPropertyPrintingConfig<TOwner, TProp> Printing<TProp>(Expression<Func<TOwner, TProp>> memberSelector)
    {
        ArgumentNullException.ThrowIfNull(memberSelector);

        if (memberSelector.Body is not MemberExpression expression)
            throw new ArgumentException("Выражение должно указывать на поле или свойство.", nameof(memberSelector));

        return new PropertyPrintingConfig<TOwner, TProp>(this, typeof(TProp), expression.Member);
    }

    public string PrintToString(TOwner obj)
    {
        var visited = new HashSet<object>(new ReferenceEqualityComparer());
        return PrintToString(obj, 0, visited, member: null, expectedType: typeof(TOwner));
    }

    private string PrintToString(object? obj, int nestingLevel, HashSet<object> visited, MemberInfo? member, Type expectedType)
    {
        if (obj == null)
            return $"null ({expectedType.Name}){Environment.NewLine}";

        var type = obj.GetType();

        if (member != null && _memberSerializers.TryGetValue(member, out var memberSerializer))
            return memberSerializer(obj) + Environment.NewLine;

        if (_typeSerializers.TryGetValue(type, out var typeSerializer))
            return typeSerializer(obj) + Environment.NewLine;

        if (IsFinalType(type))
        {
            var text = FormatFinalValue(obj, type, member);
            return text + Environment.NewLine;
        }

        if (!visited.Add(obj))
            return "cyclic reference" + Environment.NewLine;

        var indent = new string('\t', nestingLevel + 1);
        var sb = new StringBuilder();

        sb.AppendLine(type.Name);

        foreach (var memberInfo in GetSerializableMembers(type))
        {
            if (ShouldSkipMember(memberInfo))
                continue;

            var value = GetMemberValue(obj, memberInfo);
            var memberType = GetMemberType(memberInfo);

            sb.Append(indent)
              .Append(memberInfo.Name)
              .Append(" = ")
              .Append(PrintToString(value, nestingLevel + 1, visited, memberInfo, memberType));
        }

        return sb.ToString();
    }

    private static Type GetMemberType(MemberInfo member)
    {
        return member switch
        {
            FieldInfo f => f.FieldType,
            PropertyInfo p => p.PropertyType,
            _ => typeof(object)
        };
    }

    private static IEnumerable<MemberInfo> GetSerializableMembers(Type type)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;

        foreach (var member in type.GetFields(flags))
            yield return member;

        foreach (var member in type.GetProperties(flags))
        {
            var getter = member.GetMethod;
            if (getter is null || !getter.IsPublic)
                continue;

            yield return member;
        }
    }

    private static object? GetMemberValue(object obj, MemberInfo member)
    {
        return member switch
        {
            FieldInfo field => field.GetValue(obj),
            PropertyInfo property => property.GetValue(obj),
            _ => null
        };
    }

    private static bool IsFinalType(Type type)
    {
        return type.IsPrimitive
            || type == typeof(string)
            || type == typeof(decimal)
            || type == typeof(DateTime)
            || type == typeof(TimeSpan)
            || type == typeof(Guid);
    }

    private string FormatFinalValue(object obj, Type type, MemberInfo? member)
    {
        string result;

        CultureInfo? culture = null;
        if (member != null && _memberCultures.TryGetValue(member, out var memberCulture))
            culture = memberCulture;
        else if (_typeCultures.TryGetValue(type, out var typeCulture))
            culture = typeCulture;

        if (obj is IFormattable formattable && culture != null)
            result = formattable.ToString(null, culture) ?? string.Empty;
        else
            result = obj.ToString() ?? string.Empty;

        if (type == typeof(string))
        {
            var needsTrim = false;
            var maxLength = 0;

            if (member != null && _memberStringTrims.TryGetValue(member, out var memberMax))
            {
                needsTrim = result.Length > memberMax;
                maxLength = memberMax;
            }
            else if (_typeStringTrims.TryGetValue(type, out var typeMax))
            {
                needsTrim = result.Length > typeMax;
                maxLength = typeMax;
            }

            if (needsTrim)
                result = result[..maxLength];
        }

        return result;
    }

    private bool ShouldSkipMember(MemberInfo member)
    {
        if (_excludedMembers.Contains(member))
            return true;

        var type = member switch
        {
            PropertyInfo p => p.PropertyType,
            FieldInfo f => f.FieldType,
            _ => null
        };

        return type != null && _excludedTypes.Contains(type);
    }
}
