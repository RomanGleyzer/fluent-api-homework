using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
    private readonly Dictionary<Type, CultureInfo> _typeCultures = [];
    private readonly Dictionary<MemberInfo, CultureInfo> _memberCultures = [];
    private readonly Dictionary<MemberInfo, int> _memberStringMaxLength = [];
    private readonly int? _defaultStringMaxLength;

    public PrintingConfig<TOwner> Excluding<TPropType>()
    {
        return this;
    }

    public PrintingConfig<TOwner> Excluding<TProp>(Expression<Func<TOwner, TProp>> memberSelector)
    {
        return this;
    }

    public PropertyPrintingConfig<TOwner, TProp> Printing<TProp>()
    {
        return new PropertyPrintingConfig<TOwner, TProp>(this, null);
    }

    public PropertyPrintingConfig<TOwner, TProp> Printing<TProp>(Expression<Func<TOwner, TProp>> memberSelector)
    {
        return null!;
        // return new PropertyPrintingConfig<TOwner, TProp>(this, /*MemberInfo объект*/);
    }

    public string PrintToString(TOwner obj)
    {
        return PrintToString(obj, 0);
    }

    private string PrintToString(object obj, int nestingLevel)
    {
        //TODO apply configurations
        if (obj == null)
            return "null" + Environment.NewLine;

        var finalTypes = new[]
        {
            typeof(int), typeof(double), typeof(float), typeof(string),
            typeof(DateTime), typeof(TimeSpan)
        };
        if (finalTypes.Contains(obj.GetType()))
            return obj + Environment.NewLine;

        var identation = new string('\t', nestingLevel + 1);
        var sb = new StringBuilder();
        var type = obj.GetType();
        sb.AppendLine(type.Name);
        foreach (var propertyInfo in type.GetProperties())
        {
            sb.Append(identation + propertyInfo.Name + " = " +
                      PrintToString(propertyInfo.GetValue(obj),
                          nestingLevel + 1));
        }
        return sb.ToString();
    }
}