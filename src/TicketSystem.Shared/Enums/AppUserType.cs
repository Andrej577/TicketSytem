using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Shared.Enums;

public enum AppUserType
{
    [Display(Name = "Customer")]
    Customer = 1,

    [Display(Name = "Operator")]
    Operator = 2,

    [Display(Name = "Administrator")]
    Administrator = 3
}