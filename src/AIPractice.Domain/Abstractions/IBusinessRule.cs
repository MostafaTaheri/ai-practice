using AIPractice.Domain.Exceptions;

namespace AIPractice.Domain.Abstractions;

public interface IBusinessRule
{
    bool IsBroken();
    Task<bool> IsBrokenAsync(CancellationToken cancellationToken = default) => Task.FromResult(IsBroken());
    BaseException GetRelatedException();
}