using Remora.Results;

namespace Sniffer.KillBoard.Errors
{
    internal record NotConfigurableError(string Message) : ResultError(Message);
}
