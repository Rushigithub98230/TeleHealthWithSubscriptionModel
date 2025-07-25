using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartTelehealth.Application.DTOs;

namespace SmartTelehealth.Application.Interfaces;

public interface IAppointmentService
{
    Task<ApiResponse<AppointmentDto>> CreateAppointmentAsync(CreateAppointmentDto createDto);
    Task<ApiResponse<AppointmentDto>> GetAppointmentByIdAsync(Guid id);
    Task<ApiResponse<IEnumerable<AppointmentDto>>> GetPatientAppointmentsAsync(Guid patientId);
    Task<ApiResponse<IEnumerable<AppointmentDto>>> GetProviderAppointmentsAsync(Guid providerId);
    Task<ApiResponse<IEnumerable<AppointmentDto>>> GetPendingAppointmentsAsync();
    Task<ApiResponse<AppointmentDto>> UpdateAppointmentAsync(Guid id, UpdateAppointmentDto updateDto);
    Task<ApiResponse<bool>> DeleteAppointmentAsync(Guid id);
    Task<ApiResponse<AppointmentDto>> BookAppointmentAsync(BookAppointmentDto bookDto);
    Task<ApiResponse<AppointmentDto>> ProcessPaymentAsync(Guid appointmentId, ProcessPaymentDto paymentDto);
    Task<ApiResponse<AppointmentDto>> ProviderAcceptAppointmentAsync(Guid appointmentId, ProviderAcceptDto acceptDto);
    Task<ApiResponse<AppointmentDto>> ProviderRejectAppointmentAsync(Guid appointmentId, ProviderRejectDto rejectDto);
    Task<ApiResponse<AppointmentDto>> StartMeetingAsync(Guid appointmentId);
    Task<ApiResponse<AppointmentDto>> EndMeetingAsync(Guid appointmentId);
    Task<ApiResponse<AppointmentDto>> CompleteAppointmentAsync(Guid appointmentId, CompleteAppointmentDto completeDto);
    Task<ApiResponse<AppointmentDto>> CancelAppointmentAsync(Guid appointmentId, string reason);
    Task<ApiResponse<string>> GenerateMeetingLinkAsync(Guid appointmentId);
    Task<ApiResponse<string>> GetOpenTokTokenAsync(Guid appointmentId, Guid userId);
    Task<ApiResponse<bool>> StartRecordingAsync(Guid appointmentId);
    Task<ApiResponse<bool>> StopRecordingAsync(Guid appointmentId);
    Task<ApiResponse<string>> GetRecordingUrlAsync(Guid appointmentId);
    Task<ApiResponse<bool>> CapturePaymentAsync(Guid appointmentId);
    Task<ApiResponse<bool>> RefundPaymentAsync(Guid appointmentId, decimal? amount = null);
    Task<ApiResponse<PaymentStatusDto>> GetPaymentStatusAsync(Guid appointmentId);
    Task<ApiResponse<AppointmentDocumentDto>> UploadDocumentAsync(Guid appointmentId, UploadDocumentDto uploadDto);
    Task<ApiResponse<IEnumerable<AppointmentDocumentDto>>> GetAppointmentDocumentsAsync(Guid appointmentId);
    Task<ApiResponse<bool>> DeleteDocumentAsync(Guid documentId);
    Task<ApiResponse<AppointmentReminderDto>> ScheduleReminderAsync(Guid appointmentId, ScheduleReminderDto reminderDto);
    Task<ApiResponse<IEnumerable<AppointmentReminderDto>>> GetAppointmentRemindersAsync(Guid appointmentId);
    Task<ApiResponse<bool>> SendReminderAsync(Guid reminderId);
    Task<ApiResponse<bool>> LogAppointmentEventAsync(Guid appointmentId, LogAppointmentEventDto eventDto);
    Task<ApiResponse<IEnumerable<AppointmentEventDto>>> GetAppointmentEventsAsync(Guid appointmentId);
    Task<ApiResponse<IEnumerable<ProviderAvailabilityDto>>> GetProviderAvailabilityAsync(Guid providerId, DateTime date);
    Task<ApiResponse<bool>> CheckProviderAvailabilityAsync(Guid providerId, DateTime startTime, DateTime endTime);
    Task<ApiResponse<bool>> ValidateSubscriptionAccessAsync(Guid patientId, Guid categoryId);
    Task<ApiResponse<decimal>> CalculateAppointmentFeeAsync(Guid patientId, Guid providerId, Guid categoryId);
    Task<ApiResponse<bool>> ApplySubscriptionDiscountAsync(Guid appointmentId);
    Task<ApiResponse<bool>> ProcessExpiredAppointmentsAsync();
    Task<ApiResponse<bool>> AutoCancelAppointmentAsync(Guid appointmentId);
    Task<ApiResponse<AppointmentAnalyticsDto>> GetAppointmentAnalyticsAsync(DateTime startDate, DateTime endDate);
    Task<ApiResponse<IEnumerable<AppointmentDto>>> GetAppointmentsByStatusAsync(Guid appointmentStatusId);
    Task<ApiResponse<IEnumerable<AppointmentDto>>> GetUpcomingAppointmentsAsync();
    Task<ApiResponse<AppointmentParticipantDto>> AddParticipantAsync(Guid appointmentId, Guid? userId, string? email, string? phone, Guid participantRoleId, Guid invitedByUserId);
    Task<ApiResponse<AppointmentInvitationDto>> InviteExternalAsync(Guid appointmentId, string email, string? phone, string? message, Guid invitedByUserId);
    Task<ApiResponse<bool>> MarkParticipantJoinedAsync(Guid appointmentId, Guid? userId, string? email);
    Task<ApiResponse<bool>> MarkParticipantLeftAsync(Guid appointmentId, Guid? userId, string? email);
    Task<ApiResponse<IEnumerable<AppointmentParticipantDto>>> GetParticipantsAsync(Guid appointmentId);
    Task<string> GenerateVideoTokenAsync(Guid appointmentId, Guid? userId, string? email, Guid participantRoleId);
    Task<ApiResponse<AppointmentPaymentLogDto>> CreatePaymentLogAsync(Guid appointmentId, Guid userId, decimal amount, string paymentMethod, string? paymentIntentId = null, string? sessionId = null);
    Task<ApiResponse<AppointmentPaymentLogDto>> UpdatePaymentStatusAsync(Guid paymentLogId, Guid paymentStatusId, string? failureReason = null);
    Task<ApiResponse<AppointmentPaymentLogDto>> ProcessRefundAsync(Guid appointmentId, decimal refundAmount, string reason);
    Task<ApiResponse<IEnumerable<AppointmentPaymentLogDto>>> GetPaymentLogsAsync(Guid appointmentId);
    Task<ApiResponse<AppointmentDto>> ConfirmPaymentAsync(Guid appointmentId, string paymentIntentId);
    Task<ApiResponse<AppointmentDto>> ProviderActionAsync(Guid appointmentId, string action, string? notes = null);
    Task<ApiResponse<bool>> IsAppointmentServiceHealthyAsync();
    Task<ApiResponse<IEnumerable<CategoryWithSubscriptionsDto>>> GetCategoriesWithSubscriptionsAsync();
    Task<ApiResponse<IEnumerable<FeaturedProviderDto>>> GetFeaturedProvidersAsync();
} 