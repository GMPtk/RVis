using System;
using System.Windows.Input;

#nullable disable

namespace RVisUI.Mvvm.Design
{
  public sealed class ImportRSimViewModel : IImportRSimViewModel
  {
    public ICommand BrowseForScript => throw new NotImplementedException();

    public string PathToScript { get => @"C:\something\or\other.R"; set => throw new NotImplementedException(); }

    public bool OpenOnImport { get => true; set => throw new NotImplementedException(); }

    public ICommand Import => throw new NotImplementedException();

    public bool CanImport => throw new NotImplementedException();

    public bool IsBusy { get => false; set => throw new NotImplementedException(); }
    
    public string BusyWith { get => "Running sim"; set => throw new NotImplementedException(); }
  }
}
