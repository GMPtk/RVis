using OxyPlot;
using RVis.Model;
using System.Data;
using System.Linq;
using static LanguageExt.Prelude;

namespace Sampling.Design
{
  internal static class Data
  {
    internal const int SAMPLE_SIZE = 100;

    internal static INoDesignActivityViewModel NoDesignActivityViewModel => 
      new NoDesignActivityViewModel();

    internal static ISamplesDesignActivityViewModel SamplesDesignActivityViewModel =>
      new SamplesDesignActivityViewModel() { Samples = GetSamples() };

    internal static IOutputsDesignActivityViewModel OutputsDesignActivityViewModel =>
      new OutputsDesignActivityViewModel(new RVisUI.AppInf.Design.AppService(), new RVisUI.AppInf.Design.AppSettings())
      {
        ElementNames = Array("aaa", "bbb", "ccc"),
        Outputs = GetOutputs()
      };

    internal static IParameterSamplingViewModel BetaParameterSamplingViewModel
    {
      get
      {
        var parameterSamplingViewModel = new ParameterSamplingViewModel(
          new SimParameter("a", "123", "mg/L", default)
          )
        {
          Distribution = new BetaDistribution(0.5, 0.5)
        };

        DoSample(parameterSamplingViewModel);

        return parameterSamplingViewModel;
      }
    }

    internal static IParameterSamplingViewModel BetaScaledParameterSamplingViewModel
    {
      get
      {
        var parameterSamplingViewModel = new ParameterSamplingViewModel(
          new SimParameter("b","123","mg/L", default)
          )
        {
          Distribution = new BetaScaledDistribution(0.5, 0.5, 10, 5)
        };

        DoSample(parameterSamplingViewModel);

        return parameterSamplingViewModel;
      }
    }

    internal static IParameterSamplingViewModel GammaParameterSamplingViewModel
    {
      get
      {
        var parameterSamplingViewModel = new ParameterSamplingViewModel(
          new SimParameter("c","123","mg/L", default)
          )
        {
          Distribution = new GammaDistribution(2, 0.5)
        };

        DoSample(parameterSamplingViewModel);

        return parameterSamplingViewModel;
      }
    }

    internal static IParameterSamplingViewModel InvariantParameterSamplingViewModel
    {
      get
      {
        var parameterSamplingViewModel = new ParameterSamplingViewModel(
          new SimParameter("d","123","mg/L", default)
          )
        {
          Distribution = new InvariantDistribution(42)
        };

        DoSample(parameterSamplingViewModel);

        return parameterSamplingViewModel;
      }
    }

    internal static IParameterSamplingViewModel InverseGammaParameterSamplingViewModel
    {
      get
      {
        var parameterSamplingViewModel = new ParameterSamplingViewModel(
          new SimParameter("e","123","mg/L", default)
          )
        {
          Distribution = new InverseGammaDistribution(1, 1)
        };

        DoSample(parameterSamplingViewModel);

        return parameterSamplingViewModel;
      }
    }

    internal static IParameterSamplingViewModel LogNormalParameterSamplingViewModel
    {
      get
      {
        var parameterSamplingViewModel = new ParameterSamplingViewModel(
          new SimParameter("f","123","mg/L", default)
          )
        {
          Distribution = new LogNormalDistribution(2, 0.6)
        };

        DoSample(parameterSamplingViewModel);

        return parameterSamplingViewModel;
      }
    }

    internal static IParameterSamplingViewModel LogUniformParameterSamplingViewModel
    {
      get
      {
        var parameterSamplingViewModel = new ParameterSamplingViewModel(
          new SimParameter("g","123","mg/L", default)
          )
        {
          Distribution = new LogUniformDistribution(0.6, 2)
        };

        DoSample(parameterSamplingViewModel);

        return parameterSamplingViewModel;
      }
    }

    internal static IParameterSamplingViewModel NormalParameterSamplingViewModel
    {
      get
      {
        var parameterSamplingViewModel = new ParameterSamplingViewModel(
          new SimParameter("h","123","mg/L", default)
          )
        {
          Distribution = new NormalDistribution(2, 0.6)
        };

        DoSample(parameterSamplingViewModel);

        return parameterSamplingViewModel;
      }
    }

    internal static IParameterSamplingViewModel StudentTParameterSamplingViewModel
    {
      get
      {
        var parameterSamplingViewModel = new ParameterSamplingViewModel(
          new SimParameter("i","123","mg/L", default)
          )
        {
          Distribution = new StudentTDistribution(0, 1, 5)
        };

        DoSample(parameterSamplingViewModel);

        return parameterSamplingViewModel;
      }
    }

    internal static IParameterSamplingViewModel UniformParameterSamplingViewModel
    {
      get
      {
        var parameterSamplingViewModel = new ParameterSamplingViewModel(
          new SimParameter("j","123","mg/L", default)
          )
        {
          Distribution = new UniformDistribution(2, 4)
        };

        DoSample(parameterSamplingViewModel);

        return parameterSamplingViewModel;
      }
    }

    private static void DoSample(IParameterSamplingViewModel parameterSamplingViewModel)
    {
      parameterSamplingViewModel.Distribution.FillSamples(_samples);
      parameterSamplingViewModel.Samples = _samples.ToArr();
    }

    private static DataView GetSamples()
    {
      const int nColumns = 20;
      const int nRows = 100;
      var columnNames = Range(1, nColumns).Map(i => $"Column{i:000}");
      var dataTable = new DataTable();
      columnNames.Iter(cn => dataTable.Columns.Add(new DataColumn(cn, typeof(double))));
      for (var row = 0; row < nRows; ++row)
      {
        var dataRow = dataTable.NewRow();
        dataRow.ItemArray = Range(1, nColumns)
          .Map(c => 1.0 * c * row)
          .Cast<object>()
          .ToArray();
        dataTable.Rows.Add(dataRow);
      }
      return dataTable.DefaultView;
    }

    private static PlotModel GetOutputs()
    {
      var outputs = new PlotModel() { Title = "OUTPUTS" };

      return outputs;
    }

    private static readonly double[] _samples = new double[SAMPLE_SIZE];
  }
}
