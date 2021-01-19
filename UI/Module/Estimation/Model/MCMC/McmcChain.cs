using LanguageExt;
using MathNet.Numerics.Distributions;
using RVis.Base.Extensions;
using RVis.Data;
using RVis.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using static LanguageExt.Prelude;
using static RVis.Base.Check;
using static RVis.Base.Extensions.NumExt;
using static RVis.Model.RandomNumberGenerator;
using static System.Double;
using static System.Math;
using DataTable = System.Data.DataTable;

using IterationState = System.ValueTuple<
  int,                                          // current iteration
  LanguageExt.Arr<(string Name, double Value)>, // current values
  LanguageExt.Arr<(string Name, double Value)>  // proposed values 
  >;

namespace Estimation
{
  internal sealed partial class McmcChain
  {
    internal McmcChain(
      int no,
      int iterations,
      int burnIn,
      double targetAcceptRate,
      bool approximate,
      Arr<ModelParameter> modelParameters,
      Arr<ModelOutput> modelOutputs,
      Arr<SimObservations> observations
      ) : this(
        no,
        iterations,
        burnIn,
        targetAcceptRate,
        approximate,
        modelParameters,
        modelOutputs,
        observations,
        default,
        default,
        default
        )
    {
    }

    internal McmcChain(
      int no,
      int iterations,
      int burnIn,
      double targetAcceptRate,
      bool approximate,
      Arr<ModelParameter> modelParameters,
      Arr<ModelOutput> modelOutputs,
      Arr<SimObservations> observations,
      DataTable? chainData,
      DataTable? errorData,
      IDictionary<string, DataTable>? posteriorData
      )
    {
      RequireTrue(no > 0);
      RequireTrue(iterations > 0);
      RequireTrue(iterations > burnIn);
      RequireFalse(modelParameters.IsEmpty);
      RequireFalse(modelOutputs.IsEmpty);
      RequireFalse(observations.IsEmpty);

      No = no;
      _iterations = iterations;
      _burnIn = burnIn;
      _targetAcceptRate = targetAcceptRate;
      _approximate = approximate;
      _modelParameters = modelParameters.ToArray();
      _modelOutputs = modelOutputs.ToArray();
      _observations = observations;

      _chainData = chainData?.Copy() ?? new DataTable();
      _errorData = errorData?.Copy() ?? new DataTable();
      _posteriorData = new SortedDictionary<string, DataTable>();
      posteriorData?.Iter(kvp => _posteriorData.Add(kvp.Key, kvp.Value.Copy()));

      Configure();
    }

    internal int No { get; }

    internal Exception? Fault { get; private set; }

    internal bool IsFaulted => Fault != default;

    internal bool IsComplete => _currentIteration == _iterations || IsFaulted;

    internal bool Propose(out IterationState iterationState)
    {
      RequireFalse(IsComplete);

      if (_currentIteration.IsntFound())
      {
        for (_currentIteration = 0; _currentIteration < _iterations; ++_currentIteration)
        {
          if (IsNaN(_errorData.Rows[_currentIteration].Field<double>(0))) break;
        }
      }

      RequireFalse(IsComplete);

      if (_currentIteration == 0)
      {
        ProposeInitial(out iterationState);
      }
      else
      {
        ResumeIteration(out iterationState);
      }

      return true;
    }

    internal bool Propose(
      NumDataColumn independentData,
      Arr<NumDataColumn> outputData,
      out IterationState iterationState
      )
    {
      RequireTrue(_currentIteration >= 0);
      RequireTrue(_currentIteration == 0 || !IsNaN(_currentLogLikelihood));
      RequireFalse(IsComplete);

      double proposedLogLikelihood;
      try
      {
        proposedLogLikelihood = GetProposalLogLikelihood(
          independentData,
          outputData
          );
      }
      catch (Exception ex)
      {
        FaultChain(ex);
        iterationState = default;
        return false;
      }

      ProcessCurrentProposal(proposedLogLikelihood); // M-H + bookkeeping (parameters)

      var currentChainDataRow = _chainData.Rows[_currentIteration];

      var indexNextModelParameter = _currentIteration == 0
        ? NOT_FOUND
        : _modelParameters.FindIndex(
            mp => IsNaN(currentChainDataRow.Field<double>(ToAcceptColumnName(mp.Name)))
          );

      var isIterationComplete = indexNextModelParameter.IsntFound();

      if (isIterationComplete) StorePosterior(); // model outputs collected

      var isLastIteration = (_currentIteration + 1) == _iterations;

      if (isIterationComplete && isLastIteration)
      {
        ++_currentIteration;
        var currentValues = _modelParameters
          .Select(mp => (mp.Name, Value: currentChainDataRow.Field<double>(mp.Name)))
          .ToArr();
        iterationState = (_currentIteration, currentValues, default);
        return false;
      }

      if (isIterationComplete)
      {
        UpdateModelOutputs(); // M-H + bookkeeping (error models)

        var doAdjustForAcceptRate =
          !IsNaN(_targetAcceptRate) &&
          _currentIteration > _burnIn &&
          _currentIteration % ACCEPT_RATE_ADJUST_INTERVAL == 0;

        if (doAdjustForAcceptRate) AdjustForAcceptRate();

        // done - advance and initialize next iteration using current
        ++_currentIteration;
        indexNextModelParameter = 0;
        var nextChainDataRow = _chainData.Rows[_currentIteration];
        _modelParameters.Iter(
          mp => nextChainDataRow[mp.Name] = currentChainDataRow[mp.Name]
          );
        _modelParameters.Iter(
          mp => nextChainDataRow[ToLLColumnName(mp.Name)] = currentChainDataRow[ToLLColumnName(mp.Name)]
          );
        currentChainDataRow = nextChainDataRow;
      }

      _currentProposal = _modelParameters[indexNextModelParameter].GetProposal();

      var proposedValues = _modelParameters
        .Select(mp => (
          mp.Name,
          Value: mp.Name == _currentProposal.Name
            ? _currentProposal.Value
            : currentChainDataRow.Field<double>(mp.Name)
          )
        )
        .ToArr();

      if (isIterationComplete) // meaning was completed...
      {
        // ...so include update for client
        var currentValues = _modelParameters
          .Select(mp => (mp.Name, Value: currentChainDataRow.Field<double>(mp.Name)))
          .ToArr();
        iterationState = (_currentIteration, currentValues, proposedValues);
        return true;
      }

      iterationState = (_currentIteration, default, proposedValues);
      return true;
    }

    internal void FaultChain(Exception fault)
    {
      RequireFalse(IsComplete);
      Fault = fault;
    }

    internal (
      Arr<ModelParameter> ModelParameters,
      Arr<ModelOutput> ModelOutputs,
      DataTable ChainData,
      DataTable ErrorData,
      IDictionary<string, DataTable> PosteriorData
      ) GetState()
    {
      if (!IsComplete)
      {
        // abandon work on current iteration
        var currentChainDataRow = _chainData.Rows[_currentIteration];
        Range(0, _chainData.Columns.Count).Iter(i => currentChainDataRow[i] = NaN);
      }

      _chainData.AcceptChanges();
      _errorData.AcceptChanges();
      _posteriorData.Iter(kvp => kvp.Value.AcceptChanges());

      var posteriorData = new SortedDictionary<string, DataTable>();
      _posteriorData.Iter(kvp => posteriorData.Add(kvp.Key, kvp.Value.Copy()));

      return (_modelParameters, _modelOutputs, _chainData.Copy(), _errorData.Copy(), posteriorData);
    }

    private void ConfigureChainData()
    {
      var chainDataColumnNames = ToColumnNames(_modelParameters);

      if (_chainData.HasNoSchema())
      {
        chainDataColumnNames.Iter(
          cn => _chainData.Columns.Add(new DataColumn(cn, typeof(double)))
          );
      }
      else
      {
        RequireTrue(
          chainDataColumnNames.SequenceEqual(
            _chainData.Columns.Cast<DataColumn>().Select(dc => dc.ColumnName)
            )
          );
        RequireTrue(
          _chainData.Columns.Cast<DataColumn>().All(
            dc => dc.DataType == typeof(double)
            )
          );
      }

      foreach (DataColumn dataColumn in _chainData.Columns)
      {
        dataColumn.DefaultValue = NaN;
      }

      while (_chainData.Rows.Count < _iterations)
      {
        _chainData.Rows.Add(_chainData.NewRow());
      }

      RequireTrue(_chainData.Rows.Count == _iterations);
    }

    private void ConfigureErrorData()
    {
      var errorDataColumnNames = ToColumnNames(_modelOutputs);

      if (_errorData.HasNoSchema())
      {
        errorDataColumnNames.Iter(
          cn => _errorData.Columns.Add(new DataColumn(cn, typeof(double)))
          );
      }
      else
      {
        RequireTrue(
          errorDataColumnNames.SequenceEqual(
            _errorData.Columns.Cast<DataColumn>().Select(dc => dc.ColumnName)
            )
          );
        RequireTrue(
          _errorData.Columns.Cast<DataColumn>().All(
            dc => dc.DataType == typeof(double)
            )
          );
      }

      foreach (DataColumn dataColumn in _errorData.Columns)
      {
        dataColumn.DefaultValue = NaN;
      }

      while (_errorData.Rows.Count < _iterations)
      {
        _errorData.Rows.Add(_errorData.NewRow());
      }

      RequireTrue(_errorData.Rows.Count == _iterations);
    }

    private void Configure()
    {
      ConfigureChainData();
      ConfigureErrorData();

      var lastChainDataRow = _chainData.Rows[^1];

      var isComplete = _modelParameters.All(
        mp => !IsNaN(lastChainDataRow.Field<double>(ToLLColumnName(mp.Name)))
        );

      if (isComplete) _currentIteration = _iterations;

      // give state maps their keys
      _observations.Iter(o => _proposalOutputs.Add(o.GetHashCode(), default));
      _modelParameters.Iter(mp => _priorDensities.Add(mp.Name, default));
      _modelOutputs.Iter(mo => _outputLogLikelihoods.Add(mo.Name, (NaN, NaN)));
    }

    private void ProposeInitial(out IterationState iterationState)
    {
      for (var i = 0; i < _modelParameters.Length; ++i)
      {
        var modelParameter = _modelParameters[i];

        modelParameter = new ModelParameter(
          modelParameter.Name,
          modelParameter.Distribution,
          _modelParameters[i].Distribution.Mean,
          modelParameter.Step
          );

        _modelParameters[i] = modelParameter;

        _priorDensities[modelParameter.Name] = Log(modelParameter.GetValueDensity());
      }

      var proposedValues = _modelParameters
        .Select(mp => (mp.Name, mp.Value))
        .ToArr();

      var currentChainRow = _chainData.Rows[_currentIteration];

      proposedValues.Iter(pv => currentChainRow[pv.Name] = pv.Value);

      iterationState = (_currentIteration, default, proposedValues);
    }

    private void ResumeIteration(out IterationState iterationState)
    {
      var previousErrorDataRow = _errorData.Rows[_currentIteration - 1];
      _currentLogLikelihood = 0d;

      _modelOutputs.Iter(mo =>
      {
        // populate current LL state using results from last iteration
        var logLikelihood = previousErrorDataRow.Field<double>(ToLLColumnName(mo.Name));
        _currentLogLikelihood += logLikelihood;
        _outputLogLikelihoods[mo.Name] = (NaN, logLikelihood);

        // populate proposal output state from posterior for last iteration 
        var outputObservations = _observations.Filter(o => o.Subject == mo.Name);
        var posteriorData = _posteriorData[mo.Name];
        var previousPosteriorDataRow = posteriorData.Rows[_currentIteration];
        var data = previousPosteriorDataRow.ItemArray.Cast<double>().ToArray();
        var taken = 0;

        foreach (var observations in outputObservations)
        {
          var proposalOutput = data.Skip(taken).Take(observations.X.Count).ToArr();
          _proposalOutputs[observations.GetHashCode()] = (default, proposalOutput);
          taken += observations.X.Count;
        }
      });

      RequireFalse(IsNaN(_currentLogLikelihood));

      // re-initialize current iteration with data from last
      var previousChainDataRow = _chainData.Rows[_currentIteration - 1];
      var currentChainDataRow = _chainData.Rows[_currentIteration];

      _modelParameters.Iter(
        mp => currentChainDataRow[mp.Name] = previousChainDataRow[mp.Name]
        );
      _modelParameters.Iter(
        mp => currentChainDataRow[ToLLColumnName(mp.Name)] = previousChainDataRow[ToLLColumnName(mp.Name)]
        );
      _modelParameters.Iter(
        mp => _priorDensities[mp.Name] = Log(mp.GetValueDensity())
        );

      // start at beginning of chain row and propose
      _currentProposal = _modelParameters[0].GetProposal();

      var proposedValues = _modelParameters
        .Map(mp => (mp.Name, Value: mp.Name == _currentProposal.Name ? _currentProposal.Value : mp.Value))
        .ToArr();

      iterationState = (_currentIteration, default, proposedValues);
    }

    private void HandleInvalidLogLikelihood(
      ModelOutput modelOutput,
      IReadOnlyList<double> independentColumn,
      IReadOnlyList<double> predictedOutput,
      IReadOnlyList<double> x
      )
    {
      var message = $"In chain no.{No}, iteration no.{_currentIteration}, error model ({modelOutput.ErrorModel.GetType().Name}) for {modelOutput.Name} failed to compute likelihood";

      string? reason = default;

      if (_approximate && !independentColumn.AllUnique(d => d))
      {
        reason = "approximation requires independent variable to contain unique values";
      }

      if (reason.IsntAString() && !modelOutput.ErrorModel.CanHandleNegativeSampleSpace)
      {
        var hasNegativePrediction = predictedOutput.Any(d => d < 0d);
        var hasNegativeObservation = x.Any(d => d < 0d);

        if (hasNegativePrediction || hasNegativeObservation)
        {
          reason = "error model does not support negative sample space";
        }
      }

      message = $"{message}. Reason: {(reason ?? "unknown")}.";

      throw new InvalidOperationException(message);
    }

    private double GetProposalLogLikelihood(
      NumDataColumn independentData,
      Arr<NumDataColumn> outputData
      )
    {
      var proposedLogLikelihood = 0.0;

      foreach (var modelOutput in _modelOutputs)
      {
        var predictedOutput = outputData
          .Find(ndc => ndc.Name == modelOutput.Name)
          .AssertSome();

        var outputObservations = _observations.Filter(o => o.Subject == modelOutput.Name);

        var outputLogLikelihood = 0.0;

        foreach (var observations in outputObservations)
        {
          var proposalOutput = _approximate
            ? observations.X.ApproximateY(independentData.Data, predictedOutput.Data).ToArr()
            : observations.X.SelectNearestY(independentData.Data, predictedOutput.Data).ToArr();

          RequireFalse(
            proposalOutput.Exists(IsNaN),
            $"In chain no.{No}, iteration no.{_currentIteration}, proposal produced NaNs for {modelOutput.Name}"
            );

          // record approximated outputs for these observations
          // will be used in perturbing error models
          // will be reversed if proposal rejected
          // will be recorded in posterior at end of iteration
          var (_, currentPO) = _proposalOutputs[observations.GetHashCode()];
          _proposalOutputs[observations.GetHashCode()] = (currentPO, proposalOutput);

          var logLikelihood = modelOutput.ErrorModel.GetLogLikelihood(proposalOutput, observations.Y);
          if (IsNaN(logLikelihood))
          {
            HandleInvalidLogLikelihood(
              modelOutput,
              independentData.Data,
              predictedOutput.Data,
              observations.X
              );
          }
          outputLogLikelihood += logLikelihood;
        }

        // record LL per output so we can M-H error model at end of iteration
        // will be reversed if proposal rejected
        var (_, currentLL) = _outputLogLikelihoods[modelOutput.Name];
        _outputLogLikelihoods[modelOutput.Name] = (currentLL, outputLogLikelihood);
        proposedLogLikelihood += outputLogLikelihood;
      }

      return proposedLogLikelihood;
    }

    private void ProcessCurrentProposal(double proposedLogLikelihood)
    {
      var currentChainDataRow = _chainData.Rows[_currentIteration];

      if (_currentIteration == 0)
      {
        // top row is prior means; not individually perturbed so same LL
        _modelParameters.Iter(
          mp => currentChainDataRow[ToLLColumnName(mp.Name)] = proposedLogLikelihood
          );
        _currentLogLikelihood = proposedLogLikelihood;
        return;
      }

      // do M-H

      var ratio = Exp(proposedLogLikelihood - _currentLogLikelihood);

      var currentProposalDensity = Log(_currentProposal.GetValueDensity());
      var currentPriorDensity = _priorDensities.Values.Sum();
      var proposalPriorDensity = _priorDensities
        .Select(
          kvp => kvp.Key == _currentProposal.Name
            ? currentProposalDensity
            : kvp.Value
          )
        .Sum();

      ratio *= Exp(proposalPriorDensity - currentPriorDensity);

      var accept = Min(1.0, ratio) > ContinuousUniform.Sample(Generator, 0, 1);
      currentChainDataRow[ToAcceptColumnName(_currentProposal.Name)] = accept ? 1d : 0d;

      if (accept)
      {
        _currentLogLikelihood = proposedLogLikelihood;

        currentChainDataRow[_currentProposal.Name] = _currentProposal.Value;
        currentChainDataRow[ToLLColumnName(_currentProposal.Name)] = proposedLogLikelihood;

        var indexCurrentProposal = _modelParameters.FindIndex(mp => mp.Name == _currentProposal.Name);
        _modelParameters[indexCurrentProposal] = _currentProposal;
        _priorDensities[_currentProposal.Name] = currentProposalDensity;
      }
      else
      {
        // rollback per output LLs and per obs outputs
        foreach (var modelOutput in _modelOutputs)
        {
          var (previousLL, _) = _outputLogLikelihoods[modelOutput.Name];
          RequireFalse(IsNaN(previousLL));
          _outputLogLikelihoods[modelOutput.Name] = (NaN, previousLL);

          var outputObservations = _observations.Filter(o => o.Subject == modelOutput.Name);

          foreach (var observations in outputObservations)
          {
            var key = observations.GetHashCode();
            var (previousPO, _) = _proposalOutputs[key];
            RequireFalse(previousPO.IsEmpty);
            _proposalOutputs[key] = (default, previousPO);
          }
        }
      }

    }

    private void StorePosterior()
    {
      foreach (var modelOutput in _modelOutputs)
      {
        if (!_posteriorData.ContainsKey(modelOutput.Name))
        {
          _posteriorData.Add(modelOutput.Name, new DataTable());
        }

        var posteriorData = _posteriorData[modelOutput.Name];

        var outputObservations = _observations.Filter(o => o.Subject == modelOutput.Name);

        if (posteriorData.HasNoSchema())
        {
          foreach (var observations in outputObservations)
          {
            var nColumns = observations.X.Count;
            for (var i = 0; i < nColumns; ++i)
            {
              posteriorData.Columns.Add(
                new DataColumn($"{observations.RefName}/{observations.ID} {i:000}", typeof(double))
                );
            }
          }

          var allXs = outputObservations.Bind(o => o.X);
          var firstRow = posteriorData.NewRow();
          firstRow.ItemArray = allXs.Cast<object>().ToArray();
          posteriorData.Rows.Add(firstRow);
        }

        foreach (DataColumn dataColumn in posteriorData.Columns)
        {
          dataColumn.DefaultValue = NaN;
        }

        while (posteriorData.Rows.Count < _iterations + 1)
        {
          posteriorData.Rows.Add(posteriorData.NewRow());
        }

        var currentPosteriorRow = posteriorData.Rows[_currentIteration + 1]; // +1 'cos first row contains obs IV
        var allYs = outputObservations.Bind(o => _proposalOutputs[o.GetHashCode()].Current);
        currentPosteriorRow.ItemArray = allYs.Cast<object>().ToArray();
      }
    }

    private void UpdateModelOutputs()
    {
      var currentErrorDataRow = _errorData.Rows[_currentIteration];
      var isBurnIn = _currentIteration < _burnIn;
      var proposedLogLikelihood = 0d;

      // for each output propose perturbed error model and do M-H

      for (var i = 0; i < _modelOutputs.Length; ++i)
      {
        var modelOutput = _modelOutputs[i];
        var proposedModelOutput = modelOutput.GetPerturbed(isBurnIn);
        var outputObservations = _observations.Filter(o => o.Subject == proposedModelOutput.Name);
        var outputLogLikelihood = 0d;

        foreach (var observations in outputObservations)
        {
          var (_, currentPO) = _proposalOutputs[observations.GetHashCode()];
          outputLogLikelihood += proposedModelOutput.ErrorModel.GetLogLikelihood(currentPO, observations.Y);
        }

        var (_, currentLogLikelihood) = _outputLogLikelihoods[proposedModelOutput.Name];

        if (IsNaN(outputLogLikelihood))
        {
          proposedLogLikelihood += currentLogLikelihood;
          continue;
        }

        var ratio = Exp(outputLogLikelihood - currentLogLikelihood);
        var accept = Min(1d, ratio) > ContinuousUniform.Sample(Generator, 0d, 1d);

        if (accept)
        {
          _modelOutputs[i] = proposedModelOutput;
          proposedLogLikelihood += outputLogLikelihood;

          currentErrorDataRow[ToLLColumnName(proposedModelOutput.Name)] = outputLogLikelihood;
          currentErrorDataRow[ToAcceptColumnName(proposedModelOutput.Name)] = 1d;
        }
        else
        {
          proposedLogLikelihood += currentLogLikelihood;

          currentErrorDataRow[ToLLColumnName(proposedModelOutput.Name)] = currentLogLikelihood;
          currentErrorDataRow[ToAcceptColumnName(proposedModelOutput.Name)] = 0d;
        }
      }

      _currentLogLikelihood = proposedLogLikelihood;
    }

    private void AdjustForAcceptRate()
    {
      for (var i = 0; i < _modelParameters.Length; ++i)
      {
        var modelParameter = _modelParameters[i];
        var acceptColumnName = ToAcceptColumnName(modelParameter.Name);

        var nAccepts = Range(0, ACCEPT_RATE_ADJUST_INTERVAL).Sum(
          offset => _chainData.Rows[_currentIteration - offset].Field<double>(acceptColumnName)
          );

        RequireFalse(IsNaN(nAccepts));

        var acceptRate = nAccepts / ACCEPT_RATE_ADJUST_INTERVAL;

        // bias > 1 => accepting too many
        var bias = acceptRate / _targetAcceptRate;

        // we're using a narrow window so limit effect
        bias = Max(0.5, Min(2.0, bias));

        // high bias intended to widen proposal sampling space
        _modelParameters[i] = modelParameter.ApplyBias(bias);
      }

      for (var i = 0; i < _modelOutputs.Length; ++i)
      {
        var modelOutput = _modelOutputs[i];
        var acceptColumnName = ToAcceptColumnName(modelOutput.Name);

        var nAccepts = Range(0, ACCEPT_RATE_ADJUST_INTERVAL).Sum(
          offset => _errorData.Rows[_currentIteration - offset].Field<double>(acceptColumnName)
          );

        RequireFalse(IsNaN(nAccepts));

        var acceptRate = nAccepts / ACCEPT_RATE_ADJUST_INTERVAL;

        var bias = acceptRate / _targetAcceptRate;

        bias = Max(0.5, Min(2.0, bias));

        _modelOutputs[i] = modelOutput.ApplyBias(bias);
      }
    }

    private const string _logLikelihoodSuffix = "LL";
    private const string _acceptSuffix = "Accept";
    private const int ACCEPT_RATE_ADJUST_INTERVAL = 20;

    private readonly int _iterations;
    private readonly int _burnIn;
    private readonly double _targetAcceptRate;
    private readonly bool _approximate;
    private readonly ModelParameter[] _modelParameters;
    private readonly ModelOutput[] _modelOutputs;
    private readonly Arr<SimObservations> _observations;
    private readonly DataTable _chainData;
    private readonly DataTable _errorData;
    private readonly IDictionary<string, DataTable> _posteriorData;

    private int _currentIteration = NOT_FOUND;
    private double _currentLogLikelihood = NaN;
    private ModelParameter _currentProposal;
    private readonly IDictionary<int, (Arr<double> Previous, Arr<double> Current)> _proposalOutputs =
      new SortedDictionary<int, (Arr<double> Previous, Arr<double> Current)>();
    private readonly IDictionary<string, (double Previous, double Current)> _outputLogLikelihoods =
      new SortedDictionary<string, (double Previous, double Current)>();
    private readonly IDictionary<string, double> _priorDensities =
      new SortedDictionary<string, double>();
  }
}
