using System.ComponentModel.DataAnnotations;

namespace SmartTelehealth.Core.Entities;

public class Consultation : BaseEntity
{
    public enum ConsultationStatus
    {
        Scheduled,
        InProgress,
        Completed,
        Cancelled,
        NoShow
    }
    
    public enum ConsultationType
    {
        Initial,
        FollowUp,
        Emergency,
        Routine
    }
    
    // Foreign keys
    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;
    
    public Guid ProviderId { get; set; }
    public virtual Provider Provider { get; set; } = null!;
    
    public Guid CategoryId { get; set; }
    public virtual Category Category { get; set; } = null!;
    
    public Guid? SubscriptionId { get; set; }
    public virtual Subscription? Subscription { get; set; }
    
    public Guid? HealthAssessmentId { get; set; }
    public virtual HealthAssessment? HealthAssessment { get; set; }
    
    // Consultation details
    public ConsultationStatus Status { get; set; } = ConsultationStatus.Scheduled;
    
    public ConsultationType Type { get; set; } = ConsultationType.Initial;
    
    public DateTime ScheduledAt { get; set; }
    
    public DateTime? StartedAt { get; set; }
    
    public DateTime? EndedAt { get; set; }
    
    public int DurationMinutes { get; set; } = 30;
    
    public decimal Fee { get; set; }
    
    [MaxLength(500)]
    public string? MeetingUrl { get; set; }
    
    [MaxLength(100)]
    public string? MeetingId { get; set; }
    
    [MaxLength(1000)]
    public string? Notes { get; set; }
    
    [MaxLength(1000)]
    public string? Diagnosis { get; set; }
    
    [MaxLength(1000)]
    public string? TreatmentPlan { get; set; }
    
    [MaxLength(1000)]
    public string? Prescriptions { get; set; }
    
    public bool RequiresFollowUp { get; set; }
    
    public DateTime? FollowUpDate { get; set; }
    
    public bool IsOneTime { get; set; } = false;
    
    [MaxLength(500)]
    public string? CancellationReason { get; set; }
    
    // Navigation properties
    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
    public virtual ICollection<MedicationDelivery> MedicationDeliveries { get; set; } = new List<MedicationDelivery>();
    
    public bool IsCompleted => Status == ConsultationStatus.Completed;
    public bool IsCancelled => Status == ConsultationStatus.Cancelled;
    public bool IsNoShow => Status == ConsultationStatus.NoShow;
    
    public TimeSpan? ActualDuration => StartedAt.HasValue && EndedAt.HasValue 
        ? EndedAt.Value - StartedAt.Value 
        : null;
} 