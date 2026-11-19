namespace RemitMon.Infrastructure.Adapters;

public sealed class NotConfiguredException : InvalidOperationException
{
    public NotConfiguredException(string message) : base(message) { }
}
