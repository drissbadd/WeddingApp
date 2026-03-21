namespace WeddingApp.Models.ViewModels;

public class DashboardViewModel
{
    public int TotalGuests { get; set; }
    public int Confirmed { get; set; }
    public int Declined { get; set; }
    public int Pending { get; set; }
    public int InvitationsSent { get; set; }
    public int TotalAttending { get; set; } // confirmed + their plus ones
    public List<Guest> RecentRsvps { get; set; } = new();
    public Dictionary<string, int> ByCategory { get; set; } = new();
    public Dictionary<string, int> ByMeal { get; set; } = new();
    public Dictionary<string, (int Confirmed, int Pending, int Declined)> ByCategoryStatus { get; set; } = new();
}
