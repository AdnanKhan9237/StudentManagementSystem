using System.ComponentModel.DataAnnotations;

namespace StudentManagementSystem.ViewModels;

public class RoomViewModel
{
    public int Id { get; set; }
    
    [Required]
    [Display(Name = "Room Number")]
    public string RoomNumber { get; set; } = string.Empty;
    
    [Required]
    [Display(Name = "Room Name")]
    public string RoomName { get; set; } = string.Empty;
    
    [Required]
    [Range(1, 1000)]
    public int Capacity { get; set; }
    
    [Required]
    [Display(Name = "Room Type")]
    public string RoomType { get; set; } = string.Empty;
    
    [Required]
    public string Building { get; set; } = string.Empty;
    
    public string? Floor { get; set; }
    
    public string? Equipment { get; set; }
    
    [Display(Name = "Has Projector")]
    public bool HasProjector { get; set; }
    
    [Display(Name = "Has Computers")]
    public bool HasComputers { get; set; }
    
    [Display(Name = "Has Air Conditioning")]
    public bool HasAirConditioning { get; set; }
    
    // Display properties
    public DateTime CreatedDate { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ModifiedDate { get; set; }
    public string? ModifiedBy { get; set; }
    public int CurrentBatches { get; set; }
    // Navigation properties for display
    public List<BatchSummaryViewModel> AssignedBatches { get; set; } = new();
}

public class RoomListViewModel
{
    public List<RoomViewModel> Rooms { get; set; } = new();
    public string SearchTerm { get; set; } = string.Empty;
    public string TypeFilter { get; set; } = string.Empty;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 15;
    public int TotalRecords { get; set; }
    public List<string> RoomTypes { get; set; } = new();
}