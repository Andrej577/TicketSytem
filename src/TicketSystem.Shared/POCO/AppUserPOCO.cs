using TicketSystem.Shared.DTO;
using TicketSystem.Shared.Enums;

namespace TicketSystem.Shared.POCO;

public sealed class AppUserPOCO : AppUserDTO
{
    public AppUserPOCO()
    {
    }

    public AppUserPOCO(AppUserDTO appUser)
    {
        Id = appUser.Id;
        Email = appUser.Email;
        FirstName = appUser.FirstName;
        LastName = appUser.LastName;
        UserTypeId = appUser.UserTypeId;
        CreatedAt = appUser.CreatedAt;
        UpdatedAt = appUser.UpdatedAt;
        UpdatedByUserId = appUser.UpdatedByUserId;
    }

    public string UpdatedByUserEmail { get; set; } = string.Empty;

    public string UserTypeName => ((AppUserType)UserTypeId).ToString();
}
