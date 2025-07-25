using AutoMapper;
using SmartTelehealth.Core.Entities;
using SmartTelehealth.Application.DTOs;

namespace SmartTelehealth.Application.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // User mappings
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString()))
            .ForMember(dest => dest.UserRoleId, opt => opt.MapFrom(src => src.UserRoleId.ToString()));
        
        CreateMap<UserDto, User>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.Parse(src.Id)))
            .ForMember(dest => dest.UserRoleId, opt => opt.MapFrom(src => Guid.Parse(src.UserRoleId)));

        // Appointment mappings
        CreateMap<Appointment, AppointmentDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString()))
            .ForMember(dest => dest.PatientId, opt => opt.MapFrom(src => src.PatientId.ToString()))
            .ForMember(dest => dest.ProviderId, opt => opt.MapFrom(src => src.ProviderId.ToString()))
            .ForMember(dest => dest.CategoryId, opt => opt.MapFrom(src => src.CategoryId.ToString()))
            .ForMember(dest => dest.SubscriptionId, opt => opt.MapFrom(src => src.SubscriptionId.ToString()))
            .ForMember(dest => dest.ConsultationId, opt => opt.MapFrom(src => src.ConsultationId.ToString()))
            .ForMember(dest => dest.AppointmentTypeId, opt => opt.MapFrom(src => src.AppointmentTypeId))
            .ForMember(dest => dest.ConsultationModeId, opt => opt.MapFrom(src => src.ConsultationModeId));

        // Subscription mappings
        CreateMap<Subscription, SubscriptionDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString()))
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId.ToString()))
            .ForMember(dest => dest.PlanId, opt => opt.MapFrom(src => src.SubscriptionPlanId.ToString()))
            .ForMember(dest => dest.PlanName, opt => opt.MapFrom(src => src.SubscriptionPlan.Name))
            .ForMember(dest => dest.PlanDescription, opt => opt.MapFrom(src => src.SubscriptionPlan.Description))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
            .ForMember(dest => dest.StatusReason, opt => opt.MapFrom(src => src.StatusReason))
            .ForMember(dest => dest.CurrentPrice, opt => opt.MapFrom(src => src.CurrentPrice))
            .ForMember(dest => dest.AutoRenew, opt => opt.MapFrom(src => src.AutoRenew))
            .ForMember(dest => dest.Notes, opt => opt.MapFrom(src => src.Notes))
            .ForMember(dest => dest.StartDate, opt => opt.MapFrom(src => src.StartDate))
            .ForMember(dest => dest.EndDate, opt => opt.MapFrom(src => src.EndDate))
            .ForMember(dest => dest.NextBillingDate, opt => opt.MapFrom(src => src.NextBillingDate))
            .ForMember(dest => dest.PausedDate, opt => opt.MapFrom(src => src.PausedDate))
            .ForMember(dest => dest.ResumedDate, opt => opt.MapFrom(src => src.ResumedDate))
            .ForMember(dest => dest.CancelledDate, opt => opt.MapFrom(src => src.CancelledDate))
            .ForMember(dest => dest.ExpirationDate, opt => opt.MapFrom(src => src.ExpirationDate))
            .ForMember(dest => dest.CancellationReason, opt => opt.MapFrom(src => src.CancellationReason))
            .ForMember(dest => dest.PauseReason, opt => opt.MapFrom(src => src.PauseReason))
            .ForMember(dest => dest.StripeSubscriptionId, opt => opt.MapFrom(src => src.StripeSubscriptionId))
            .ForMember(dest => dest.StripeCustomerId, opt => opt.MapFrom(src => src.StripeCustomerId))
            .ForMember(dest => dest.PaymentMethodId, opt => opt.MapFrom(src => src.PaymentMethodId))
            .ForMember(dest => dest.LastPaymentDate, opt => opt.MapFrom(src => src.LastPaymentDate))
            .ForMember(dest => dest.LastPaymentFailedDate, opt => opt.MapFrom(src => src.LastPaymentFailedDate))
            .ForMember(dest => dest.LastPaymentError, opt => opt.MapFrom(src => src.LastPaymentError))
            .ForMember(dest => dest.FailedPaymentAttempts, opt => opt.MapFrom(src => src.FailedPaymentAttempts))
            .ForMember(dest => dest.IsTrialSubscription, opt => opt.MapFrom(src => src.IsTrialSubscription))
            .ForMember(dest => dest.TrialStartDate, opt => opt.MapFrom(src => src.TrialStartDate))
            .ForMember(dest => dest.TrialEndDate, opt => opt.MapFrom(src => src.TrialEndDate))
            .ForMember(dest => dest.TrialDurationInDays, opt => opt.MapFrom(src => src.TrialDurationInDays))
            .ForMember(dest => dest.LastUsedDate, opt => opt.MapFrom(src => src.LastUsedDate))
            .ForMember(dest => dest.TotalUsageCount, opt => opt.MapFrom(src => src.TotalUsageCount))
            .ForMember(dest => dest.StatusHistory, opt => opt.MapFrom(src => src.StatusHistory))
            .ForMember(dest => dest.Payments, opt => opt.MapFrom(src => src.Payments))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
            .ForMember(dest => dest.IsPaused, opt => opt.MapFrom(src => src.IsPaused))
            .ForMember(dest => dest.IsCancelled, opt => opt.MapFrom(src => src.IsCancelled))
            .ForMember(dest => dest.IsExpired, opt => opt.MapFrom(src => src.IsExpired))
            .ForMember(dest => dest.HasPaymentIssues, opt => opt.MapFrom(src => src.HasPaymentIssues))
            .ForMember(dest => dest.IsInTrial, opt => opt.MapFrom(src => src.IsInTrial))
            .ForMember(dest => dest.DaysUntilNextBilling, opt => opt.MapFrom(src => src.DaysUntilNextBilling))
            .ForMember(dest => dest.IsNearExpiration, opt => opt.MapFrom(src => src.IsNearExpiration))
            .ForMember(dest => dest.CanPause, opt => opt.MapFrom(src => src.CanPause))
            .ForMember(dest => dest.CanResume, opt => opt.MapFrom(src => src.CanResume))
            .ForMember(dest => dest.CanCancel, opt => opt.MapFrom(src => src.CanCancel))
            .ForMember(dest => dest.CanRenew, opt => opt.MapFrom(src => src.CanRenew))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt));
        CreateMap<SubscriptionStatusHistory, SubscriptionStatusHistoryDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString()))
            .ForMember(dest => dest.SubscriptionId, opt => opt.MapFrom(src => src.SubscriptionId.ToString()))
            .ForMember(dest => dest.FromStatus, opt => opt.MapFrom(src => src.FromStatus))
            .ForMember(dest => dest.ToStatus, opt => opt.MapFrom(src => src.ToStatus))
            .ForMember(dest => dest.Reason, opt => opt.MapFrom(src => src.Reason))
            .ForMember(dest => dest.ChangedByUserId, opt => opt.MapFrom(src => src.ChangedByUserId.HasValue ? src.ChangedByUserId.Value.ToString() : null))
            .ForMember(dest => dest.ChangedAt, opt => opt.MapFrom(src => src.ChangedAt))
            .ForMember(dest => dest.Metadata, opt => opt.MapFrom(src => src.Metadata));
        CreateMap<SubscriptionPayment, SubscriptionPaymentDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString()))
            .ForMember(dest => dest.SubscriptionId, opt => opt.MapFrom(src => src.SubscriptionId.ToString()))
            .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.Amount))
            .ForMember(dest => dest.TaxAmount, opt => opt.MapFrom(src => src.TaxAmount))
            .ForMember(dest => dest.NetAmount, opt => opt.MapFrom(src => src.NetAmount))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString()))
            .ForMember(dest => dest.FailureReason, opt => opt.MapFrom(src => src.FailureReason))
            .ForMember(dest => dest.DueDate, opt => opt.MapFrom(src => src.DueDate))
            .ForMember(dest => dest.PaidAt, opt => opt.MapFrom(src => src.PaidAt))
            .ForMember(dest => dest.FailedAt, opt => opt.MapFrom(src => src.FailedAt))
            .ForMember(dest => dest.BillingPeriodStart, opt => opt.MapFrom(src => src.BillingPeriodStart))
            .ForMember(dest => dest.BillingPeriodEnd, opt => opt.MapFrom(src => src.BillingPeriodEnd))
            .ForMember(dest => dest.StripePaymentIntentId, opt => opt.MapFrom(src => src.StripePaymentIntentId))
            .ForMember(dest => dest.StripeInvoiceId, opt => opt.MapFrom(src => src.StripeInvoiceId))
            .ForMember(dest => dest.ReceiptUrl, opt => opt.MapFrom(src => src.ReceiptUrl))
            .ForMember(dest => dest.PaymentIntentId, opt => opt.MapFrom(src => src.PaymentIntentId))
            .ForMember(dest => dest.InvoiceId, opt => opt.MapFrom(src => src.InvoiceId))
            .ForMember(dest => dest.AttemptCount, opt => opt.MapFrom(src => src.AttemptCount))
            .ForMember(dest => dest.NextRetryAt, opt => opt.MapFrom(src => src.NextRetryAt))
            .ForMember(dest => dest.RefundedAmount, opt => opt.MapFrom(src => src.RefundedAmount))
            .ForMember(dest => dest.Refunds, opt => opt.MapFrom(src => src.Refunds))
            .ForMember(dest => dest.IsPaid, opt => opt.MapFrom(src => src.IsPaid))
            .ForMember(dest => dest.IsFailed, opt => opt.MapFrom(src => src.IsFailed))
            .ForMember(dest => dest.IsRefunded, opt => opt.MapFrom(src => src.IsRefunded))
            .ForMember(dest => dest.IsOverdue, opt => opt.MapFrom(src => src.IsOverdue))
            .ForMember(dest => dest.RemainingAmount, opt => opt.MapFrom(src => src.RemainingAmount))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt));
        CreateMap<PaymentRefund, PaymentRefundDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString()))
            .ForMember(dest => dest.SubscriptionPaymentId, opt => opt.MapFrom(src => src.SubscriptionPaymentId.ToString()))
            .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.Amount))
            .ForMember(dest => dest.Reason, opt => opt.MapFrom(src => src.Reason))
            .ForMember(dest => dest.StripeRefundId, opt => opt.MapFrom(src => src.StripeRefundId))
            .ForMember(dest => dest.RefundedAt, opt => opt.MapFrom(src => src.RefundedAt))
            .ForMember(dest => dest.ProcessedByUserId, opt => opt.MapFrom(src => src.ProcessedByUserId.HasValue ? src.ProcessedByUserId.Value.ToString() : null));
        // Plan mappings
        CreateMap<SubscriptionPlan, SubscriptionPlanDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString()))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.ShortDescription, opt => opt.MapFrom(src => src.ShortDescription))
            .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Price))
            .ForMember(dest => dest.DiscountedPrice, opt => opt.MapFrom(src => src.DiscountedPrice))
            .ForMember(dest => dest.DiscountValidUntil, opt => opt.MapFrom(src => src.DiscountValidUntil))
            .ForMember(dest => dest.BillingCycleId, opt => opt.MapFrom(src => src.BillingCycleId))
            .ForMember(dest => dest.CurrencyId, opt => opt.MapFrom(src => src.CurrencyId))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
            .ForMember(dest => dest.IsFeatured, opt => opt.MapFrom(src => src.IsFeatured))
            .ForMember(dest => dest.IsTrialAllowed, opt => opt.MapFrom(src => src.IsTrialAllowed))
            .ForMember(dest => dest.TrialDurationInDays, opt => opt.MapFrom(src => src.TrialDurationInDays))
            .ForMember(dest => dest.DisplayOrder, opt => opt.MapFrom(src => src.DisplayOrder))
            .ForMember(dest => dest.StripeProductId, opt => opt.MapFrom(src => src.StripeProductId))
            .ForMember(dest => dest.StripeMonthlyPriceId, opt => opt.MapFrom(src => src.StripeMonthlyPriceId))
            .ForMember(dest => dest.StripeQuarterlyPriceId, opt => opt.MapFrom(src => src.StripeQuarterlyPriceId))
            .ForMember(dest => dest.StripeAnnualPriceId, opt => opt.MapFrom(src => src.StripeAnnualPriceId))
            .ForMember(dest => dest.Features, opt => opt.MapFrom(src => src.Features))
            .ForMember(dest => dest.Terms, opt => opt.MapFrom(src => src.Terms))
            .ForMember(dest => dest.EffectiveDate, opt => opt.MapFrom(src => src.EffectiveDate))
            .ForMember(dest => dest.ExpirationDate, opt => opt.MapFrom(src => src.ExpirationDate))
            .ForMember(dest => dest.EffectivePrice, opt => opt.MapFrom(src => src.EffectivePrice))
            .ForMember(dest => dest.HasActiveDiscount, opt => opt.MapFrom(src => src.HasActiveDiscount))
            .ForMember(dest => dest.IsCurrentlyAvailable, opt => opt.MapFrom(src => src.IsCurrentlyAvailable))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt));

        // Chat mappings
        CreateMap<ChatRoom, ChatRoomDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString()));

        CreateMap<Message, MessageDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString()))
            .ForMember(dest => dest.ChatRoomId, opt => opt.MapFrom(src => src.ChatRoomId.ToString()))
            .ForMember(dest => dest.SenderId, opt => opt.MapFrom(src => src.SenderId.ToString()));

        // Notification mappings
        CreateMap<Notification, NotificationDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString()))
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId.ToString()));
    }
} 