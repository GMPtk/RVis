using System;

namespace RVis.Model
{
  public sealed class ServerLicense : IDisposable
  {
    public readonly static ServerLicense NullLicense = new ServerLicense();

    internal ServerLicense(IRVisClient client, Action<ServerLicense> expireLicense)
    {
      Client = client;
      _expireLicense = expireLicense;
    }

    public IRVisClient Client { get; }

    public void Dispose(bool disposing)
    {
      if (!_disposed)
      {
        if (disposing)
        {
          _expireLicense(this);
        }

        _disposed = true;
      }
    }

    public void Dispose() => Dispose(true);

    private ServerLicense() { }

    private bool _disposed = false;
    private readonly Action<ServerLicense> _expireLicense;
  }
}
