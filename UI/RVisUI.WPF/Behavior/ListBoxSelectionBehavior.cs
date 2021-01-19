using Microsoft.Xaml.Behaviors;
using RVis.Base.Extensions;
using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace RVisUI.Wpf
{
  public class ListBoxSelectionBehavior : Behavior<ListBox>
  {
    public static readonly DependencyProperty SelectedItemsProperty =
      DependencyProperty.Register(
        nameof(SelectedItems),
        typeof(object[]),
        typeof(ListBoxSelectionBehavior),
        new FrameworkPropertyMetadata(
          null,
          FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
          HandleSelectedItemsChanged
          )
        );

    public object[]? SelectedItems
    {
      get => (object[]?)GetValue(SelectedItemsProperty);
      set => SetValue(SelectedItemsProperty, value);
    }

    public static readonly DependencyProperty SelectedValuesProperty =
      DependencyProperty.Register(
        nameof(SelectedValues),
        typeof(IList),
        typeof(ListBoxSelectionBehavior),
        new FrameworkPropertyMetadata(
          null,
          FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
          HandleSelectedValuesChanged
          )
        );

    public IList? SelectedValues
    {
      get => (IList?)GetValue(SelectedValuesProperty);
      set => SetValue(SelectedValuesProperty, value);
    }

    protected override void OnAttached()
    {
      base.OnAttached();

      AssociatedObject.SelectionChanged += HandleSelectionChanged;
      ((INotifyCollectionChanged)AssociatedObject.Items).CollectionChanged += HandleCollectionChanged;

      _modelHandled = true;
      SelectedValuesToItems();
      SelectItems();
      _modelHandled = false;
    }

    protected override void OnDetaching()
    {
      base.OnDetaching();

      if (AssociatedObject != null)
      {
        AssociatedObject.SelectionChanged -= HandleSelectionChanged;
        ((INotifyCollectionChanged)AssociatedObject.Items).CollectionChanged -= HandleCollectionChanged;
      }
    }

    private static void HandleSelectedItemsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
      if (sender is not ListBoxSelectionBehavior behavior) return;
      if (behavior._modelHandled) return;
      if (behavior.AssociatedObject == null) return;

      behavior._modelHandled = true;
      behavior.SelectedItemsToValues();
      behavior.SelectItems();
      behavior._modelHandled = false;
    }

    private static void HandleSelectedValuesChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
      if (sender is not ListBoxSelectionBehavior behavior) return;
      if (behavior._modelHandled) return;
      if (behavior.AssociatedObject == null) return;

      behavior._modelHandled = true;
      behavior.SelectedValuesToItems();
      behavior.SelectItems();
      behavior._modelHandled = false;
    }

    private static object? GetDeepPropertyValue(object obj, string path)
    {
      if (path.IsntAString()) return obj;

      var propertyInfo = GetDeepPropertyInfo(obj, path);
      return propertyInfo.GetValue(obj, null);
    }

    private static PropertyInfo GetDeepPropertyInfo(object obj, string path)
    {
      while (true)
      {
        if (path.Contains('.'))
        {
          string[] split = path.Split('.');
          string remainingProperty = path[(path.IndexOf('.') + 1)..];
          var property = obj.GetType().GetProperty(split[0]).AssertNotNull();
          obj = property.GetValue(obj, null).AssertNotNull();
          path = remainingProperty;
          continue;
        }
        return obj.GetType().GetProperty(path).AssertNotNull();
      }
    }

    //private static Type GetSourcePropertyType(ListBoxSelectionBehavior instance, DependencyProperty dependencyProperty)
    //{
    //  var binding = BindingOperations.GetBinding(instance, dependencyProperty);
    //  var path = binding.Path.Path;
    //  var propertyInfo = GetDeepPropertyInfo(instance.AssociatedObject.DataContext, path);
    //  return propertyInfo.PropertyType;
    //}

    private void SelectItems()
    {
      _viewHandled = true;
      AssociatedObject.SelectedItems.Clear();
      if (SelectedItems != null)
      {
        foreach (var item in SelectedItems) AssociatedObject.SelectedItems.Add(item);
      }
      _viewHandled = false;
    }

    private void SelectedValuesToItems()
    {
      if (SelectedValues == null)
      {
        SelectedItems = null;
      }
      else
      {
        SelectedItems = AssociatedObject.Items
          .Cast<object>()
          .Where(o => SelectedValues.Contains(GetDeepPropertyValue(o, AssociatedObject.SelectedValuePath)))
          .ToArray();
      }
    }

    private void SelectedItemsToValues()
    {
      if (SelectedItems == null)
      {
        SelectedValues = null;
      }
      else
      {
        SelectedValues = SelectedItems
          .Select(i => GetDeepPropertyValue(i, AssociatedObject.SelectedValuePath))
          .ToArray();
      }
    }

    private void HandleSelectionChanged(object sender, SelectionChangedEventArgs args)
    {
      if (_viewHandled) return;
      if (AssociatedObject.Items.SourceCollection == null) return;
      SelectedItems = AssociatedObject.SelectedItems.Cast<object>().ToArray();
      //var sourcePropertyType = GetSourcePropertyType(this, SelectedItemsProperty);
      //var selectedItems = Array.CreateInstance(
      //  sourcePropertyType.GetElementType(), 
      //  AssociatedObject.SelectedItems.Count
      //  ) as object[];
      //for (var i = 0; i < selectedItems.Length; ++i)
      //{
      //  selectedItems[i] = AssociatedObject.SelectedItems[i];
      //}

      //SelectedItems = selectedItems;
    }

    // Re-select items when the set of items changes
    private void HandleCollectionChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
      if (_viewHandled) return;
      if (AssociatedObject.Items.SourceCollection == null) return;
      SelectItems();
    }

    private bool _viewHandled;
    private bool _modelHandled;
  }
}
