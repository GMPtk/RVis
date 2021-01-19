using System;
using System.Windows.Input;

#nullable disable

namespace RVisUI.Mvvm.Design
{
  public sealed class ImportMCSimViewModel : IImportMCSimViewModel
  {
    public ICommand BrowseForExecutable => throw new NotImplementedException();

    public string PathToExecutable { get => @"C:\something\or\other.exe"; set => throw new NotImplementedException(); }

    public ICommand BrowseForConfigurationFile => throw new NotImplementedException();

    public string PathToConfigurationFile { get => @"C:\something\or\other.R"; set => throw new NotImplementedException(); }

    public ICommand BrowseForTemplateInFile => throw new NotImplementedException();

    public string PathToTemplateInFile { get => @"C:\something\or\other.in"; set => throw new NotImplementedException(); }

    public bool OpenOnImport { get => true; set => throw new NotImplementedException(); }

    public ICommand Import => throw new NotImplementedException();

    public bool CanImport => throw new NotImplementedException();

    public bool IsBusy { get => false; set => throw new NotImplementedException(); }
    
    public string BusyWith { get => "Running sim"; set => throw new NotImplementedException(); }
  }
}
