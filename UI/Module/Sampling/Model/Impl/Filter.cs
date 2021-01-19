namespace Sampling
{
  internal sealed record Filter(
    string OutputName,
    double From,
    double To,
    int At,
    bool IsEnabled
    );
}
