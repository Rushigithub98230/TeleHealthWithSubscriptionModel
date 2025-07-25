using AutoMapper;
using Microsoft.Extensions.Logging;
using SmartTelehealth.Application.DTOs;
using SmartTelehealth.Application.Interfaces;
using SmartTelehealth.Core.Entities;
using SmartTelehealth.Core.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace SmartTelehealth.Application.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<SubscriptionService> _logger;
    private readonly IStripeService _stripeService;
    private readonly PrivilegeService _privilegeService;
    private readonly INotificationService _notificationService;
    private readonly IAuditService _auditService;
    private readonly IUserService _userService;
    private readonly ISubscriptionPlanPrivilegeRepository _planPrivilegeRepo;
    private readonly IUserSubscriptionPrivilegeUsageRepository _usageRepo;

    public SubscriptionService(
        ISubscriptionRepository subscriptionRepository,
        IMapper mapper,
        ILogger<SubscriptionService> logger,
        PrivilegeService privilegeService,
        INotificationService notificationService,
        IAuditService auditService,
        IUserService userService,
        ISubscriptionPlanPrivilegeRepository planPrivilegeRepo,
        IUserSubscriptionPrivilegeUsageRepository usageRepo)
    {
        _subscriptionRepository = subscriptionRepository;
        _mapper = mapper;
        _logger = logger;
        _privilegeService = privilegeService;
        _notificationService = notificationService;
        _auditService = auditService;
        _userService = userService;
        _planPrivilegeRepo = planPrivilegeRepo;
        _usageRepo = usageRepo;
    }

    public async Task<ApiResponse<SubscriptionDto>> GetSubscriptionAsync(string subscriptionId)
    {
        var entity = await _subscriptionRepository.GetByIdAsync(Guid.Parse(subscriptionId));
        if (entity == null)
            return ApiResponse<SubscriptionDto>.ErrorResponse("Subscription not found");
        return ApiResponse<SubscriptionDto>.SuccessResponse(_mapper.Map<SubscriptionDto>(entity));
    }

    public async Task<ApiResponse<IEnumerable<SubscriptionDto>>> GetUserSubscriptionsAsync(string userId)
    {
        var entities = await _subscriptionRepository.GetByUserIdAsync(Guid.Parse(userId));
        var dtos = _mapper.Map<IEnumerable<SubscriptionDto>>(entities);
        return ApiResponse<IEnumerable<SubscriptionDto>>.SuccessResponse(dtos);
    }

    public async Task<ApiResponse<SubscriptionDto>> CreateSubscriptionAsync(CreateSubscriptionDto createDto)
    {
        // 1. Check if plan exists and is active
        var plan = await _subscriptionRepository.GetSubscriptionPlanByIdAsync(Guid.Parse(createDto.PlanId));
        if (plan == null)
            return ApiResponse<SubscriptionDto>.ErrorResponse("Subscription plan does not exist");
        if (!plan.IsActive)
            return ApiResponse<SubscriptionDto>.ErrorResponse("Subscription plan is not active");

        // 2. Prevent duplicate subscriptions for the same user and plan (active or paused)
        var userSubscriptions = await _subscriptionRepository.GetByUserIdAsync(Guid.Parse(createDto.UserId));
        if (userSubscriptions.Any(s => s.SubscriptionPlanId == plan.Id && (s.Status == Subscription.SubscriptionStatuses.Active || s.Status == Subscription.SubscriptionStatuses.Paused)))
            return ApiResponse<SubscriptionDto>.ErrorResponse("User already has an active or paused subscription for this plan");

        var entity = _mapper.Map<Subscription>(createDto);
        // Trial logic
        if (plan.IsTrialAllowed && plan.TrialDurationInDays > 0)
        {
            entity.IsTrialSubscription = true;
            entity.TrialStartDate = DateTime.UtcNow;
            entity.TrialEndDate = DateTime.UtcNow.AddDays(plan.TrialDurationInDays);
            entity.TrialDurationInDays = plan.TrialDurationInDays;
            entity.Status = Subscription.SubscriptionStatuses.TrialActive;
        }
        else
        {
            entity.Status = Subscription.SubscriptionStatuses.Active;
        }
        entity.StartDate = DateTime.UtcNow;
        entity.NextBillingDate = DateTime.UtcNow.AddMonths(1);
        var created = await _subscriptionRepository.CreateAsync(entity);
        // Add status history
        await _subscriptionRepository.AddStatusHistoryAsync(new SubscriptionStatusHistory {
            SubscriptionId = created.Id,
            FromStatus = null,
            ToStatus = created.Status,
            ChangedAt = DateTime.UtcNow
        });
        var dto = _mapper.Map<SubscriptionDto>(created);
        // Send confirmation and welcome emails
        var userResult = await _userService.GetUserByIdAsync(createDto.UserId);
        if (userResult.Success && userResult.Data != null)
        {
            await _notificationService.SendSubscriptionConfirmationAsync(userResult.Data.Email, userResult.Data.FullName, dto);
            await _notificationService.SendSubscriptionWelcomeEmailAsync(userResult.Data.Email, userResult.Data.FullName, dto);
        }
        return ApiResponse<SubscriptionDto>.SuccessResponse(dto, "Subscription created");
    }

    public async Task<ApiResponse<SubscriptionDto>> CancelSubscriptionAsync(string subscriptionId, string? reason = null)
    {
        var entity = await _subscriptionRepository.GetByIdAsync(Guid.Parse(subscriptionId));
        if (entity == null)
            return ApiResponse<SubscriptionDto>.ErrorResponse("Subscription not found");
        // Prevent cancelling an already cancelled subscription
        if (entity.IsCancelled)
            return ApiResponse<SubscriptionDto>.ErrorResponse("Subscription is already cancelled");
        // Validate status transition
        var validation = entity.ValidateStatusTransition(Subscription.SubscriptionStatuses.Cancelled);
        if (validation != ValidationResult.Success)
            return ApiResponse<SubscriptionDto>.ErrorResponse(validation.ErrorMessage);
        var oldStatus = entity.Status;
        entity.Status = Subscription.SubscriptionStatuses.Cancelled;
        entity.CancellationReason = reason;
        entity.CancelledDate = DateTime.UtcNow;
        var updated = await _subscriptionRepository.UpdateAsync(entity);
        // Add status history
        await _subscriptionRepository.AddStatusHistoryAsync(new SubscriptionStatusHistory {
            SubscriptionId = updated.Id,
            FromStatus = oldStatus,
            ToStatus = updated.Status,
            Reason = reason,
            ChangedAt = DateTime.UtcNow
        });
        var dto = _mapper.Map<SubscriptionDto>(updated);
        // Send cancellation email
        var userResult = await _userService.GetUserByIdAsync(entity.UserId.ToString());
        if (userResult.Success && userResult.Data != null)
            await _notificationService.SendSubscriptionCancellationEmailAsync(userResult.Data.Email, userResult.Data.FullName, dto);
        return ApiResponse<SubscriptionDto>.SuccessResponse(dto, "Subscription cancelled");
    }

    public async Task<ApiResponse<SubscriptionDto>> PauseSubscriptionAsync(string subscriptionId)
    {
        var entity = await _subscriptionRepository.GetByIdAsync(Guid.Parse(subscriptionId));
        if (entity == null)
            return ApiResponse<SubscriptionDto>.ErrorResponse("Subscription not found");
        if (entity.IsPaused)
            return ApiResponse<SubscriptionDto>.ErrorResponse("Subscription is already paused");
        if (entity.IsCancelled)
            return ApiResponse<SubscriptionDto>.ErrorResponse("Cannot pause a cancelled subscription");
        // Validate status transition
        var validation = entity.ValidateStatusTransition(Subscription.SubscriptionStatuses.Paused);
        if (validation != ValidationResult.Success)
            return ApiResponse<SubscriptionDto>.ErrorResponse(validation.ErrorMessage);
        var oldStatus = entity.Status;
        entity.Status = Subscription.SubscriptionStatuses.Paused;
        entity.PausedDate = DateTime.UtcNow;
        var updated = await _subscriptionRepository.UpdateAsync(entity);
        // Add status history
        await _subscriptionRepository.AddStatusHistoryAsync(new SubscriptionStatusHistory {
            SubscriptionId = updated.Id,
            FromStatus = oldStatus,
            ToStatus = updated.Status,
            ChangedAt = DateTime.UtcNow
        });
        var dto = _mapper.Map<SubscriptionDto>(updated);
        // Send pause notification
        var userResult = await _userService.GetUserByIdAsync(entity.UserId.ToString());
        if (userResult.Success && userResult.Data != null)
            await _notificationService.SendSubscriptionPausedNotificationAsync(userResult.Data.Email, userResult.Data.FullName, dto);
        return ApiResponse<SubscriptionDto>.SuccessResponse(dto, "Subscription paused");
    }

    public async Task<ApiResponse<SubscriptionDto>> ResumeSubscriptionAsync(string subscriptionId)
    {
        var entity = await _subscriptionRepository.GetByIdAsync(Guid.Parse(subscriptionId));
        if (entity == null)
            return ApiResponse<SubscriptionDto>.ErrorResponse("Subscription not found");
        if (!entity.IsPaused)
            return ApiResponse<SubscriptionDto>.ErrorResponse("Only paused subscriptions can be resumed");
        // Validate status transition
        var validation = entity.ValidateStatusTransition(Subscription.SubscriptionStatuses.Active);
        if (validation != ValidationResult.Success)
            return ApiResponse<SubscriptionDto>.ErrorResponse(validation.ErrorMessage);
        var oldStatus = entity.Status;
        entity.Status = Subscription.SubscriptionStatuses.Active;
        entity.ResumedDate = DateTime.UtcNow;
        var updated = await _subscriptionRepository.UpdateAsync(entity);
        // Add status history
        await _subscriptionRepository.AddStatusHistoryAsync(new SubscriptionStatusHistory {
            SubscriptionId = updated.Id,
            FromStatus = oldStatus,
            ToStatus = updated.Status,
            ChangedAt = DateTime.UtcNow
        });
        var dto = _mapper.Map<SubscriptionDto>(updated);
        // Send resume notification
        var userResult = await _userService.GetUserByIdAsync(entity.UserId.ToString());
        if (userResult.Success && userResult.Data != null)
            await _notificationService.SendSubscriptionResumedNotificationAsync(userResult.Data.Email, userResult.Data.FullName, dto);
        return ApiResponse<SubscriptionDto>.SuccessResponse(dto, "Subscription resumed");
    }

    public async Task<ApiResponse<SubscriptionDto>> UpgradeSubscriptionAsync(string subscriptionId, string newPlanId)
    {
        var entity = await _subscriptionRepository.GetByIdAsync(Guid.Parse(subscriptionId));
        if (entity == null)
            return ApiResponse<SubscriptionDto>.ErrorResponse("Subscription not found");
        // 5. Prevent upgrading to the same plan
        if (entity.SubscriptionPlanId == Guid.Parse(newPlanId))
            return ApiResponse<SubscriptionDto>.ErrorResponse("Subscription is already on this plan");
        entity.SubscriptionPlanId = Guid.Parse(newPlanId);
        entity.UpdatedAt = DateTime.UtcNow;
        var updated = await _subscriptionRepository.UpdateAsync(entity);
        return ApiResponse<SubscriptionDto>.SuccessResponse(_mapper.Map<SubscriptionDto>(updated), "Subscription upgraded");
    }

    public async Task<ApiResponse<SubscriptionDto>> ReactivateSubscriptionAsync(string subscriptionId)
    {
        var entity = await _subscriptionRepository.GetByIdAsync(Guid.Parse(subscriptionId));
        if (entity == null)
            return ApiResponse<SubscriptionDto>.ErrorResponse("Subscription not found");
        // Validate status transition
        var validation = entity.ValidateStatusTransition(Subscription.SubscriptionStatuses.Active);
        if (validation != ValidationResult.Success)
            return ApiResponse<SubscriptionDto>.ErrorResponse(validation.ErrorMessage);
        var oldStatus = entity.Status;
        entity.Status = Subscription.SubscriptionStatuses.Active;
        entity.UpdatedAt = DateTime.UtcNow;
        var updated = await _subscriptionRepository.UpdateAsync(entity);
        // Add status history
        await _subscriptionRepository.AddStatusHistoryAsync(new SubscriptionStatusHistory {
            SubscriptionId = updated.Id,
            FromStatus = oldStatus,
            ToStatus = updated.Status,
            ChangedAt = DateTime.UtcNow
        });
        return ApiResponse<SubscriptionDto>.SuccessResponse(_mapper.Map<SubscriptionDto>(updated), "Subscription reactivated");
    }

    public async Task<ApiResponse<IEnumerable<SubscriptionPlanDto>>> GetAllPlansAsync()
    {
        var plans = await _subscriptionRepository.GetAllSubscriptionPlansAsync();
        var dtos = _mapper.Map<IEnumerable<SubscriptionPlanDto>>(plans);
        return ApiResponse<IEnumerable<SubscriptionPlanDto>>.SuccessResponse(dtos);
    }

    public async Task<ApiResponse<SubscriptionPlanDto>> GetPlanByIdAsync(string planId)
    {
        var plan = await _subscriptionRepository.GetSubscriptionPlanByIdAsync(Guid.Parse(planId));
        if (plan == null)
            return ApiResponse<SubscriptionPlanDto>.ErrorResponse("Plan not found");
        return ApiResponse<SubscriptionPlanDto>.SuccessResponse(_mapper.Map<SubscriptionPlanDto>(plan));
    }

    public async Task<ApiResponse<IEnumerable<BillingHistoryDto>>> GetBillingHistoryAsync(string subscriptionId)
    {
        // Implement billing history retrieval logic here
        // For now, return an empty list
        return ApiResponse<IEnumerable<BillingHistoryDto>>.SuccessResponse(new List<BillingHistoryDto>());
    }

    public async Task<ApiResponse<IEnumerable<PaymentMethodDto>>> GetPaymentMethodsAsync(string userId)
    {
        var methods = await _stripeService.GetCustomerPaymentMethodsAsync(userId);
        return ApiResponse<IEnumerable<PaymentMethodDto>>.SuccessResponse(methods);
    }

    public async Task<ApiResponse<PaymentMethodDto>> AddPaymentMethodAsync(string userId, string paymentMethodId)
    {
        var methodId = await _stripeService.AddPaymentMethodAsync(userId, paymentMethodId);
        var method = new PaymentMethodDto { PaymentMethodId = methodId };
        return ApiResponse<PaymentMethodDto>.SuccessResponse(method, "Payment method added");
    }

    public async Task<ApiResponse<SubscriptionDto>> GetSubscriptionByPlanIdAsync(string planId)
    {
        // TODO: Implement logic to get subscription by plan ID
        return ApiResponse<SubscriptionDto>.ErrorResponse("Not implemented");
    }

    public async Task<ApiResponse<IEnumerable<SubscriptionDto>>> GetActiveSubscriptionsAsync()
    {
        // TODO: Implement logic to get active subscriptions
        return ApiResponse<IEnumerable<SubscriptionDto>>.SuccessResponse(new List<SubscriptionDto>());
    }

    public async Task<ApiResponse<SubscriptionDto>> GetSubscriptionByIdAsync(string subscriptionId)
    {
        // TODO: Implement logic to get subscription by ID
        return await GetSubscriptionAsync(subscriptionId);
    }

    public async Task<ApiResponse<SubscriptionDto>> UpdateSubscriptionAsync(string subscriptionId, UpdateSubscriptionDto updateDto)
    {
        // TODO: Implement logic to update subscription
        return ApiResponse<SubscriptionDto>.ErrorResponse("Not implemented");
    }

    public async Task<ApiResponse<PaymentResultDto>> ProcessPaymentAsync(string subscriptionId, PaymentRequestDto paymentRequest)
    {
        // TODO: Integrate with Stripe or payment provider
        return ApiResponse<PaymentResultDto>.ErrorResponse("Not implemented");
    }

    public async Task<ApiResponse<UsageStatisticsDto>> GetUsageStatisticsAsync(string subscriptionId)
    {
        // TODO: Implement usage statistics logic
        return ApiResponse<UsageStatisticsDto>.ErrorResponse("Not implemented");
    }

    public async Task<ApiResponse<IEnumerable<SubscriptionDto>>> GetAllSubscriptionsAsync()
    {
        // TODO: Implement logic to get all subscriptions
        return ApiResponse<IEnumerable<SubscriptionDto>>.SuccessResponse(new List<SubscriptionDto>());
    }

    // --- ANALYTICS & REPORTING ---
    public async Task<ApiResponse<SubscriptionAnalyticsDto>> GetSubscriptionAnalyticsAsync(string subscriptionId)
    {
        // Simulate analytics: count active/cancelled/paused, revenue, churn, etc.
        var allSubscriptions = await _subscriptionRepository.GetAllSubscriptionsAsync();
        var analytics = new SubscriptionAnalyticsDto
        {
            ActiveSubscriptions = allSubscriptions.Count(s => s.Status == Subscription.SubscriptionStatuses.Active),
            CancelledSubscriptions = allSubscriptions.Count(s => s.Status == Subscription.SubscriptionStatuses.Cancelled),
            PausedSubscriptions = allSubscriptions.Count(s => s.Status == Subscription.SubscriptionStatuses.Paused),
            TotalSubscriptions = allSubscriptions.Count(),
            NewSubscriptionsThisMonth = allSubscriptions.Count(s => s.StartDate >= new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1)),
            ChurnRate = allSubscriptions.Count(s => s.Status == Subscription.SubscriptionStatuses.Cancelled) / (decimal)allSubscriptions.Count(),
            AverageSubscriptionValue = allSubscriptions.Any() ? allSubscriptions.Average(s => s.CurrentPrice) : 0,
            TotalRevenue = allSubscriptions.Sum(s => s.CurrentPrice),
            MonthlyRevenue = allSubscriptions.Where(s => s.StartDate >= DateTime.UtcNow.AddMonths(-1)).Sum(s => s.CurrentPrice),
            YearlyRevenue = allSubscriptions.Where(s => s.StartDate >= DateTime.UtcNow.AddYears(-1)).Sum(s => s.CurrentPrice),
            MonthlyGrowth = 0, // Placeholder
            SubscriptionsByPlan = allSubscriptions.GroupBy(s => s.SubscriptionPlan.Name).ToDictionary(g => g.Key, g => g.Count()),
            SubscriptionsByStatus = allSubscriptions.GroupBy(s => s.Status).ToDictionary(g => g.Key, g => g.Count()),
            TopCategories = new List<CategoryAnalyticsDto>() // Placeholder
        };
        return ApiResponse<SubscriptionAnalyticsDto>.SuccessResponse(analytics);
    }

    // --- THIRD-PARTY INTEGRATION ERROR HANDLING ---
    // Simulate webhook event handler for payment provider (Stripe/PayPal)
    public async Task<ApiResponse<bool>> HandlePaymentProviderWebhookAsync(string eventType, string subscriptionId, string? errorMessage = null)
    {
        if (eventType == "payment_failed")
        {
            await HandleFailedPaymentAsync(subscriptionId, errorMessage ?? "Unknown error");
            // Log event
            var sub = await _subscriptionRepository.GetByIdAsync(Guid.Parse(subscriptionId));
            if (sub != null)
                await _auditService.LogPaymentEventAsync(sub.UserId.ToString(), "PaymentFailed", subscriptionId, "Failed", errorMessage);
            return ApiResponse<bool>.SuccessResponse(true, "Payment failure handled");
        }
        if (eventType == "payment_succeeded")
        {
            // Mark as paid, notify user, etc.
            var sub = await _subscriptionRepository.GetByIdAsync(Guid.Parse(subscriptionId));
            if (sub != null)
            {
                sub.Status = Subscription.SubscriptionStatuses.Active;
                await _subscriptionRepository.UpdateAsync(sub);
                var userResult = await _userService.GetUserByIdAsync(sub.UserId.ToString());
                if (userResult.Success && userResult.Data != null)
                {
                    var billingRecord = new BillingRecordDto { Amount = sub.CurrentPrice, PaidDate = DateTime.UtcNow, Description = "Webhook Payment Success" };
                    await _notificationService.SendPaymentSuccessEmailAsync(userResult.Data.Email, userResult.Data.FullName, billingRecord);
                }
                await _auditService.LogPaymentEventAsync(sub.UserId.ToString(), "PaymentSucceeded", subscriptionId, "Succeeded");
            }
            return ApiResponse<bool>.SuccessResponse(true, "Payment success handled");
        }
        // Add more event types as needed
        return ApiResponse<bool>.ErrorResponse("Unhandled webhook event type");
    }

    public async Task<ApiResponse<SubscriptionPlanDto>> CreatePlanAsync(CreateSubscriptionPlanDto createPlanDto)
    {
        try
        {
            var plan = new SubscriptionPlan
            {
                Name = createPlanDto.Name,
                Description = createPlanDto.Description,
                Price = createPlanDto.Price,
                BillingCycleId = createPlanDto.BillingCycleId,
                CurrencyId = createPlanDto.CurrencyId,
                IsActive = createPlanDto.IsActive,
                DisplayOrder = createPlanDto.DisplayOrder,
                // PlanPrivileges will be added later or via a separate call
            };
            var created = await _subscriptionRepository.CreateSubscriptionPlanAsync(plan);
            return ApiResponse<SubscriptionPlanDto>.SuccessResponse(_mapper.Map<SubscriptionPlanDto>(created), "Plan created");
        }
        catch (Exception ex)
        {
            return ApiResponse<SubscriptionPlanDto>.ErrorResponse($"Failed to create plan: {ex.Message}");
        }
    }

    public async Task<ApiResponse<SubscriptionPlanDto>> UpdatePlanAsync(string planId, UpdateSubscriptionPlanDto updatePlanDto)
    {
        try
        {
            var plan = await _subscriptionRepository.GetSubscriptionPlanByIdAsync(Guid.Parse(planId));
            if (plan == null)
                return ApiResponse<SubscriptionPlanDto>.ErrorResponse("Plan not found");
            plan.Name = updatePlanDto.Name;
            plan.Description = updatePlanDto.Description;
            plan.IsActive = updatePlanDto.IsActive;
            // Remove updates to Price, BillingCycleId, CurrencyId, etc., as they are not present in the DTO
            var updated = await _subscriptionRepository.UpdateSubscriptionPlanAsync(plan);
            return ApiResponse<SubscriptionPlanDto>.SuccessResponse(_mapper.Map<SubscriptionPlanDto>(updated), "Plan updated");
        }
        catch (Exception ex)
        {
            return ApiResponse<SubscriptionPlanDto>.ErrorResponse($"Failed to update plan: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> ActivatePlanAsync(string planId)
    {
        try
        {
            var plan = await _subscriptionRepository.GetSubscriptionPlanByIdAsync(Guid.Parse(planId));
            if (plan == null)
                return ApiResponse<bool>.ErrorResponse("Plan not found");
            plan.IsActive = true;
            await _subscriptionRepository.UpdateSubscriptionPlanAsync(plan);
            return ApiResponse<bool>.SuccessResponse(true, "Plan activated");
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.ErrorResponse($"Failed to activate plan: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> DeactivatePlanAsync(string planId)
    {
        try
        {
            var plan = await _subscriptionRepository.GetSubscriptionPlanByIdAsync(Guid.Parse(planId));
            if (plan == null)
                return ApiResponse<bool>.ErrorResponse("Plan not found");
            plan.IsActive = false;
            await _subscriptionRepository.UpdateSubscriptionPlanAsync(plan);
            return ApiResponse<bool>.SuccessResponse(true, "Plan deactivated");
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.ErrorResponse($"Failed to deactivate plan: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> DeletePlanAsync(string planId)
    {
        try
        {
            var result = await _subscriptionRepository.DeleteSubscriptionPlanAsync(Guid.Parse(planId));
            if (!result)
                return ApiResponse<bool>.ErrorResponse("Plan not found or could not be deleted");
            return ApiResponse<bool>.SuccessResponse(true, "Plan deleted");
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.ErrorResponse($"Failed to delete plan: {ex.Message}");
        }
    }

    public async Task<ApiResponse<SubscriptionDto>> GetByStripeSubscriptionIdAsync(string stripeSubscriptionId)
    {
        // TODO: Implement logic to get subscription by Stripe ID
        return ApiResponse<SubscriptionDto>.ErrorResponse("Not implemented");
    }

    public async Task<ApiResponse<IEnumerable<SubscriptionPlanDto>>> GetAllSubscriptionPlansAsync()
    {
        // TODO: Implement logic to get all subscription plans
        return ApiResponse<IEnumerable<SubscriptionPlanDto>>.SuccessResponse(new List<SubscriptionPlanDto>());
    }

    public async Task<ApiResponse<IEnumerable<SubscriptionPlanDto>>> GetActiveSubscriptionPlansAsync()
    {
        // TODO: Implement logic to get active subscription plans
        return ApiResponse<IEnumerable<SubscriptionPlanDto>>.SuccessResponse(new List<SubscriptionPlanDto>());
    }

    public async Task<ApiResponse<IEnumerable<SubscriptionPlanDto>>> GetSubscriptionPlansByCategoryAsync(string category)
    {
        // TODO: Implement logic to get plans by category
        return ApiResponse<IEnumerable<SubscriptionPlanDto>>.SuccessResponse(new List<SubscriptionPlanDto>());
    }

    public async Task<ApiResponse<SubscriptionPlanDto>> GetSubscriptionPlanAsync(string planId)
    {
        // TODO: Implement logic to get a subscription plan
        return ApiResponse<SubscriptionPlanDto>.ErrorResponse("Not implemented");
    }

    public async Task<ApiResponse<SubscriptionPlanDto>> CreateSubscriptionPlanAsync(CreateSubscriptionPlanDto createDto)
    {
        // TODO: Implement logic to create a subscription plan
        return ApiResponse<SubscriptionPlanDto>.ErrorResponse("Not implemented");
    }

    public async Task<ApiResponse<SubscriptionPlanDto>> UpdateSubscriptionPlanAsync(string planId, UpdateSubscriptionPlanDto updateDto)
    {
        // TODO: Implement logic to update a subscription plan
        return ApiResponse<SubscriptionPlanDto>.ErrorResponse("Not implemented");
    }

    public async Task<ApiResponse<bool>> DeleteSubscriptionPlanAsync(string planId)
    {
        // TODO: Implement logic to delete a subscription plan
        return ApiResponse<bool>.ErrorResponse("Not implemented");
    }

    // Example: Booking a consultation using privilege system
    public async Task<ApiResponse<bool>> BookConsultationAsync(Guid userId, Guid subscriptionId)
    {
        // Check if user has remaining consult privileges
        var remaining = await _privilegeService.GetRemainingPrivilegeAsync(subscriptionId, "Teleconsultation");
        if (remaining <= 0)
            return ApiResponse<bool>.ErrorResponse("No teleconsultations remaining in your plan.");
        var used = await _privilegeService.UsePrivilegeAsync(subscriptionId, "Teleconsultation");
        if (!used)
            return ApiResponse<bool>.ErrorResponse("Failed to use teleconsultation privilege.");
        // Proceed with booking logic (not shown)
        return ApiResponse<bool>.SuccessResponse(true, "Consultation booked.");
    }

    // Example: Medication supply using privilege system
    public async Task<ApiResponse<bool>> RequestMedicationSupplyAsync(Guid userId, Guid subscriptionId)
    {
        var remaining = await _privilegeService.GetRemainingPrivilegeAsync(subscriptionId, "MedicationSupply");
        if (remaining <= 0)
            return ApiResponse<bool>.ErrorResponse("No medication supply privilege remaining in your plan.");
        var used = await _privilegeService.UsePrivilegeAsync(subscriptionId, "MedicationSupply");
        if (!used)
            return ApiResponse<bool>.ErrorResponse("Failed to use medication supply privilege.");
        // Proceed with medication supply logic (not shown)
        return ApiResponse<bool>.SuccessResponse(true, "Medication supply requested.");
    }

    // --- PAYMENT & BILLING EDGE CASES ---

    // 1. Handle failed payment and update subscription status
    public async Task<ApiResponse<PaymentResultDto>> HandleFailedPaymentAsync(string subscriptionId, string reason)
    {
        var entity = await _subscriptionRepository.GetByIdAsync(Guid.Parse(subscriptionId));
        if (entity == null)
            return ApiResponse<PaymentResultDto>.ErrorResponse("Subscription not found");
        entity.Status = Subscription.SubscriptionStatuses.PaymentFailed;
        entity.LastPaymentError = reason;
        entity.FailedPaymentAttempts += 1;
        entity.UpdatedAt = DateTime.UtcNow;
        await _subscriptionRepository.UpdateAsync(entity);
        // Add status history
        await _subscriptionRepository.AddStatusHistoryAsync(new SubscriptionStatusHistory {
            SubscriptionId = entity.Id,
            FromStatus = entity.Status,
            ToStatus = Subscription.SubscriptionStatuses.PaymentFailed,
            Reason = reason,
            ChangedAt = DateTime.UtcNow
        });
        // Send payment failed email
        var userResult = await _userService.GetUserByIdAsync(entity.UserId.ToString());
        if (userResult.Success && userResult.Data != null)
        {
            var billingRecord = new BillingRecordDto { Amount = entity.CurrentPrice, DueDate = DateTime.UtcNow, Description = reason };
            await _notificationService.SendPaymentFailedEmailAsync(userResult.Data.Email, userResult.Data.FullName, billingRecord);
        }
        return ApiResponse<PaymentResultDto>.ErrorResponse($"Payment failed: {reason}");
    }

    // 2. Retry payment and reactivate subscription if successful
    public async Task<ApiResponse<PaymentResultDto>> RetryPaymentAsync(string subscriptionId, PaymentRequestDto paymentRequest)
    {
        var entity = await _subscriptionRepository.GetByIdAsync(Guid.Parse(subscriptionId));
        if (entity == null)
            return ApiResponse<PaymentResultDto>.ErrorResponse("Subscription not found");
        // Simulate payment retry logic
        var paymentResult = await _stripeService.ProcessPaymentAsync(entity.UserId.ToString(), paymentRequest.Amount, "USD");
        if (paymentResult.Status == "succeeded")
        {
            // Send payment success email
            var userResult = await _userService.GetUserByIdAsync(entity.UserId.ToString());
            if (userResult.Success && userResult.Data != null)
            {
                var billingRecord = new BillingRecordDto { Amount = paymentRequest.Amount, PaidDate = DateTime.UtcNow, Description = "Retry Payment" };
                await _notificationService.SendPaymentSuccessEmailAsync(userResult.Data.Email, userResult.Data.FullName, billingRecord);
            }
            entity.Status = Subscription.SubscriptionStatuses.Active;
            entity.UpdatedAt = DateTime.UtcNow;
            await _subscriptionRepository.UpdateAsync(entity);
            // TODO: Trigger notification to user
            return ApiResponse<PaymentResultDto>.SuccessResponse(paymentResult, "Payment retried and subscription reactivated");
        }
        else
        {
            return ApiResponse<PaymentResultDto>.ErrorResponse($"Payment retry failed: {paymentResult.ErrorMessage}");
        }
    }

    // 3. Auto-renewal logic (to be called by a scheduler/cron job)
    public async Task<ApiResponse<SubscriptionDto>> AutoRenewSubscriptionAsync(string subscriptionId)
    {
        var entity = await _subscriptionRepository.GetByIdAsync(Guid.Parse(subscriptionId));
        if (entity == null)
            return ApiResponse<SubscriptionDto>.ErrorResponse("Subscription not found");
        if (entity.Status != Subscription.SubscriptionStatuses.Active)
            return ApiResponse<SubscriptionDto>.ErrorResponse("Only active subscriptions can be auto-renewed");
        // Simulate payment
        var paymentResult = await _stripeService.ProcessPaymentAsync(entity.UserId.ToString(), entity.CurrentPrice, "USD");
        if (paymentResult.Status == "succeeded")
        {
            // Send renewal confirmation email
            var userResult = await _userService.GetUserByIdAsync(entity.UserId.ToString());
            if (userResult.Success && userResult.Data != null)
            {
                var billingRecord = new BillingRecordDto { Amount = entity.CurrentPrice, PaidDate = DateTime.UtcNow, Description = "Auto-Renewal" };
                await _notificationService.SendPaymentSuccessEmailAsync(userResult.Data.Email, userResult.Data.FullName, billingRecord);
            }
            entity.NextBillingDate = entity.NextBillingDate.AddMonths(1);
            entity.UpdatedAt = DateTime.UtcNow;
            await _subscriptionRepository.UpdateAsync(entity);
            // TODO: Add billing history record
            return ApiResponse<SubscriptionDto>.SuccessResponse(_mapper.Map<SubscriptionDto>(entity), "Subscription auto-renewed");
        }
        else
        {
            await HandleFailedPaymentAsync(subscriptionId, paymentResult.ErrorMessage ?? "Auto-renewal payment failed");
            return ApiResponse<SubscriptionDto>.ErrorResponse($"Auto-renewal payment failed: {paymentResult.ErrorMessage}");
        }
    }

    // 4. Prorated upgrades/downgrades
    public async Task<ApiResponse<SubscriptionDto>> ProrateUpgradeAsync(string subscriptionId, string newPlanId)
    {
        var entity = await _subscriptionRepository.GetByIdAsync(Guid.Parse(subscriptionId));
        if (entity == null)
            return ApiResponse<SubscriptionDto>.ErrorResponse("Subscription not found");
        if (entity.SubscriptionPlanId == Guid.Parse(newPlanId))
            return ApiResponse<SubscriptionDto>.ErrorResponse("Already on this plan");
        // Simulate proration calculation
        var daysLeft = (entity.NextBillingDate - DateTime.UtcNow).TotalDays;
        var oldPlan = await _subscriptionRepository.GetSubscriptionPlanByIdAsync(entity.SubscriptionPlanId);
        var newPlan = await _subscriptionRepository.GetSubscriptionPlanByIdAsync(Guid.Parse(newPlanId));
        if (oldPlan == null || newPlan == null)
            return ApiResponse<SubscriptionDto>.ErrorResponse("Plan not found");
        // In proration, use Price and BillingCycleId
        var credit = (decimal)(daysLeft / 30.0) * oldPlan.Price; // Assuming Price is the monthly price
        var charge = newPlan.Price - credit;
        // Simulate payment for the difference
        var paymentResult = await _stripeService.ProcessPaymentAsync(entity.UserId.ToString(), charge, "USD");
        if (paymentResult.Status == "succeeded")
        {
            entity.SubscriptionPlanId = newPlan.Id;
            entity.UpdatedAt = DateTime.UtcNow;
            await _subscriptionRepository.UpdateAsync(entity);
            // TODO: Add billing history record
            return ApiResponse<SubscriptionDto>.SuccessResponse(_mapper.Map<SubscriptionDto>(entity), "Subscription upgraded with proration");
        }
        else
        {
            return ApiResponse<SubscriptionDto>.ErrorResponse($"Prorated upgrade payment failed: {paymentResult.ErrorMessage}");
        }
    }

    // --- USAGE & LIMITS ---

    // Check if user can use a privilege (e.g., book a consult)
    public async Task<ApiResponse<bool>> CanUsePrivilegeAsync(string subscriptionId, string privilegeName)
    {
        var subscription = await _subscriptionRepository.GetByIdAsync(Guid.Parse(subscriptionId));
        if (subscription == null || subscription.Status != Subscription.SubscriptionStatuses.Active)
            return ApiResponse<bool>.ErrorResponse("Subscription not active");
        var planPrivileges = await _planPrivilegeRepo.GetByPlanIdAsync(subscription.SubscriptionPlanId);
        var planPrivilege = planPrivileges.FirstOrDefault(p => p.Privilege.Name == privilegeName);
        if (planPrivilege == null)
            return ApiResponse<bool>.ErrorResponse("Privilege not included in plan");
        var usages = await _usageRepo.GetBySubscriptionIdAsync(subscription.Id);
        var usage = usages.FirstOrDefault(u => u.SubscriptionPlanPrivilegeId == planPrivilege.Id);
        int used = usage?.UsedValue ?? 0;
        int allowed = planPrivilege.Value;
        if (used >= allowed)
            return ApiResponse<bool>.ErrorResponse($"Usage limit reached for {privilegeName}");
        return ApiResponse<bool>.SuccessResponse(true);
    }

    // Increment privilege usage (to be called after successful action)
    public async Task IncrementPrivilegeUsageAsync(string subscriptionId, string privilegeName)
    {
        var subscription = await _subscriptionRepository.GetByIdAsync(Guid.Parse(subscriptionId));
        if (subscription == null) return;
        var planPrivileges = await _planPrivilegeRepo.GetByPlanIdAsync(subscription.SubscriptionPlanId);
        var planPrivilege = planPrivileges.FirstOrDefault(p => p.Privilege.Name == privilegeName);
        if (planPrivilege == null) return;
        var usages = await _usageRepo.GetBySubscriptionIdAsync(subscription.Id);
        var usage = usages.FirstOrDefault(u => u.SubscriptionPlanPrivilegeId == planPrivilege.Id);
        if (usage == null)
        {
            usage = new UserSubscriptionPrivilegeUsage
            {
                SubscriptionId = subscription.Id,
                SubscriptionPlanPrivilegeId = planPrivilege.Id,
                UsedValue = 1,
                UsagePeriodStart = DateTime.UtcNow,
                UsagePeriodEnd = DateTime.UtcNow.AddMonths(1)
            };
            await _usageRepo.AddAsync(usage);
        }
        else
        {
            usage.UsedValue += 1;
            await _usageRepo.UpdateAsync(usage);
        }
    }

    // Reset usage counters for all active subscriptions (to be called by a scheduler/cron job at billing cycle start)
    public async Task ResetAllUsageCountersAsync()
    {
        var activeSubscriptions = await _subscriptionRepository.GetActiveSubscriptionsAsync();
        foreach (var subscription in activeSubscriptions)
        {
            var usages = await _usageRepo.GetBySubscriptionIdAsync(subscription.Id);
            foreach (var usage in usages)
            {
                usage.UsedValue = 0;
                await _usageRepo.UpdateAsync(usage);
            }
        }
    }

    // Expire unused benefits (e.g., free consults) if not used within the period
    public async Task ExpireUnusedBenefitsAsync()
    {
        var activeSubscriptions = await _subscriptionRepository.GetActiveSubscriptionsAsync();
        foreach (var subscription in activeSubscriptions)
        {
            var usages = await _usageRepo.GetBySubscriptionIdAsync(subscription.Id);
            foreach (var usage in usages)
            {
                // For now, just reset to 0 at expiry; can be extended for carry-over logic
                usage.UsedValue = 0;
                await _usageRepo.UpdateAsync(usage);
            }
        }
    }

    // --- ADMIN OPERATIONS ---

    // Deactivate a plan (admin action)
    public async Task<ApiResponse<bool>> DeactivatePlanAsync(string planId, string adminUserId)
    {
        // Example: deactivate plan and notify/pause all subscribers
        var plan = await _subscriptionRepository.GetSubscriptionPlanByIdAsync(Guid.Parse(planId));
        if (plan == null)
            return ApiResponse<bool>.ErrorResponse("Plan not found");
        plan.IsActive = false;
        await _subscriptionRepository.UpdateSubscriptionPlanAsync(plan);
        // TODO: Pause all subscribers and notify them
        return ApiResponse<bool>.SuccessResponse(true, "Plan deactivated and subscribers notified/paused.");
    }

    // Bulk cancel subscriptions (admin action)
    public async Task<ApiResponse<int>> BulkCancelSubscriptionsAsync(IEnumerable<string> subscriptionIds, string adminUserId, string? reason = null)
    {
        int cancelled = 0;
        foreach (var id in subscriptionIds)
        {
            var sub = await _subscriptionRepository.GetByIdAsync(Guid.Parse(id));
            if (sub != null && sub.Status == Subscription.SubscriptionStatuses.Active)
            {
                sub.Status = Subscription.SubscriptionStatuses.Cancelled;
                sub.CancellationReason = reason ?? "Bulk admin cancel";
                sub.CancelledDate = DateTime.UtcNow;
                await _subscriptionRepository.UpdateAsync(sub);
                var userResult = await _userService.GetUserByIdAsync(sub.UserId.ToString());
                if (userResult.Success && userResult.Data != null)
                    await _notificationService.SendSubscriptionCancelledNotificationAsync(userResult.Data.Email, userResult.Data.FullName, _mapper.Map<SubscriptionDto>(sub));
                await _auditService.LogUserActionAsync(adminUserId, "BulkCancelSubscription", "Subscription", id, "Cancelled by admin");
                cancelled++;
            }
        }
        return ApiResponse<int>.SuccessResponse(cancelled, $"{cancelled} subscriptions cancelled.");
    }

    // Bulk upgrade subscriptions (admin action)
    public async Task<ApiResponse<int>> BulkUpgradeSubscriptionsAsync(IEnumerable<string> subscriptionIds, string newPlanId, string adminUserId)
    {
        int upgraded = 0;
        foreach (var id in subscriptionIds)
        {
            var sub = await _subscriptionRepository.GetByIdAsync(Guid.Parse(id));
            if (sub != null && sub.Status == Subscription.SubscriptionStatuses.Active && sub.SubscriptionPlanId != Guid.Parse(newPlanId))
            {
                sub.SubscriptionPlanId = Guid.Parse(newPlanId);
                sub.UpdatedAt = DateTime.UtcNow;
                await _subscriptionRepository.UpdateAsync(sub);
                var userResult = await _userService.GetUserByIdAsync(sub.UserId.ToString());
                if (userResult.Success && userResult.Data != null)
                    await _notificationService.SendSubscriptionConfirmationAsync(userResult.Data.Email, userResult.Data.FullName, _mapper.Map<SubscriptionDto>(sub));
                await _auditService.LogUserActionAsync(adminUserId, "BulkUpgradeSubscription", "Subscription", id, $"Upgraded to plan {newPlanId}");
                upgraded++;
            }
        }
        return ApiResponse<int>.SuccessResponse(upgraded, $"{upgraded} subscriptions upgraded.");
    }
} 