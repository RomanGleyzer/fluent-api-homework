using System;
using System.Globalization;
using System.Reflection;

namespace ObjectPrinting;

public class PropertyPrintingConfig<TOwner, TProp>(PrintingConfig<TOwner> printingConfig, Type targetType, MemberInfo? memberInfo)
{
    private readonly PrintingConfig<TOwner> _printingConfig = printingConfig;
    private readonly Type _targetType = targetType;
    private readonly MemberInfo? _memberInfo = memberInfo;

    public PrintingConfig<TOwner> Using(Func<TProp, string> serializer)
    {
        ArgumentNullException.ThrowIfNull(serializer);

        string boxed(object o) => serializer((TProp)o);

        if (_memberInfo != null)
            _printingConfig.SetMemberSerializer(_memberInfo, boxed);
        else
            _printingConfig.SetTypeSerializer(_targetType, boxed);

        return _printingConfig;
    }

    public PrintingConfig<TOwner> Using(CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);

        _printingConfig.SetTypeCulture(_targetType, culture);
        return _printingConfig;
    }

    public PrintingConfig<TOwner> TrimmedToLength(int maxLength)
    {
        if (_memberInfo != null)
            _printingConfig.SetMemberStringTrim(_memberInfo, maxLength);
        else
            _printingConfig.SetTypeStringTrim(_targetType, maxLength);

        return _printingConfig;
    }
}
