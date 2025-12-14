using System;
using System.Globalization;

namespace ObjectPrinting;

public interface IPropertyPrintingConfig<TOwner, TProp> : IPrintingConfig<TOwner>
{
    IPropertyPrintingConfig<TOwner, TProp> Using(Func<TProp, string> serializer);

    IPropertyPrintingConfig<TOwner, TProp> Using(CultureInfo culture);

    IPropertyPrintingConfig<TOwner, TProp> TrimmedToLength(int maxLength);
}