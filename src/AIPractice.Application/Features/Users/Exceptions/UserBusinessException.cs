using AIPractice.Application.Exceptions;
using AIPractice.Domain.Users;

namespace AIPractice.Application.Features.Users.Exceptions;

public abstract class BaseBusinessUserException : BusinessException
{
    public BaseBusinessUserException(string code, string message, List<string> details) : base(
       code: $"{nameof(User)}-{code}",
       message: message,
       details: details)
    { }

    public sealed class UserValidationException : BaseBusinessUserException
    {
        public UserValidationException(string message, List<string>? details = null) : base(code: "100", message: message, details: details ?? []) { }
    }
}