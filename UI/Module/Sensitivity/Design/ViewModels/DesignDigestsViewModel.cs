using LanguageExt;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using static LanguageExt.Prelude;

#nullable disable

namespace Sensitivity.Design
{
  internal sealed class DesignDigestsViewModel : IDesignDigestsViewModel
  {
    public ObservableCollection<IDesignDigestViewModel> DesignDigestViewModels { get; }
      = new ObservableCollection<IDesignDigestViewModel>(
        Range(1, 50).Map(i => new DesignDigestViewModel(DateTime.Now.AddHours(i), $"Sensitivity No.{i:0000}s"))
        );

    public IDesignDigestViewModel SelectedDesignDigestViewModel { get => DesignDigestViewModels[2]; set => throw new NotImplementedException(); }

    public ICommand LoadSensitivityDesign => throw new NotImplementedException();

    public ICommand DeleteSensitivityDesign => throw new NotImplementedException();

    public ICommand FollowKeyboardInDesignDigests => throw new NotImplementedException();

    public Option<(DateTime CreatedOn, DateTime SelectedOn)> TargetSensitivityDesign { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
  }
}
