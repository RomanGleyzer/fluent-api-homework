using System;
using System.Globalization;

namespace ObjectPrinting;

public interface IPropertyPrintingConfig<TOwner, TProp>
{
    IPrintingConfig<TOwner> Using(Func<TProp, string> serializer);

    IPrintingConfig<TOwner> Using(CultureInfo culture);

    IPrintingConfig<TOwner> TrimmedToLength(int maxLength);
}
