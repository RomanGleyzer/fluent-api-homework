using System;
using System.Globalization;
using System.Reflection;

namespace ObjectPrinting;

public class PropertyPrintingConfig<TOwner, TProp>(PrintingConfig<TOwner> printingConfig, MemberInfo? memberInfo)
{
    private readonly PrintingConfig<TOwner> _parentConfig = printingConfig;
    private readonly MemberInfo? _memberInfo = memberInfo;

    public PrintingConfig<TOwner> Using(Func<TProp, string> serializer)
    {
        /*
         * Если _memberInfo null, тогда работаем со всеми значениями типа TProp
         * Иначе только с конкретным свойством
         */

        return _parentConfig;
    }

    public PrintingConfig<TOwner> Using(CultureInfo c)
    {
        /*
         * Если _memberInfo null, тогда работаем со всеми значениями типа TProp
         * Иначе только с конкретным свойством
         */

        return _parentConfig;
    }

    public PrintingConfig<TOwner> TrimmedToLength(int len)
    {
        /*
         * Если _memberInfo null, тогда устанавливаем длину для обычной строки
         * Иначе только для конткретного свойства
         */

        return _parentConfig;
    }
}
