using TicketSystem.Shared.DTO;

namespace TicketSystem.Web.POCO;

public sealed class CustomerPOCO : AppUserDTO
{
    public CustomerPOCO()
    {
    }

    public CustomerPOCO(AppUserDTO appUser)
    {
        Id = appUser.Id;
        Email = appUser.Email;
        UserTypeId = appUser.UserTypeId;
        CreatedAt = appUser.CreatedAt;
        UpdatedAt = appUser.UpdatedAt;
        UpdatedByUserId = appUser.UpdatedByUserId;
    }

    public string UpdatedByUserEmail { get; set; } = string.Empty;
}
