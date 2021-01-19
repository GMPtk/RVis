using RVis.Model;
using System.Data;
using System.Linq;
using static LanguageExt.Prelude;
using static RVis.Base.Check;

namespace Sampling.Design
{
  internal static class Data
  {
    public const int SAMPLE_SIZE = 100;

    public static DataView Samples => GetSamples();

    public static IParameterSamplingViewModel BetaParameterSamplingViewModel
    {
      get
      {
        var parameterSamplingViewModel = new ParameterSamplingViewModel(
          new SimParameter("a", "123", "mg/L", default),
          new ModuleState()
          )
        {
          Distribution = new BetaDistribution(0.5, 0.5)
        };

        DoSample(parameterSamplingViewModel);

        return parameterSamplingViewModel;
      }
    }

    public static IParameterSamplingViewModel BetaScaledParameterSamplingViewModel
    {
      get
      {
        var parameterSamplingViewModel = new ParameterSamplingViewModel(
          new SimParameter("b", "123", "mg/L", default),
          new ModuleState()
          )
        {
          Distribution = new BetaScaledDistribution(0.5, 0.5, 10, 5)
        };

        DoSample(parameterSamplingViewModel);

        return parameterSamplingViewModel;
      }
    }

    public static IParameterSamplingViewModel GammaParameterSamplingViewModel
    {
      get
      {
        var parameterSamplingViewModel = new ParameterSamplingViewModel(
          new SimParameter("c", "123", "mg/L", default),
          new ModuleState()
          )
        {
          Distribution = new GammaDistribution(2, 0.5)
        };

        DoSample(parameterSamplingViewModel);

        return parameterSamplingViewModel;
      }
    }

    public static IParameterSamplingViewModel InvariantParameterSamplingViewModel
    {
      get
      {
        var parameterSamplingViewModel = new ParameterSamplingViewModel(
          new SimParameter("d", "123", "mg/L", default),
          new ModuleState()
          )
        {
          Distribution = new InvariantDistribution(42)
        };

        DoSample(parameterSamplingViewModel);

        return parameterSamplingViewModel;
      }
    }

    public static IParameterSamplingViewModel InverseGammaParameterSamplingViewModel
    {
      get
      {
        var parameterSamplingViewModel = new ParameterSamplingViewModel(
          new SimParameter("e", "123", "mg/L", default),
          new ModuleState()
          )
        {
          Distribution = new InverseGammaDistribution(1, 1)
        };

        DoSample(parameterSamplingViewModel);

        return parameterSamplingViewModel;
      }
    }

    public static IParameterSamplingViewModel LogNormalParameterSamplingViewModel
    {
      get
      {
        var parameterSamplingViewModel = new ParameterSamplingViewModel(
          new SimParameter("f", "123", "mg/L", default),
          new ModuleState()
          )
        {
          Distribution = new LogNormalDistribution(2, 0.6)
        };

        DoSample(parameterSamplingViewModel);

        return parameterSamplingViewModel;
      }
    }

    public static IParameterSamplingViewModel LogUniformParameterSamplingViewModel
    {
      get
      {
        var parameterSamplingViewModel = new ParameterSamplingViewModel(
          new SimParameter("g", "123", "mg/L", default),
          new ModuleState()
          )
        {
          Distribution = new LogUniformDistribution(0.6, 2)
        };

        DoSample(parameterSamplingViewModel);

        return parameterSamplingViewModel;
      }
    }

    public static IParameterSamplingViewModel NormalParameterSamplingViewModel
    {
      get
      {
        var parameterSamplingViewModel = new ParameterSamplingViewModel(
          new SimParameter("h", "123", "mg/L", default),
          new ModuleState()
          )
        {
          Distribution = new NormalDistribution(2, 0.6)
        };

        DoSample(parameterSamplingViewModel);

        return parameterSamplingViewModel;
      }
    }

    public static IParameterSamplingViewModel StudentTParameterSamplingViewModel
    {
      get
      {
        var parameterSamplingViewModel = new ParameterSamplingViewModel(
          new SimParameter("i", "123", "mg/L", default),
          new ModuleState()
          )
        {
          Distribution = new StudentTDistribution(0, 1, 5)
        };

        DoSample(parameterSamplingViewModel);

        return parameterSamplingViewModel;
      }
    }

    public static IParameterSamplingViewModel UniformParameterSamplingViewModel
    {
      get
      {
        var parameterSamplingViewModel = new ParameterSamplingViewModel(
          new SimParameter("j", "123", "mg/L", default),
          new ModuleState()
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
      RequireNotNull(parameterSamplingViewModel.Distribution);

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

    private static readonly double[] _samples = new double[SAMPLE_SIZE];
  }
}
