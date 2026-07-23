namespace KadirliApp.Domain.Entities;

public class UserNeighborhood
{
    public Guid UserId { get; set; }
    public User User { get; set; } = default!;

    public Guid NeighborhoodId { get; set; }
    public Neighborhood Neighborhood { get; set; } = default!;
}
