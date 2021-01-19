﻿using RVisUI.Model.Extensions;
using System.ComponentModel;
using System.Windows.Input;

namespace Estimation
{
  internal sealed class PriorViewModel : IPriorViewModel, INotifyPropertyChanged
  {
    internal PriorViewModel(string name, ICommand toggleSelect)
    {
      Name = name;
      ToggleSelect = toggleSelect;
      SortKey = name.ToUpperInvariant();
    }

    public string Name { get; }

    public string? Distribution
    {
      get => _distribution;
      set => this.RaiseAndSetIfChanged(ref _distribution, value, PropertyChanged);
    }
    private string? _distribution;

    public bool IsSelected
    {
      get => _isSelected;
      set => this.RaiseAndSetIfChanged(ref _isSelected, value, PropertyChanged);
    }
    private bool _isSelected;

    public ICommand ToggleSelect { get; }

    public string SortKey { get; }

    public event PropertyChangedEventHandler? PropertyChanged;
  }
}
