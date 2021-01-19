using CsvHelper;
using LanguageExt;
using ReactiveUI;
using RVis.Base.Extensions;
using RVis.Model.Extensions;
using RVisUI.Model;
using RVisUI.Model.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Windows.Input;
using static LanguageExt.Prelude;
using static RVis.Base.Check;
using static System.Environment;
using static System.Globalization.CultureInfo;
using static System.IO.File;
using static System.IO.Path;
using static System.String;

namespace Evidence
{
  internal sealed class ImportObservationsViewModel : IImportObservationsViewModel, INotifyPropertyChanged
  {
    internal ImportObservationsViewModel(IAppState appState, IAppService appService)
    {
      _appState = appState;
      _appService = appService;

      SelectFile = ReactiveCommand.Create(HandleSelectFile);
      OK = ReactiveCommand.Create(
        HandleOK,
        this.WhenAny(vm => vm.ErrorMessage, _ => ErrorMessage == default)
        );
      Cancel = ReactiveCommand.Create(HandleCancel);
      this.ObservableForProperty(vm => vm.EvidenceName).Subscribe(ObserveEvidenceName);

      var simulation = _appState.Target.AssertSome("No simulation");
      var independentVariable = simulation.SimConfig.SimOutput.IndependentVariable;

      FQIndependentVariable = independentVariable.GetFQName();

      UpdateErrorMessage();
    }

    public string? SelectedFile
    {
      get => _selectedFile;
      set => this.RaiseAndSetIfChanged(ref _selectedFile, value, PropertyChanged);
    }
    private string? _selectedFile;

    public ICommand SelectFile { get; }

    public string FQIndependentVariable { get; }

    public string? EvidenceName
    {
      get => _evidenceName;
      set => this.RaiseAndSetIfChanged(ref _evidenceName, value, PropertyChanged);
    }
    private string? _evidenceName;

    public string? EvidenceDescription
    {
      get => _evidenceDescription;
      set => this.RaiseAndSetIfChanged(ref _evidenceDescription, value, PropertyChanged);
    }
    private string? _evidenceDescription;

    public string? RefName
    {
      get => _refName;
      set => this.RaiseAndSetIfChanged(ref _refName, value, PropertyChanged);
    }
    private string? _refName;

    public string? RefHash
    {
      get => _refHash;
      set => this.RaiseAndSetIfChanged(ref _refHash, value, PropertyChanged);
    }
    private string? _refHash;

    public Arr<IObservationsColumnViewModel> ObservationsColumnViewModels
    {
      get => _observationsColumnViewModels;
      set => this.RaiseAndSetIfChanged(ref _observationsColumnViewModels, value, PropertyChanged);
    }
    private Arr<IObservationsColumnViewModel> _observationsColumnViewModels;

    public string? ErrorMessage
    {
      get => _errorMessage;
      set => this.RaiseAndSetIfChanged(ref _errorMessage, value, PropertyChanged);
    }
    private string? _errorMessage;

    public ICommand OK { get; }

    public ICommand Cancel { get; }

    public bool? DialogResult
    {
      get => _dialogResult;
      set => this.RaiseAndSetIfChanged(ref _dialogResult, value, PropertyChanged);
    }
    private bool? _dialogResult;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void HandleSelectFile()
    {
      var didOpen = _appService.OpenFile(
        "Select Observations",
        _importDirectory,
        "Data files|*.csv;*.txt|All files|*.*",
        out string? pathToFile
        );

      if (!didOpen) return;

      _importDirectory = GetDirectoryName(pathToFile);

      RequireFile(pathToFile);

      var lines = ReadAllLines(pathToFile);
      var delimited = Join(NewLine, lines);
      var refHash = (pathToFile + delimited).ToHash();

      if (_appState.SimEvidence.EvidenceSources.ContainsEvidenceSource(refHash))
      {
        _appService.Notify(
          NotificationType.Error,
          nameof(Evidence),
          nameof(HandleSelectFile),
          "These data have already been imported",
          this
          );
        return;
      }

      if (lines.Length < 3)
      {
        _appService.Notify(
          NotificationType.Error,
          nameof(Evidence),
          nameof(HandleSelectFile),
          "Expecting at least three rows",
          this
          );
        return;
      }

      var lastLine = lines.Last().Trim();
      var delimiters = new[] { '\t', ',', ':', ' ', '|' }.Where(c => lastLine.Contains(c));

      Arr<(string ColumnName, Arr<double> Observations)> columns = default;

      try
      {
        foreach (var delimiter in delimiters)
        {
          if (ParseDelimited(
            delimited,
            delimiter,
            out columns
            )) break;
        }

        RequireFalse(columns.IsEmpty, "File format error?");
      }
      catch (Exception ex)
      {
        _appService.Notify(
          NotificationType.Error,
          nameof(Evidence),
          nameof(HandleSelectFile),
          "Failed to parse content as delimited text: " + ex.Message,
          this
          );
        return;
      }

      var simulation = _appState.Target.AssertSome("No simulation");
      var independentVariable = simulation.SimConfig.SimOutput.IndependentVariable;

      var (columnName, observations) = columns.Head();

      var ivVM = new ObservationsColumnViewModel(
        columnName,
        Array(independentVariable.Name),
        observations
        );

      var subjects = Array(
        ObservationsColumnViewModel.NO_SELECTION,
        _appState.SimEvidence.Subjects.OrderBy(s => s.ToLowerInvariant()).ToArray()
        );

      var otherVMs = columns.Tail().Map(t => new ObservationsColumnViewModel(t.ColumnName, subjects, t.Observations));
      ObservationsColumnViewModels = Array<IObservationsColumnViewModel>(ivVM, otherVMs.ToArray());

      _columnSubjectSubscriptions?.Dispose();
      _columnSubjectSubscriptions = new CompositeDisposable(
        ObservationsColumnViewModels
          .Map(vm => vm.WhenAnyValue(vmm => vmm.Subject, vmm => vmm.RefName, (s, rn) => (s, rn)))
          .Map(o => o.Subscribe(ObserveColumnSubjectRefName))
        );

      EvidenceName = GetFileNameWithoutExtension(pathToFile);
      EvidenceDescription = default;
      RefName = EvidenceName;
      RefHash = refHash;
      SelectedFile = pathToFile;
      UpdateErrorMessage();
    }

    private void ObserveColumnSubjectRefName((string, string?) _) => UpdateErrorMessage();

    private static bool ParseDelimited(
      string delimited,
      char delimiter,
      out Arr<(string ColumnName, Arr<double> Observations)> columns
      )
    {
      columns = default;

      var header = Arr<string>.Empty;
      var rows = new List<Arr<double>>();

      using (var reader = new StringReader(delimited))
      using (var csv = new CsvReader(reader, InvariantCulture))
      {
        csv.Configuration.Delimiter = delimiter.ToString(InvariantCulture);
        while (csv.Read())
        {
          if (header.IsEmpty)
          {
            if (!csv.ReadHeader()) return false;
            header = csv.Context.Record;
          }
          else
          {
            var row = csv.Context.Record.Select(
              f => double.TryParse(f, out double d)
                ? Some(d)
                : None
              );

            if (row.ContainsNone()) return false;

            rows.Add(row.Somes().ToArr());
          }
        }
      }

      if (header.Count < 2) return false;
      if (rows.Count < 2) return false;
      if (rows.Any(r => r.Count != header.Count)) return false;

      columns = header
        .Map((i, h) => (h, rows.Select(r => r[i]).ToArr()))
        .ToArr();

      return true;
    }

    private void HandleOK()
    {
      _columnSubjectSubscriptions?.Dispose();
      DialogResult = true;
    }

    private void HandleCancel()
    {
      _columnSubjectSubscriptions?.Dispose();
      DialogResult = false;
    }

    private void ObserveEvidenceName(object _) => UpdateErrorMessage();

    private void UpdateErrorMessage()
    {
      string? errorMessage = default;

      if (SelectedFile.IsntAString())
      {
        errorMessage = "(no file selected)";
      }
      else if (EvidenceName.IsntAString())
      {
        errorMessage = "Name required";
      }
      else if (ObservationsColumnViewModels.Tail().ForAll(vm => vm.Subject == ObservationsColumnViewModel.NO_SELECTION))
      {
        errorMessage = "All observations ignored";
      }
      else if (ObservationsColumnViewModels.NotAllUnique(vm => (vm.Subject, RefName = vm.RefName ?? vm.ColumnName)))
      {
        errorMessage = "Duplicate columns";
      }

      ErrorMessage = errorMessage;
    }

    private readonly IAppState _appState;
    private readonly IAppService _appService;
    private string? _importDirectory;
    private IDisposable? _columnSubjectSubscriptions;
  }
}
