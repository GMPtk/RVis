using ReactiveUI;
using RVisUI.Model.Extensions;
using System;
using System.ComponentModel;
using System.Windows.Input;

namespace RVisUI.AppInf
{
  public sealed class SelectableItemViewModel<T> : ISelectableItemViewModel, INotifyPropertyChanged
  {
    public SelectableItemViewModel(T item, string label, string sortKey, ICommand select, bool isSelected)
    {
      Item = item;
      Label = label;
      SortKey = sortKey;
      Select = select;
      _isSelected = isSelected;

      this
        .ObservableForProperty(vm => vm.IsSelected)
        .Subscribe(_ => select?.Execute(this));
    }

    public T Item { get; }

    public string Label { get; }

    public string SortKey { get; }

    public ICommand Select { get; }

    public bool IsSelected
    {
      get => _isSelected;
      set => this.RaiseAndSetIfChanged(ref _isSelected, value, PropertyChanged);
    }
    private bool _isSelected;

    public event PropertyChangedEventHandler? PropertyChanged;
  }
}
