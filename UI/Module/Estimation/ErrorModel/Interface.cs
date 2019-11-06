using System.Collections.Generic;

namespace Estimation
{
  internal interface IErrorModel
  {
    ErrorModelType ErrorModelType { get; }
    bool CanHandleNegativeSampleSpace { get; }
    bool IsConfigured { get; }
    double GetLogLikelihood(double mu, double x);
    double GetLogLikelihood(IEnumerable<double> mu, IEnumerable<double> x);
    IErrorModel GetPerturbed(bool isBurnIn);
    IErrorModel ApplyBias(double bias);
    string ToString(string variableName);
  }
}
