using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace ObjectPrinting;

public class PrintingConfig<TOwner>
{
    private readonly HashSet<Type> _excludedTypes = [];
    private readonly HashSet<MemberInfo> _excludedMembers = [];
    private readonly Dictionary<Type, Func<object, string>> _typeSerializers = [];
    private readonly Dictionary<MemberInfo, Func<object, string>> _memberSerializers = [];

    public PrintingConfig<TOwner> Excluding<TPropType>()
    {
        _excludedTypes.Add(typeof(TPropType));
        return this;
    }

    public PrintingConfig<TOwner> Excluding<TProp>(Expression<Func<TOwner, TProp>> memberSelector)
    {
        ArgumentNullException.ThrowIfNull(memberSelector);

        if (memberSelector.Body is not MemberExpression expression)
            throw new ArgumentException(null, nameof(memberSelector));

        _excludedMembers.Add(expression.Member);
        return this;
    }

    public PropertyPrintingConfig<TOwner, TProp> Printing<TProp>()
    {
        return new PropertyPrintingConfig<TOwner, TProp>(this, null);
    }

    public PropertyPrintingConfig<TOwner, TProp> Printing<TProp>(Expression<Func<TOwner, TProp>> memberSelector)
    {
        ArgumentNullException.ThrowIfNull(memberSelector);

        if (memberSelector.Body is not MemberExpression expression)
            throw new ArgumentException(null, nameof(memberSelector));

        return new PropertyPrintingConfig<TOwner, TProp>(this, expression.Member);
    }

    public string PrintToString(TOwner obj)
    {
        var visited = new HashSet<object>();
        return PrintToString(obj, 0, visited, null);
    }

    private string PrintToString(object obj, int nestingLevel, HashSet<object> visited, MemberInfo? member)
    {
        if (obj == null)
            return "null" + Environment.NewLine;

        var type = obj.GetType();

        if (!IsFinalType(type))
        {
            if (visited.Contains(type))
                return "Cycle ref" + Environment.NewLine;

            visited.Add(obj);
        }

        if (member != null && _memberSerializers.TryGetValue(type, out var memberSerializer))
            return memberSerializer(obj) + Environment.NewLine;

        if (_typeSerializers.TryGetValue(type, out var typeSerializer))
            return typeSerializer(obj) + Environment.NewLine;

        if (IsFinalType(type))
            return obj + Environment.NewLine;

        var identation = new string('\t', nestingLevel + 1);
        var sb = new StringBuilder();

        sb.AppendLine(type.Name);

        foreach (var propertyInfo in type.GetProperties())
        {
            if (ShouldSkipMember(propertyInfo))
                continue;

            var val = propertyInfo.GetValue(obj);
            sb.Append(
                identation 
                + propertyInfo.Name 
                + " = " 
                + PrintToString(val, nestingLevel + 1, visited, propertyInfo)
                );
        }

        return sb.ToString();
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

        if (type == null)
            return false;

        if (_excludedTypes.Contains(type))
            return true;

        return false;
    }
}