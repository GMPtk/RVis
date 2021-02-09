using Ninject;
using NLog;
using ReactiveUI;
using RVis.Base.Extensions;
using RVis.Client;
using RVis.Model;
using RVisUI.Controls.Dialogs;
using RVisUI.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using static RVis.Base.Check;

namespace RVisUI.Ioc
{
  internal sealed class AppService : IAppService, IDisposable
  {
    public AppService(IAppSettings appSettings)
    {
      _appSettings = appSettings;
      SecondInterval = Observable.Interval(TimeSpan.FromSeconds(1), DispatcherScheduler.Current);
    }

    public IKernel Factory => App.Current.NinjectKernel;

    public IObservable<long> SecondInterval { get; }

    public bool CheckAccess() => App.Current.Dispatcher.CheckAccess();

    public bool ShowDialog(object view, object viewModel, object? parentViewModel)
    {
      RequireNotNull(view);
      RequireNotNull(viewModel);

      if (view is not Window window)
      {
        throw new ArgumentException(
          $"Expecting Window instance; got {view.GetType().Name}",
          nameof(view)
          );
      }

      window.Icon = App.Current.MainWindow.Icon.Clone();
      window.DataContext = viewModel;
      window.Owner = GetWindowForDataContext(parentViewModel);
      return window.ShowDialog() == true;
    }

    public bool ShowDialog(object viewModel, object? parentViewModel)
    {
      var typeName = viewModel.GetType().Name;

      RequireTrue(typeName.EndsWith("ViewModel", StringComparison.InvariantCulture));
      var dialogViewName = typeName.Substring(0, typeName.Length - "ViewModel".Length);

      RequireTrue(_dialogViews.ContainsKey(dialogViewName));
      var viewType = _dialogViews[dialogViewName];

      var view = RequireInstanceOf<Window>(Activator.CreateInstance(viewType));
      view.DataContext = viewModel;
      view.Owner = GetWindowForDataContext(parentViewModel);

      return view.ShowDialog() == true;
    }

    public bool AskUserYesNoQuestion(string question, string prompt, string about)
    {
      var dialog = new AskYesNoDialog(question, prompt, about)
      {
        Owner = App.Current.MainWindow
      };

      return dialog.ShowDialog() == true;
    }

    public bool BrowseForDirectory(string? startPath, [NotNullWhen(true)] out string? pathToDirectory)
    {
      using (var dialog = new FolderBrowserDialog { SelectedPath = startPath })
      {
        if (dialog.ShowDialog() == DialogResult.OK)
        {
          pathToDirectory = dialog.SelectedPath;
          return pathToDirectory.IsAString();
        }
      }

      pathToDirectory = null;
      return false;
    }

    public bool OpenFile(string purpose, string? initialDirectory, string filter, [NotNullWhen(true)] out string? pathToFile)
    {
      var openFileDialog = new Microsoft.Win32.OpenFileDialog()
      {
        InitialDirectory = initialDirectory,
        Filter = filter,
        Title = purpose
      };

      var didChoose = openFileDialog.ShowDialog(App.Current.MainWindow);
      if (didChoose.IsTrue())
      {
        pathToFile = openFileDialog.FileName;
        return true;
      }

      pathToFile = null;
      return false;
    }

    public bool SaveFile(
      string purpose,
      string? initialDirectory,
      string filter,
      string extension,
      [NotNullWhen(true)] out string? pathToFile
      )
    {
      var saveFileDialog = new Microsoft.Win32.SaveFileDialog
      {
        DefaultExt = "." + extension,
        Filter = filter,
        InitialDirectory = initialDirectory,
        Title = purpose
      };

      var didSave = saveFileDialog.ShowDialog(App.Current.MainWindow);
      if (didSave.IsTrue())
      {
        pathToFile = saveFileDialog.FileName;
        return true;
      }

      pathToFile = null;
      return false;
    }

    public void Notify(string about, string subject, Exception ex, object? originatingViewModel = default) =>
      Notify(NotificationType.Error, about, subject, ex.Message, originatingViewModel);

    public void Notify(NotificationType notificationType, string about, string subject, string detail, object? originatingViewModel = default)
    {
      Window? owner = default;
      if (originatingViewModel != default)
      {
        foreach (Window window in App.Current.Windows)
        {
          if (ReferenceEquals(window.DataContext, originatingViewModel))
          {
            owner = window;
            break;
          }
        }
      }

      owner ??= App.Current.GetActiveWindow() ?? App.Current.MainWindow;

      void DoNotify(object? _, EventArgs? __)
      {
        if (owner != null) owner.ContentRendered -= DoNotify;

        new NotifyDialog(notificationType, about, subject + Environment.NewLine + Environment.NewLine + detail)
        {
          Owner = owner
        }
        .ShowDialog();
      }

      if (owner != null && !owner.IsLoaded)
      {
        owner.ContentRendered += DoNotify;
      }
      else
      {
        DoNotify(default, default);
      }
    }

    public void ScheduleAction(Action action) =>
      App.Current.Dispatcher.Invoke(action);

    public void ScheduleLowPriorityAction(Action action) =>
      App.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, action);

    public IRVisServerPool RVisServerPool => _rVisServerPool;

    public void ResetRVisServerPool() => ConfigureRVisServerPool();

    public void Initialize()
    {
      _appSettings
        .ObservableForProperty(ro => ro.RThrottlingUseCores)
        .Subscribe(_ => ConfigureRVisServerPool());

      ConfigureRVisServerPool();

      ConfigureDialogViewLookup();
    }

    public IReactiveSafeInvoke GetReactiveSafeInvoke() => new _ReactiveSafeInvoke(App.Current.Log, this);

    private class _ReactiveSafeInvoke : IReactiveSafeInvoke
    {
      internal _ReactiveSafeInvoke(ILogger logger, IAppService appService)
      {
        _logger = logger;
        _appService = appService;
      }

      public bool React { get; private set; } = true;

      public IDisposable SuspendedReactivity
      {
        get
        {
          RequireTrue(React);
          React = false;
          return Disposable.Create(() => React = true);
        }
      }

      public Action<T> SuspendAndInvoke<T>(Action<T> action, [CallerMemberName] string subject = "") =>
      t =>
      {
        _appService.CheckAccess();

        if (!React) return;

        React = false;

        try
        {
          action(t);
        }
        catch (Exception ex)
        {
          _logger.Error(ex, $"Observer fault. Subject: {subject}.");
          _appService.Notify("Observer fault via SafeInvoke", subject, ex);
        }
        finally
        {
          React = true;
        }
      };

      private readonly ILogger _logger;
      private readonly IAppService _appService;
    }

    public Action<T> SafeInvoke<T>(Action<T> action, [CallerMemberName] string subject = "") =>
      t =>
      {
        try
        {
          action(t);
        }
        catch (Exception ex)
        {
          App.Current.Log.Error(ex, $"Observer fault. Subject: {subject}.");
          Notify("Observer fault via SafeInvoke", subject, ex);
        }
      };

    public void Dispose() => Dispose(true);

    private void Dispose(bool disposing)
    {
      if (!_disposed)
      {
        if (disposing)
        {
          _rVisServerPool.Dispose();
        }

        _disposed = true;
      }
    }

    private void ConfigureRVisServerPool()
    {
      _rVisServerPool.DestroyPool();
      _rVisServerPool.CreatePool(
        () => new RVisServer(),
        _appSettings.RThrottlingUseCores
        );
    }

    private void ConfigureDialogViewLookup()
    {
      var assembly = typeof(AppService).Assembly;
      var ns = $"{nameof(RVisUI)}.{nameof(Controls)}.{nameof(Controls.Dialogs)}";
      var reNS = ns.Replace(".", @"\.");
      var reDialogViewTypeName = new Regex(reNS + @"\.(.*)Dialog$");

      var classes = assembly.GetTypes().Where(t => t.IsClass);
      foreach (var @class in classes)
      {
        if (@class.FullName is null) continue;

        var match = reDialogViewTypeName.Match(@class.FullName);

        if (match.Success)
        {
          var dialogViewName = match.Groups[1].Value;
          _dialogViews.Add(dialogViewName, @class);
        }
      }
    }

    private static Window GetWindowForDataContext(object? dataContext)
    {
      if (dataContext != default)
      {
        foreach (Window window in App.Current.Windows)
        {
          if (ReferenceEquals(window.DataContext, dataContext))
          {
            return window;
          }
        }
      }

      return App.Current.MainWindow;
    }

    private readonly IAppSettings _appSettings;
    private readonly RVisServerPool _rVisServerPool = new RVisServerPool();
    private readonly IDictionary<string, Type> _dialogViews = new SortedDictionary<string, Type>();
    private bool _disposed = false;
  }
}
