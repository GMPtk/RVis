using LanguageExt;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using static LanguageExt.Prelude;

#nullable disable

namespace Sampling.Design
{
  internal sealed class DesignDigestsViewModel : IDesignDigestsViewModel
  {
    public ObservableCollection<IDesignDigestViewModel> DesignDigestViewModels { get; }
      = new ObservableCollection<IDesignDigestViewModel>(
        Range(1, 50).Map(i => new DesignDigestViewModel(DateTime.Now.AddHours(i), $"Sampling No.{i:0000}s"))
        );

    public IDesignDigestViewModel SelectedDesignDigestViewModel { get => DesignDigestViewModels[2]; set => throw new NotImplementedException(); }

    public ICommand LoadSamplingDesign => throw new NotImplementedException();

    public ICommand DeleteSamplingDesign => throw new NotImplementedException();

    public ICommand FollowKeyboardInDesignDigests => throw new NotImplementedException();

    public Option<(DateTime CreatedOn, DateTime SelectedOn)> TargetSamplingDesign { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
  }
}
