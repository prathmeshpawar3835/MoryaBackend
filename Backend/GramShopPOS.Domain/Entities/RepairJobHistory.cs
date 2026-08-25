using GramShopPOS.Domain.Enums;

namespace GramShopPOS.Domain.Entities;

public class RepairJobHistory : BaseEntity
{
    public int RepairJobId { get; set; }
    public RepairJobStatus Status { get; set; }
    public string? Notes { get; set; }
    public int UserId { get; set; }

    public RepairJob RepairJob { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}
