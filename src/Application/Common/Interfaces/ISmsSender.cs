namespace MySociety.Application.Common.Interfaces;

public interface ISmsSender
{
    Task SendAsync(string phone, string message, CancellationToken cancellationToken);
}
