using System;

namespace ObjectPrinting;

public static class ObjectPrintingExtensions
{
    public static string PrintToString<TOwner>(this TOwner obj)
    {
        return ObjectPrinter.For<TOwner>().PrintToString(obj);
    }

    public static string PrintToString<TOwner>(this TOwner obj, Func<PrintingConfig<TOwner>, PrintingConfig<TOwner>> config)
    {
        return config(ObjectPrinter.For<TOwner>()).PrintToString(obj);
    }
}