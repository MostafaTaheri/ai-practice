using AIPractice.Application.Exceptions;

namespace AIPractice.Application.Abstractions;

public interface IBusinessValidator
{
   Task<List<BusinessException>> ValidateAsync();
}
