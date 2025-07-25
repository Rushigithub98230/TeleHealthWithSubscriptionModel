using System.ComponentModel.DataAnnotations;

namespace SmartTelehealth.Core.Entities;

public class ChatRoom : BaseEntity
{
    public enum ChatRoomType
    {
        PatientProvider,
        Group,
        Support,
        Consultation
    }

    public enum ChatRoomStatus
    {
        Active,
        Paused,
        Archived,
        Deleted
    }

    // Chat room details
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public ChatRoomType Type { get; set; } = ChatRoomType.PatientProvider;

    public ChatRoomStatus Status { get; set; } = ChatRoomStatus.Active;

    // Foreign keys for different chat types
    public Guid? PatientId { get; set; }
    public virtual User? Patient { get; set; }

    public Guid? ProviderId { get; set; }
    public virtual Provider? Provider { get; set; }

    public Guid? SubscriptionId { get; set; }
    public virtual Subscription? Subscription { get; set; }

    public Guid? ConsultationId { get; set; }
    public virtual Consultation? Consultation { get; set; }

    // Security and compliance
    public bool IsEncrypted { get; set; } = true;
    public string? EncryptionKey { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public bool AllowFileSharing { get; set; } = true;
    public bool AllowVoiceCalls { get; set; } = true;
    public bool AllowVideoCalls { get; set; } = true;

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;

    // Navigation properties
    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
    public virtual ICollection<ChatRoomParticipant> Participants { get; set; } = new List<ChatRoomParticipant>();
} 