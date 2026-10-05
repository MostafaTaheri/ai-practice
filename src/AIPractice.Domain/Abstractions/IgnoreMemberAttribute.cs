namespace AIPractice.Domain.Abstractions;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class IgnoreMemberAttribute : Attribute
{
}