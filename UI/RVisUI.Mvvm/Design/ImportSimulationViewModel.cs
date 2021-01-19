using LanguageExt;
using RVis.Data;
using RVis.Model;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using static LanguageExt.Prelude;

#nullable disable

namespace RVisUI.Mvvm.Design
{
  public class ImportSimulationViewModel : IImportSimulationViewModel
  {
    public ICommand BrowseForRFile => throw new NotImplementedException();

    public string PathToRFile
    {
      get => "(select .R file)";
      set => throw new NotImplementedException();
    }

    public ICommand InspectRFile => throw new NotImplementedException();

    public ManagedImport ManagedImport { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public Arr<ISymbolInfo> UnaryFuncs { get => Array<ISymbolInfo>(new SymbolInfo { Symbol = "111" }); set => throw new NotImplementedException(); }
    public Arr<ISymbolInfo> Scalars { get => Array<ISymbolInfo>(new SymbolInfo { Symbol = "222" }); set => throw new NotImplementedException(); }
    public Arr<ISymbolInfo> ScalarSets { get => Array<ISymbolInfo>(new SymbolInfo { Symbol = "333" }); set => throw new NotImplementedException(); }
    public Arr<ISymbolInfo> DataSets { get => Array<ISymbolInfo>(null, null, null); set => throw new NotImplementedException(); }

    public ISymbolInfo ExecutiveFunction { get => new SymbolInfo { Symbol = "run_my_model_please()" }; set => throw new NotImplementedException(); }
    public ISymbolInfo ExecutiveFormal { get => new SymbolInfo { Symbol = "[a_list_of_parameters]" }; set => throw new NotImplementedException(); }
    public NumDataTable ExecutiveOutput
    {
      get => new NumDataTable("xxx", new NumDataColumn[] 
      {
        new NumDataColumn("time", System.Array.Empty<double>()),
        new NumDataColumn("y", System.Array.Empty<double>()),
        new NumDataColumn("z", System.Array.Empty<double>()),
      });
      set => throw new NotImplementedException();
    }

    public ICommand SelectExecutive => throw new NotImplementedException();

    public ICommand ImportUsingExec => throw new NotImplementedException();

    public ICommand ImportUsingTmpl => throw new NotImplementedException();

    public bool IsBusy { get => false; set => throw new NotImplementedException(); }
    public string BusyWith { get => "Running and Inspecting"; set => throw new NotImplementedException(); }
    public ObservableCollection<string> BusyMessages => throw new NotImplementedException();
    public bool EnableBusyCancel { get => true; set => throw new NotImplementedException(); }
    public ICommand BusyCancel => throw new NotImplementedException();
  }
}
