using LanguageExt;
using RVis.Base;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;

namespace RVisUI.Model.Extensions
{
  public static class FxExt
  {
    public static Action<T> SafeInvoke<T>(this Action<T> action, [CallerMemberName] string subject = "") =>
      t =>
      {
        try
        {
          action(t);
        }
        catch (Exception ex)
        {
          Logger.Log.Error(ex, subject);
        }
      };

    public static bool RaiseAndSetIfChanged<T, U>(
      this T @this,
      ref U backingField,
      U newValue,
      PropertyChangedEventHandler? propertyChangedEventHandler,
      [CallerMemberName] string propertyName = ""
      ) where T : INotifyPropertyChanged
    {
      var changed = !EqualityComparer<U>.Default.Equals(backingField, newValue);
      if (changed)
      {
        backingField = newValue;
        propertyChangedEventHandler?.Invoke(@this, new PropertyChangedEventArgs(propertyName));
      }
      return changed;
    }

    public static bool RaiseSetAndObserveIfChanged<T, U, V>(
      this T @this,
      ref Arr<U> backingField,
      Arr<U> newValue,
      PropertyChangedEventHandler? propertyChangedEventHandler,
      ISubject<(Arr<U> Us, ObservableQualifier ObservableQualifier)> changesSubject,
      Func<U, V> getID,
      [CallerMemberName] string propertyName = ""
      )
      where T : INotifyPropertyChanged
      where U : IEquatable<U>
      where V : IEquatable<V>
    {
      if (backingField == newValue) return false;

      var oldValue = backingField;
      var uEqualityComparer = EqualityComparer<U>.Default;
      var vEqualityComparer = EqualityComparer<V>.Default;

      var added = newValue.Filter(nv => !oldValue.Exists(ov => vEqualityComparer.Equals(getID(ov), getID(nv))));
      var removed = oldValue.Filter(ov => !newValue.Exists(nv => vEqualityComparer.Equals(getID(nv), getID(ov))));
      var changed = newValue.Filter(nv => oldValue.Exists(ov => vEqualityComparer.Equals(getID(nv), getID(ov)) && !uEqualityComparer.Equals(nv, ov)));

      backingField = newValue;
      propertyChangedEventHandler?.Invoke(@this, new PropertyChangedEventArgs(propertyName));

      if (!added.IsEmpty)
      {
        changesSubject.OnNext((added, ObservableQualifier.Add));
      }

      if (!removed.IsEmpty)
      {
        changesSubject.OnNext((removed, ObservableQualifier.Remove));
      }

      if (!changed.IsEmpty)
      {
        changesSubject.OnNext((changed, ObservableQualifier.Change));
      }

      return true;
    }

    public static IObservable<string?> GetWhenPropertyChanged(this INotifyPropertyChanged inpc) =>
      Observable
        .FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
          h => inpc.PropertyChanged += h,
          h => inpc.PropertyChanged -= h
          )
        .Select(x => x.EventArgs.PropertyName);
  }
}
