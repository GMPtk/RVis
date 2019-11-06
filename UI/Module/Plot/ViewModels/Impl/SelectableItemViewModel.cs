using ReactiveUI;
using RVisUI.Model.Extensions;
using System;
using System.ComponentModel;
using System.Windows.Input;

namespace Plot
{
  public sealed class SelectableItemViewModel<T> : ISelectableItemViewModel, INotifyPropertyChanged
  {
    public SelectableItemViewModel(T item, string label, string sortKey, ICommand select, bool use)
    {
      Item = item;
      Label = label;
      SortKey = sortKey;
      Select = select;
      _use = use;

      this
        .ObservableForProperty(vm => vm.Use)
        .Subscribe(_ => select?.Execute(this));
    }

    public T Item { get; }

    public string Label { get; }

    public string SortKey { get; }

    public ICommand Select { get; }

    public bool Use
    {
      get => _use;
      set => this.RaiseAndSetIfChanged(ref _use, value, PropertyChanged);
    }
    private bool _use;

    public event PropertyChangedEventHandler PropertyChanged;
  }
}
