using LanguageExt;
using MailKit.Security;

namespace RVisUI.Model
{
  public sealed record RunControlJob(
    string Name,
    string File,
    RunControlConfiguration Configuration,
    RunControlNotification? Notification,
    Arr<IRunControlTask> Tasks,
    byte[] SpecHash
    );

  public sealed record RunControlConfiguration(
    string Title,
    string Simulation,
    int MaxCores
    );

  public sealed record RunControlNotification(
    string EmailAddress,
    string Password,
    string SmtpAddress,
    int SmtpPort,
    SecureSocketOptions SecureSocket
    );
}
