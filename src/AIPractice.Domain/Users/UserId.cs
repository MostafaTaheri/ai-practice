using AIPractice.Domain.Abstractions;

namespace AIPractice.Domain.Users;

public class UserId : TypedIdValueBase
{
    public UserId(Guid value) : base(value)
    {
    }

    public static UserId New() => new(SequentialGuidGenerator.Generate());
}
