using LanguageExt;
using RVisUI.Model;
using System;
using System.Windows.Input;
using static LanguageExt.Prelude;

#nullable disable

namespace RVisUI.Mvvm.Design
{
  public class DataExportConfigurationViewModel : IDataExportConfigurationViewModel
  {
    public DataExportConfiguration DataExportConfiguration { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public string Title => "Export Something or Other";

    public string RootExportDirectory
    {
      get => @"C:\xxx\yyy\zzz";
      set => throw new NotImplementedException();
    }

    public ICommand BrowseForRootExportDirectory => throw new NotImplementedException();

    public string ExportDirectoryName
    {
      get => "Some design name";
      set => throw new NotImplementedException();
    }

    public bool OpenAfterExport { get => true; set => throw new NotImplementedException(); }

    public Arr<ISelectableOutputViewModel> Outputs => Range(0, 20)
      .Map(i => new SelectableOutputViewModel($"Output {i:0000}") { IsSelected = i % 2 == 0 })
      .ToArr<ISelectableOutputViewModel>();

    public ICommand OK => throw new NotImplementedException();

    public ICommand Cancel => throw new NotImplementedException();

    public bool? DialogResult { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
  }
}
