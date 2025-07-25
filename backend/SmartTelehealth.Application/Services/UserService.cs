using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SmartTelehealth.Application.DTOs;
using SmartTelehealth.Application.Interfaces;
using SmartTelehealth.Core.Entities;
using SmartTelehealth.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using System.IO;

namespace SmartTelehealth.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly INotificationService _notificationService;
    private readonly IStripeService _stripeService;
    private readonly ILogger<UserService> _logger;
    private readonly UserManager<User> _userManager;
    private readonly IMapper _mapper;

    public UserService(
        IUserRepository userRepository,
        INotificationService notificationService,
        IStripeService stripeService,
        ILogger<UserService> logger,
        UserManager<User> userManager,
        IMapper mapper)
    {
        _userRepository = userRepository;
        _notificationService = notificationService;
        _stripeService = stripeService;
        _logger = logger;
        _userManager = userManager;
        _mapper = mapper;
    }

    // User profile operations
    public async Task<ApiResponse<UserDto>> GetUserByIdAsync(string userId)
    {
        try
        {
            if (!Guid.TryParse(userId, out var userGuid))
                return ApiResponse<UserDto>.ErrorResponse("Invalid user ID");

            var user = await _userRepository.GetByIdAsync(userGuid);
            if (user == null)
                return ApiResponse<UserDto>.ErrorResponse("User not found");

            var userDto = _mapper.Map<UserDto>(user);
            return ApiResponse<UserDto>.SuccessResponse(userDto, "User retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId}", userId);
            return ApiResponse<UserDto>.ErrorResponse($"Failed to get user: {ex.Message}");
        }
    }

    public async Task<ApiResponse<UserDto>> UpdateUserAsync(string userId, UpdateUserDto updateDto)
    {
        try
        {
            if (!Guid.TryParse(userId, out var userGuid))
                return ApiResponse<UserDto>.ErrorResponse("Invalid user ID");

            var user = await _userRepository.GetByIdAsync(userGuid);
            if (user == null)
                return ApiResponse<UserDto>.ErrorResponse("User not found");

            // Update user properties
            if (!string.IsNullOrEmpty(updateDto.FirstName))
                user.FirstName = updateDto.FirstName;
            if (!string.IsNullOrEmpty(updateDto.LastName))
                user.LastName = updateDto.LastName;
            if (!string.IsNullOrEmpty(updateDto.Email))
                user.Email = updateDto.Email;
            if (!string.IsNullOrEmpty(updateDto.PhoneNumber))
                user.PhoneNumber = updateDto.PhoneNumber;
            if (updateDto.DateOfBirth.HasValue)
                user.DateOfBirth = updateDto.DateOfBirth.Value;
            if (!string.IsNullOrEmpty(updateDto.Address))
                user.Address = updateDto.Address;
            if (!string.IsNullOrEmpty(updateDto.City))
                user.City = updateDto.City;
            if (!string.IsNullOrEmpty(updateDto.State))
                user.State = updateDto.State;
            if (!string.IsNullOrEmpty(updateDto.ZipCode))
                user.ZipCode = updateDto.ZipCode;
            if (!string.IsNullOrEmpty(updateDto.Country))
                user.Country = updateDto.Country;

            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            var userDto = _mapper.Map<UserDto>(user);
            return ApiResponse<UserDto>.SuccessResponse(userDto, "User updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", userId);
            return ApiResponse<UserDto>.ErrorResponse($"Failed to update user: {ex.Message}");
        }
    }

    public async Task<ApiResponse<object>> UploadProfilePictureAsync(Guid userId, IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
                return ApiResponse<object>.ErrorResponse("No file provided");

            // Validate file type
            var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif" };
            if (!allowedTypes.Contains(file.ContentType))
                return ApiResponse<object>.ErrorResponse("Invalid file type. Only JPEG, PNG, and GIF are allowed");

            // Validate file size (max 5MB)
            if (file.Length > 5 * 1024 * 1024)
                return ApiResponse<object>.ErrorResponse("File size too large. Maximum size is 5MB");

            // Generate unique filename
            var fileName = $"{userId}_{DateTime.UtcNow:yyyyMMddHHmmss}_{file.FileName}";
            var filePath = Path.Combine("wwwroot", "uploads", "profiles", fileName);

            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Update user profile picture
            var user = await _userRepository.GetByIdAsync(userId);
            if (user != null)
            {
                user.ProfilePicture = $"/uploads/profiles/{fileName}";
                user.UpdatedAt = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user);
            }

            return ApiResponse<object>.SuccessResponse(new { imageUrl = $"/uploads/profiles/{fileName}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading profile picture: {UserId}", userId);
            return ApiResponse<object>.ErrorResponse($"Failed to upload profile picture: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> DeleteUserAsync(string userId)
    {
        try
        {
            if (!Guid.TryParse(userId, out var userGuid))
                return ApiResponse<bool>.ErrorResponse("Invalid user ID");

            var user = await _userRepository.GetByIdAsync(userGuid);
            if (user == null)
                return ApiResponse<bool>.ErrorResponse("User not found");

            await _userRepository.DeleteAsync(userGuid);
            return ApiResponse<bool>.SuccessResponse(true, "User deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", userId);
            return ApiResponse<bool>.ErrorResponse($"Failed to delete user: {ex.Message}");
        }
    }

    public async Task<ApiResponse<IEnumerable<UserDto>>> GetUsersByRoleAsync(string role)
    {
        try
        {
            var users = await _userRepository.GetByRoleAsync(role);
            var userDtos = _mapper.Map<IEnumerable<UserDto>>(users);
            return ApiResponse<IEnumerable<UserDto>>.SuccessResponse(userDtos, $"Users with role {role} retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users by role {Role}", role);
            return ApiResponse<IEnumerable<UserDto>>.ErrorResponse($"Failed to get users by role: {ex.Message}");
        }
    }

    public async Task<ApiResponse<UserDto>> GetUserByEmailAsync(string email)
    {
        try
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
                return ApiResponse<UserDto>.ErrorResponse("User not found");

            var userDto = _mapper.Map<UserDto>(user);
            return ApiResponse<UserDto>.SuccessResponse(userDto, "User retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by email {Email}", email);
            return ApiResponse<UserDto>.ErrorResponse($"Failed to get user by email: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> ChangePasswordAsync(string userId, ChangePasswordDto changePasswordDto)
    {
        try
        {
            if (!Guid.TryParse(userId, out var userGuid))
                return ApiResponse<bool>.ErrorResponse("Invalid user ID");

            var user = await _userRepository.GetByIdAsync(userGuid);
            if (user == null)
                return ApiResponse<bool>.ErrorResponse("User not found");

            // Verify current password
            if (!VerifyPassword(changePasswordDto.CurrentPassword, user.PasswordHash))
                return ApiResponse<bool>.ErrorResponse("Current password is incorrect");

            // Hash new password
            user.PasswordHash = HashPassword(changePasswordDto.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            return ApiResponse<bool>.SuccessResponse(true, "Password changed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {UserId}", userId);
            return ApiResponse<bool>.ErrorResponse($"Failed to change password: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> ResetPasswordAsync(string email)
    {
        try
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
                return ApiResponse<bool>.ErrorResponse("User not found");

            // Generate reset token
            var resetToken = Guid.NewGuid().ToString();
            user.PasswordResetToken = resetToken;
            user.PasswordResetTokenExpires = DateTime.UtcNow.AddHours(24);
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            // Send reset email
            await _notificationService.SendPasswordResetEmailAsync(email, resetToken);

            return ApiResponse<bool>.SuccessResponse(true, "Password reset email sent successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for email {Email}", email);
            return ApiResponse<bool>.ErrorResponse($"Failed to reset password: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> ConfirmPasswordResetAsync(string email, string resetToken, string newPassword)
    {
        try
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
                return ApiResponse<bool>.ErrorResponse("User not found");

            if (user.PasswordResetToken != resetToken)
                return ApiResponse<bool>.ErrorResponse("Invalid reset token");

            if (user.PasswordResetTokenExpires < DateTime.UtcNow)
                return ApiResponse<bool>.ErrorResponse("Reset token has expired");

            // Update password
            user.PasswordHash = HashPassword(newPassword);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpires = null;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            return ApiResponse<bool>.SuccessResponse(true, "Password reset successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming password reset for email {Email}", email);
            return ApiResponse<bool>.ErrorResponse($"Failed to confirm password reset: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> UpdateUserProfileAsync(string userId, UpdateUserProfileDto profileDto)
    {
        try
        {
            if (!Guid.TryParse(userId, out var userGuid))
                return ApiResponse<bool>.ErrorResponse("Invalid user ID");

            var user = await _userRepository.GetByIdAsync(userGuid);
            if (user == null)
                return ApiResponse<bool>.ErrorResponse("User not found");

            // Update profile properties
            if (!string.IsNullOrEmpty(profileDto.FirstName))
                user.FirstName = profileDto.FirstName;
            if (!string.IsNullOrEmpty(profileDto.LastName))
                user.LastName = profileDto.LastName;
            if (!string.IsNullOrEmpty(profileDto.PhoneNumber))
                user.PhoneNumber = profileDto.PhoneNumber;
            if (profileDto.DateOfBirth.HasValue)
                user.DateOfBirth = profileDto.DateOfBirth.Value;
            if (!string.IsNullOrEmpty(profileDto.Address))
                user.Address = profileDto.Address;
            if (!string.IsNullOrEmpty(profileDto.City))
                user.City = profileDto.City;
            if (!string.IsNullOrEmpty(profileDto.State))
                user.State = profileDto.State;
            if (!string.IsNullOrEmpty(profileDto.ZipCode))
                user.ZipCode = profileDto.ZipCode;
            if (!string.IsNullOrEmpty(profileDto.Country))
                user.Country = profileDto.Country;

            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            return ApiResponse<bool>.SuccessResponse(true, "Profile updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile {UserId}", userId);
            return ApiResponse<bool>.ErrorResponse($"Failed to update profile: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> UpdateUserPreferencesAsync(string userId, UpdateUserPreferencesDto preferencesDto)
    {
        try
        {
            if (!Guid.TryParse(userId, out var userGuid))
                return ApiResponse<bool>.ErrorResponse("Invalid user ID");

            var user = await _userRepository.GetByIdAsync(userGuid);
            if (user == null)
                return ApiResponse<bool>.ErrorResponse("User not found");

            // Update preferences
            if (!string.IsNullOrEmpty(preferencesDto.NotificationPreferences))
                user.NotificationPreferences = preferencesDto.NotificationPreferences;
            if (!string.IsNullOrEmpty(preferencesDto.LanguagePreference))
                user.LanguagePreference = preferencesDto.LanguagePreference;
            if (!string.IsNullOrEmpty(preferencesDto.TimeZonePreference))
                user.TimeZonePreference = preferencesDto.TimeZonePreference;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            return ApiResponse<bool>.SuccessResponse(true, "Preferences updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user preferences {UserId}", userId);
            return ApiResponse<bool>.ErrorResponse($"Failed to update preferences: {ex.Message}");
        }
    }

    // Patient operations
    public async Task<ApiResponse<PatientDto>> GetPatientByIdAsync(Guid patientId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(patientId);
            if (user == null || user.UserType != "Patient")
                return ApiResponse<PatientDto>.ErrorResponse("Patient not found");

            var patientDto = MapToPatientDto(user);
            
            // Get additional patient data
            var stats = await GetPatientStatsAsync(patientId);
            var statsObj = await GetPatientStatsAsync(patientId);
            if (statsObj is not null)
            {
                var statsData = (dynamic)statsObj;
                patientDto.TotalAppointments = statsData.TotalAppointments ?? 0;
                patientDto.CompletedAppointments = statsData.CompletedAppointments ?? 0;
                patientDto.CancelledAppointments = statsData.CancelledAppointments ?? 0;
                patientDto.TotalSpent = statsData.TotalSpent ?? 0m;
            }

            return ApiResponse<PatientDto>.SuccessResponse(patientDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting patient by ID: {PatientId}", patientId);
            return ApiResponse<PatientDto>.ErrorResponse($"Failed to get patient: {ex.Message}");
        }
    }

    public async Task<ApiResponse<IEnumerable<PatientDto>>> GetAllPatientsAsync()
    {
        try
        {
            var patients = await _userRepository.GetByUserTypeAsync("Patient");
            var patientDtos = patients.Select(MapToPatientDto).ToList();
            return ApiResponse<IEnumerable<PatientDto>>.SuccessResponse(patientDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all patients");
            return ApiResponse<IEnumerable<PatientDto>>.ErrorResponse($"Failed to get patients: {ex.Message}");
        }
    }

    public async Task<ApiResponse<object>> GetPatientMedicalHistoryAsync(Guid patientId)
    {
        try
        {
            // This would typically fetch from a medical history repository
            var medicalHistory = new
            {
                Allergies = "None known",
                CurrentMedications = "None",
                MedicalHistory = "No significant medical history",
                FamilyHistory = "No significant family history",
                Lifestyle = "Non-smoker, occasional alcohol"
            };

            return ApiResponse<object>.SuccessResponse(medicalHistory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting patient medical history: {PatientId}", patientId);
            return ApiResponse<object>.ErrorResponse($"Failed to get medical history: {ex.Message}");
        }
    }

    public async Task<ApiResponse<object>> UpdatePatientMedicalHistoryAsync(Guid patientId, UpdateMedicalHistoryDto medicalHistoryDto)
    {
        try
        {
            // This would typically update a medical history repository
            var updatedHistory = new
            {
                Allergies = medicalHistoryDto.Allergies,
                CurrentMedications = medicalHistoryDto.CurrentMedications,
                MedicalHistory = medicalHistoryDto.MedicalHistory,
                FamilyHistory = medicalHistoryDto.FamilyHistory,
                Lifestyle = medicalHistoryDto.Lifestyle,
                UpdatedAt = DateTime.UtcNow
            };

            return ApiResponse<object>.SuccessResponse(updatedHistory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating patient medical history: {PatientId}", patientId);
            return ApiResponse<object>.ErrorResponse($"Failed to update medical history: {ex.Message}");
        }
    }

    // Provider operations
    public async Task<ApiResponse<ProviderDto>> GetProviderAsync(string id)
    {
        if (!Guid.TryParse(id, out var providerGuid))
            return ApiResponse<ProviderDto>.ErrorResponse("Invalid provider ID");
        var provider = await _userRepository.GetByIdAsync(providerGuid);
        if (provider == null)
            return ApiResponse<ProviderDto>.ErrorResponse("Provider not found");
        var providerDto = _mapper.Map<ProviderDto>(provider);
        return ApiResponse<ProviderDto>.SuccessResponse(providerDto);
    }

    public async Task<ApiResponse<ProviderDto>> GetProviderByEmailAsync(string email)
    {
        try
        {
            var provider = await _userRepository.GetByEmailAsync(email);
            if (provider == null)
            {
                return ApiResponse<ProviderDto>.ErrorResponse("Provider not found");
            }

            var providerDto = MapToProviderDto(provider);
            return ApiResponse<ProviderDto>.SuccessResponse(providerDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting provider by email {Email}", email);
            return ApiResponse<ProviderDto>.ErrorResponse($"Failed to get provider: {ex.Message}");
        }
    }

    public async Task<ApiResponse<ProviderDto>> GetProviderByLicenseAsync(string licenseNumber)
    {
        try
        {
            var provider = await _userRepository.GetByLicenseNumberAsync(licenseNumber);
            if (provider == null)
            {
                return ApiResponse<ProviderDto>.ErrorResponse("Provider not found");
            }

            var providerDto = MapToProviderDto(provider);
            return ApiResponse<ProviderDto>.SuccessResponse(providerDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting provider by license {LicenseNumber}", licenseNumber);
            return ApiResponse<ProviderDto>.ErrorResponse($"Failed to get provider: {ex.Message}");
        }
    }

    public async Task<ApiResponse<ProviderDto>> GetProviderByIdAsync(Guid providerId)
    {
        try
        {
            var provider = await _userRepository.GetByIdAsync(providerId);
            if (provider == null)
            {
                return ApiResponse<ProviderDto>.ErrorResponse("Provider not found");
            }

            var providerDto = _mapper.Map<ProviderDto>(provider);
            return ApiResponse<ProviderDto>.SuccessResponse(providerDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting provider by ID {ProviderId}", providerId);
            return ApiResponse<ProviderDto>.ErrorResponse($"Failed to get provider: {ex.Message}");
        }
    }

    public async Task<ApiResponse<IEnumerable<UserDto>>> GetAllProvidersAsync()
    {
        try
        {
            var providers = await _userRepository.GetByUserTypeAsync("Provider");
            var userDtos = providers.Select(u => new UserDto
            {
                Id = u.Id.ToString(),
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                // ... map other properties as needed ...
            }).ToList();
            return ApiResponse<IEnumerable<UserDto>>.SuccessResponse(userDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all providers");
            return ApiResponse<IEnumerable<UserDto>>.ErrorResponse($"Failed to get providers: {ex.Message}");
        }
    }

    public async Task<ApiResponse<ProviderDto>> CreateProviderAsync(CreateProviderDto createDto)
    {
        try
        {
            var provider = new User
            {
                Id = Guid.NewGuid(),
                FirstName = createDto.FirstName,
                LastName = createDto.LastName,
                Email = createDto.Email,
                Phone = createDto.PhoneNumber,
                UserType = "Provider",
                IsActive = true,
                IsEmailVerified = false,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.CreateAsync(provider);

            return ApiResponse<ProviderDto>.SuccessResponse(MapToProviderDto(provider));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating provider");
            return ApiResponse<ProviderDto>.ErrorResponse($"Failed to create provider: {ex.Message}");
        }
    }

    public async Task<ApiResponse<ProviderDto>> UpdateProviderAsync(Guid providerId, UpdateProviderDto updateDto)
    {
        try
        {
            var provider = await _userRepository.GetByIdAsync(providerId);
            if (provider == null || provider.UserType != "Provider")
                return ApiResponse<ProviderDto>.ErrorResponse("Provider not found");

            // Update provider properties
            if (!string.IsNullOrEmpty(updateDto.FirstName))
                provider.FirstName = updateDto.FirstName;
            if (!string.IsNullOrEmpty(updateDto.LastName))
                provider.LastName = updateDto.LastName;
            if (!string.IsNullOrEmpty(updateDto.Email))
                provider.Email = updateDto.Email;
            if (!string.IsNullOrEmpty(updateDto.PhoneNumber))
                provider.Phone = updateDto.PhoneNumber;

            provider.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(provider);

            return ApiResponse<ProviderDto>.SuccessResponse(MapToProviderDto(provider));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating provider: {ProviderId}", providerId);
            return ApiResponse<ProviderDto>.ErrorResponse($"Failed to update provider: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> DeleteProviderAsync(Guid providerId)
    {
        try
        {
            var provider = await _userRepository.GetByIdAsync(providerId);
            if (provider == null || provider.UserType != "Provider")
                return ApiResponse<bool>.ErrorResponse("Provider not found");

            provider.IsActive = false;
            provider.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(provider);

            return ApiResponse<bool>.SuccessResponse(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting provider: {ProviderId}", providerId);
            return ApiResponse<bool>.ErrorResponse($"Failed to delete provider: {ex.Message}");
        }
    }

    public async Task<ApiResponse<ProviderDto>> VerifyProviderAsync(Guid providerId)
    {
        try
        {
            var provider = await _userRepository.GetByIdAsync(providerId);
            if (provider == null || provider.UserType != "Provider")
                return ApiResponse<ProviderDto>.ErrorResponse("Provider not found");

            // In a real implementation, this would involve verification logic
            provider.IsEmailVerified = true;
            provider.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(provider);

            return ApiResponse<ProviderDto>.SuccessResponse(MapToProviderDto(provider));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying provider: {ProviderId}", providerId);
            return ApiResponse<ProviderDto>.ErrorResponse($"Failed to verify provider: {ex.Message}");
        }
    }

    // Provider schedule operations
    public async Task<ApiResponse<ProviderScheduleDto>> GetProviderScheduleAsync(Guid providerId)
    {
        try
        {
            // This would typically fetch from a schedule repository
            var schedule = new ProviderScheduleDto
            {
                ProviderId = providerId.ToString(),
                WeeklySchedule = new List<WeeklyScheduleDto>
                {
                    new WeeklyScheduleDto { DayOfWeek = "1", DayName = "Monday", IsAvailable = true, StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(17) },
                    new WeeklyScheduleDto { DayOfWeek = "2", DayName = "Tuesday", IsAvailable = true, StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(17) },
                    new WeeklyScheduleDto { DayOfWeek = "3", DayName = "Wednesday", IsAvailable = true, StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(17) },
                    new WeeklyScheduleDto { DayOfWeek = "4", DayName = "Thursday", IsAvailable = true, StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(17) },
                    new WeeklyScheduleDto { DayOfWeek = "5", DayName = "Friday", IsAvailable = true, StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(17) },
                    new WeeklyScheduleDto { DayOfWeek = "6", DayName = "Saturday", IsAvailable = false, StartTime = TimeSpan.Zero, EndTime = TimeSpan.Zero },
                    new WeeklyScheduleDto { DayOfWeek = "0", DayName = "Sunday", IsAvailable = false, StartTime = TimeSpan.Zero, EndTime = TimeSpan.Zero }
                },
                DefaultDurationMinutes = 30,
                IsActive = true
            };

            return ApiResponse<ProviderScheduleDto>.SuccessResponse(schedule);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting provider schedule: {ProviderId}", providerId);
            return ApiResponse<ProviderScheduleDto>.ErrorResponse($"Failed to get provider schedule: {ex.Message}");
        }
    }

    public async Task<ApiResponse<ProviderScheduleDto>> UpdateProviderScheduleAsync(Guid providerId, UpdateProviderScheduleDto scheduleDto)
    {
        try
        {
            // This would typically update a schedule repository
            var updatedSchedule = new ProviderScheduleDto
            {
                ProviderId = providerId.ToString(),
                WeeklySchedule = scheduleDto.WeeklySchedule,
                AvailableDates = scheduleDto.AvailableDates,
                UnavailableDates = scheduleDto.UnavailableDates,
                DefaultDurationMinutes = scheduleDto.DefaultDurationMinutes,
                IsActive = scheduleDto.IsActive
            };

            return ApiResponse<ProviderScheduleDto>.SuccessResponse(updatedSchedule);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating provider schedule: {ProviderId}", providerId);
            return ApiResponse<ProviderScheduleDto>.ErrorResponse($"Failed to update provider schedule: {ex.Message}");
        }
    }

    // User statistics
    public async Task<ApiResponse<object>> GetUserStatsAsync(Guid userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return ApiResponse<object>.ErrorResponse("User not found");

            var stats = user.UserType == "Patient" 
                ? await GetPatientStatsAsync(userId)
                : await GetProviderStatsAsync(userId);

            return ApiResponse<object>.SuccessResponse(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user stats: {UserId}", userId);
            return ApiResponse<object>.ErrorResponse($"Failed to get user stats: {ex.Message}");
        }
    }

    // Provider reviews
    public async Task<ApiResponse<IEnumerable<ReviewDto>>> GetProviderReviewsAsync(Guid providerId)
    {
        try
        {
            // This would typically fetch from a reviews repository
            var reviews = new List<ReviewDto>
            {
                new ReviewDto
                {
                    Id = Guid.NewGuid().ToString(),
                    ProviderId = providerId.ToString(),
                    PatientId = Guid.NewGuid().ToString(),
                    PatientName = "John Doe",
                    Rating = 5,
                    Comment = "Excellent consultation. Very professional and knowledgeable.",
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                },
                new ReviewDto
                {
                    Id = Guid.NewGuid().ToString(),
                    ProviderId = providerId.ToString(),
                    PatientId = Guid.NewGuid().ToString(),
                    PatientName = "Jane Smith",
                    Rating = 4,
                    Comment = "Good experience. Would recommend.",
                    CreatedAt = DateTime.UtcNow.AddDays(-10)
                }
            };

            return ApiResponse<IEnumerable<ReviewDto>>.SuccessResponse(reviews);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting provider reviews: {ProviderId}", providerId);
            return ApiResponse<IEnumerable<ReviewDto>>.ErrorResponse($"Failed to get provider reviews: {ex.Message}");
        }
    }

    public async Task<ApiResponse<ReviewDto>> AddProviderReviewAsync(Guid providerId, Guid userId, AddReviewDto reviewDto)
    {
        try
        {
            var review = new ReviewDto
            {
                Id = Guid.NewGuid().ToString(),
                ProviderId = providerId.ToString(),
                PatientId = userId.ToString(),
                PatientName = "Current User", // Would get from user data
                Rating = reviewDto.Rating,
                Comment = reviewDto.Comment,
                CreatedAt = DateTime.UtcNow
            };

            return ApiResponse<ReviewDto>.SuccessResponse(review);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding provider review: {ProviderId}", providerId);
            return ApiResponse<ReviewDto>.ErrorResponse($"Failed to add review: {ex.Message}");
        }
    }

    // Notifications
    public async Task<ApiResponse<IEnumerable<NotificationDto>>> GetUserNotificationsAsync(Guid userId)
    {
        try
        {
            // This would typically fetch from a notifications repository
            var notifications = new List<NotificationDto>
            {
                new NotificationDto
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId.ToString(),
                    Title = "Appointment Reminder",
                    Message = "Your appointment with Dr. Smith is in 1 hour.",
                    Type = "appointment",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddHours(-1)
                },
                new NotificationDto
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId.ToString(),
                    Title = "Payment Successful",
                    Message = "Your payment of $75 has been processed successfully.",
                    Type = "payment",
                    IsRead = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                }
            };

            return ApiResponse<IEnumerable<NotificationDto>>.SuccessResponse(notifications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user notifications: {UserId}", userId);
            return ApiResponse<IEnumerable<NotificationDto>>.ErrorResponse($"Failed to get notifications: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> MarkNotificationAsReadAsync(Guid notificationId)
    {
        try
        {
            // This would typically update a notifications repository
            return ApiResponse<bool>.SuccessResponse(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification as read: {NotificationId}", notificationId);
            return ApiResponse<bool>.ErrorResponse($"Failed to mark notification as read: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> MarkAllNotificationsAsReadAsync(Guid userId)
    {
        try
        {
            // This would typically update a notifications repository
            return ApiResponse<bool>.SuccessResponse(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read: {UserId}", userId);
            return ApiResponse<bool>.ErrorResponse($"Failed to mark all notifications as read: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> DeleteNotificationAsync(Guid notificationId)
    {
        try
        {
            // This would typically delete from a notifications repository
            return ApiResponse<bool>.SuccessResponse(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting notification: {NotificationId}", notificationId);
            return ApiResponse<bool>.ErrorResponse($"Failed to delete notification: {ex.Message}");
        }
    }

    // User preferences
    public async Task<ApiResponse<object>> GetUserPreferencesAsync(Guid userId)
    {
        try
        {
            // This would typically fetch from a preferences repository
            var preferences = new
            {
                EmailNotifications = true,
                SMSNotifications = false,
                AppointmentReminders = true,
                MarketingEmails = false,
                Language = "en",
                TimeZone = "UTC"
            };

            return ApiResponse<object>.SuccessResponse(preferences);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user preferences: {UserId}", userId);
            return ApiResponse<object>.ErrorResponse($"Failed to get preferences: {ex.Message}");
        }
    }

    public async Task<ApiResponse<object>> UpdateUserPreferencesAsync(Guid userId, UpdateUserPreferencesDto preferencesDto)
    {
        try
        {
            // This would typically update a preferences repository
            return ApiResponse<object>.SuccessResponse(preferencesDto.Preferences);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user preferences: {UserId}", userId);
            return ApiResponse<object>.ErrorResponse($"Failed to update preferences: {ex.Message}");
        }
    }

    // Password operations
    private string HashPassword(string password)
    {
        // Simple password hashing implementation - in production use BCrypt.Net.BCrypt
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password));
    }

    private bool VerifyPassword(string password, string hash)
    {
        // Simple password verification - in production use BCrypt.Net.BCrypt
        var hashedPassword = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password));
        return hashedPassword == hash;
    }

    public async Task<ApiResponse<bool>> RequestPasswordResetAsync(string email)
    {
        try
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
                return ApiResponse<bool>.ErrorResponse("User not found");

            // In a real implementation, send password reset email
            await _notificationService.SendPasswordResetEmailAsync(email, "reset-token");

            return ApiResponse<bool>.SuccessResponse(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting password reset: {Email}", email);
            return ApiResponse<bool>.ErrorResponse($"Failed to request password reset: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> ResetPasswordAsync(ResetPasswordDto resetDto)
    {
        try
        {
            // In a real implementation, verify token and update password
            return ApiResponse<bool>.SuccessResponse(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password");
            return ApiResponse<bool>.ErrorResponse($"Failed to reset password: {ex.Message}");
        }
    }

    // Email verification
    public async Task<ApiResponse<bool>> VerifyEmailAsync(string token)
    {
        try
        {
            // In a real implementation, verify email token
            return ApiResponse<bool>.SuccessResponse(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying email");
            return ApiResponse<bool>.ErrorResponse($"Failed to verify email: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> SendEmailVerificationAsync(string email)
    {
        try
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                return ApiResponse<bool>.ErrorResponse("User not found");
            }

            var verificationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            await _notificationService.SendEmailVerificationAsync(email, user.UserName, verificationToken);

            return ApiResponse<bool>.SuccessResponse(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email verification to {Email}", email);
            return ApiResponse<bool>.ErrorResponse($"Failed to send email verification: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> ResendEmailVerificationAsync(Guid userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return ApiResponse<bool>.ErrorResponse("User not found");
            }

            var verificationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            await _notificationService.SendEmailVerificationAsync(user.Email, user.UserName, verificationToken);

            return ApiResponse<bool>.SuccessResponse(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resending email verification for user {UserId}", userId);
            return ApiResponse<bool>.ErrorResponse($"Failed to resend email verification: {ex.Message}");
        }
    }

    // Account management
    public async Task<ApiResponse<bool>> DeleteAccountAsync(Guid userId, string reason)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return ApiResponse<bool>.ErrorResponse("User not found");

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            return ApiResponse<bool>.SuccessResponse(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting account: {UserId}", userId);
            return ApiResponse<bool>.ErrorResponse($"Failed to delete account: {ex.Message}");
        }
    }

    // Helper methods
    private UserDto MapToUserDto(User user)
    {
        return new UserDto
        {
            Id = user.Id.ToString(),
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = $"{user.FirstName} {user.LastName}",
            Phone = user.Phone,
            DateOfBirth = user.DateOfBirth,
            Gender = user.Gender,
            ProfilePicture = user.ProfilePicture,
            UserType = user.UserType,
            IsActive = user.IsActive,
            IsEmailVerified = user.IsEmailVerified,
            IsPhoneVerified = user.IsPhoneVerified,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt != null ? user.UpdatedAt.Value : DateTime.UtcNow,
            LastLoginAt = user.LastLoginAt,
            StripeCustomerId = user.StripeCustomerId
        };
    }

    private PatientDto MapToPatientDto(User user)
    {
        return new PatientDto
        {
            Id = user.Id.ToString(),
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = $"{user.FirstName} {user.LastName}",
            Phone = user.Phone,
            DateOfBirth = user.DateOfBirth,
            Gender = user.Gender,
            ProfilePicture = user.ProfilePicture,
            UserType = user.UserType,
            IsActive = user.IsActive,
            IsEmailVerified = user.IsEmailVerified,
            IsPhoneVerified = user.IsPhoneVerified,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt != null ? user.UpdatedAt.Value : DateTime.UtcNow,
            LastLoginAt = user.LastLoginAt,
            StripeCustomerId = user.StripeCustomerId
        };
    }

    private ProviderDto MapToProviderDto(User user)
    {
        return new ProviderDto
        {
            Id = user.Id.ToString(),
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = $"{user.FirstName} {user.LastName}",
            Phone = user.Phone,
            DateOfBirth = user.DateOfBirth,
            Gender = user.Gender,
            ProfilePicture = user.ProfilePicture,
            UserType = user.UserType,
            IsActive = user.IsActive,
            IsEmailVerified = user.IsEmailVerified,
            IsPhoneVerified = user.IsPhoneVerified,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt != null ? user.UpdatedAt.Value : DateTime.UtcNow,
            LastLoginAt = user.LastLoginAt,
            StripeCustomerId = user.StripeCustomerId
        };
    }

    private async Task<object> GetPatientStatsAsync(Guid patientId)
    {
        // This would typically fetch from appointment and payment repositories
        return new
        {
            TotalAppointments = 15,
            CompletedAppointments = 12,
            CancelledAppointments = 2,
            TotalSpent = 450.00m
        };
    }

    private async Task<object> GetProviderStatsAsync(Guid providerId)
    {
        // This would typically fetch from appointment and payment repositories
        return new
        {
            TotalAppointments = 150,
            CompletedAppointments = 140,
            TotalEarnings = 10500.00m
        };
    }

    // === BEGIN INTERFACE STUBS ===
    public async Task<ApiResponse<UserDto>> GetUserAsync(string userId)
    {
        throw new NotImplementedException();
    }
    public async Task<ApiResponse<IEnumerable<UserDto>>> GetAllUsersAsync()
    {
        throw new NotImplementedException();
    }
    public async Task<ApiResponse<UserDto>> CreateUserAsync(CreateUserDto createUserDto)
    {
        throw new NotImplementedException();
    }
    public async Task<ApiResponse<MedicalHistoryDto>> GetMedicalHistoryAsync(string userId)
    {
        throw new NotImplementedException();
    }
    public async Task<ApiResponse<MedicalHistoryDto>> UpdateMedicalHistoryAsync(string userId, UpdateMedicalHistoryDto medicalHistoryDto)
    {
        throw new NotImplementedException();
    }
    public async Task<ApiResponse<IEnumerable<PaymentMethodDto>>> GetPaymentMethodsAsync(string userId)
    {
        throw new NotImplementedException();
    }
    public async Task<ApiResponse<PaymentMethodDto>> AddPaymentMethodAsync(string userId, AddPaymentMethodDto addPaymentMethodDto)
    {
        throw new NotImplementedException();
    }
    public async Task<ApiResponse<bool>> DeletePaymentMethodAsync(string userId, string paymentMethodId)
    {
        throw new NotImplementedException();
    }
    public async Task<ApiResponse<bool>> SetDefaultPaymentMethodAsync(string userId, string paymentMethodId)
    {
        throw new NotImplementedException();
    }
    // === END INTERFACE STUBS ===

    // === BEGIN INTERFACE OVERLOADS ===
    public async Task<ApiResponse<UserDto>> GetProviderByIdAsync(string providerId)
    {
        if (!Guid.TryParse(providerId, out var guid))
            return ApiResponse<UserDto>.ErrorResponse("Invalid provider ID");
        var result = await GetProviderByIdAsync(guid);
        // Map ProviderDto to UserDto if needed
        if (result is ApiResponse<ProviderDto> providerResponse && providerResponse.Data != null)
        {
            var userDto = new UserDto
            {
                Id = providerResponse.Data.Id,
                Email = providerResponse.Data.Email,
                FirstName = providerResponse.Data.FirstName,
                LastName = providerResponse.Data.LastName,
                // ... map other properties as needed ...
            };
            return ApiResponse<UserDto>.SuccessResponse(userDto);
        }
        return ApiResponse<UserDto>.ErrorResponse(result.Message);
    }
    public async Task<ApiResponse<UserDto>> UpdateProviderAsync(string providerId, UpdateProviderDto updateProviderDto)
    {
        if (!Guid.TryParse(providerId, out var guid))
            return ApiResponse<UserDto>.ErrorResponse("Invalid provider ID");
        var result = await UpdateProviderAsync(guid, updateProviderDto);
        // Map ProviderDto to UserDto if needed
        if (result is ApiResponse<ProviderDto> providerResponse && providerResponse.Data != null)
        {
            var userDto = new UserDto
            {
                Id = providerResponse.Data.Id,
                Email = providerResponse.Data.Email,
                FirstName = providerResponse.Data.FirstName,
                LastName = providerResponse.Data.LastName,
                // ... map other properties as needed ...
            };
            return ApiResponse<UserDto>.SuccessResponse(userDto);
        }
        return ApiResponse<UserDto>.ErrorResponse(result.Message);
    }
    // === END INTERFACE OVERLOADS ===
} 