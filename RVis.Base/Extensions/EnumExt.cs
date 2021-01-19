using LanguageExt;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using static RVis.Base.Check;
using static System.Enum;

namespace RVis.Base.Extensions
{
  public static class EnumExt
  {
    public static Arr<T> GetFlags<T>() where T : Enum
    {
      var flags = new List<T>();
      var flag = 0b0001;
      var value = (object)flag;

      while (IsDefined(typeof(T), value))
      {
        flags.Add((T)value);
        flag <<= 1;
        value = flag;
      }

      return flags.ToArr();
    }

    public static bool IsAdd(this ObservableQualifier observableQualifier) =>
      (observableQualifier & ObservableQualifier.Add) != 0;

    public static bool IsChange(this ObservableQualifier observableQualifier) =>
      (observableQualifier & ObservableQualifier.Change) != 0;

    public static bool IsRemove(this ObservableQualifier observableQualifier) =>
      (observableQualifier & ObservableQualifier.Remove) != 0;

    public static bool IsAddOrChange(this ObservableQualifier observableQualifier) =>
      (observableQualifier & (ObservableQualifier.Add | ObservableQualifier.Change)) != 0;

    public static string? GetDescription(this Enum value)
    {
      RequireNotNull(value);

      var type = value.GetType();
      var name = GetName(type, value);

      if (name.IsAString())
      {
        var field = type.GetField(name);
        if (field != default)
        {
          var customAttribute = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));
          if (customAttribute is DescriptionAttribute descriptionAttribute)
          {
            return descriptionAttribute.Description;
          }
        }
      }

      return default;
    }
  }
}
