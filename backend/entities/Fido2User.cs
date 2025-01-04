using Fido2NetLib;
namespace backend.entities;

public static class UserExtensions
{
    public static Fido2User CreateFido2User(this User user)
    {
        return new Fido2User
        {
            Id = user.Id.ToByteArray(),
            Name = user.Name,
            DisplayName = user.Name
        };
    }
}