using Microsoft.AspNetCore.Mvc;
using SmartTelehealth.Application.Interfaces;
using SmartTelehealth.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace SmartTelehealth.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserService userService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// Get current user profile
        /// </summary>
        [HttpGet("profile")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetCurrentUserProfile()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponse<UserDto>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                var result = await _userService.GetUserByIdAsync(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user profile");
                return StatusCode(500, new ApiResponse<UserDto>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Update current user profile
        /// </summary>
        [HttpPut("profile")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<UserDto>>> UpdateProfile([FromBody] UpdateUserDto updateDto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponse<UserDto>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                updateDto.Id = userId;
                var result = await _userService.UpdateUserAsync(userId, updateDto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile");
                return StatusCode(500, new ApiResponse<UserDto>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get user by ID (admin only)
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetUserById(string id)
        {
            try
            {
                var result = await _userService.GetUserByIdAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by ID: {UserId}", id);
                return StatusCode(500, new ApiResponse<UserDto>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get all users with pagination (admin only)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<PagedResult<UserDto>>>> GetAllUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? userType = null)
        {
            try
            {
                var result = await _userService.GetAllUsersAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users");
                return StatusCode(500, new ApiResponse<PagedResult<UserDto>>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Create new user (admin only)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser([FromBody] CreateUserDto createDto)
        {
            try
            {
                var result = await _userService.CreateUserAsync(createDto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return StatusCode(500, new ApiResponse<UserDto>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Update user (admin only)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser(string id, [FromBody] UpdateUserDto updateDto)
        {
            try
            {
                updateDto.Id = id;
                var result = await _userService.UpdateUserAsync(id, updateDto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user: {UserId}", id);
                return StatusCode(500, new ApiResponse<UserDto>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Delete user (admin only)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteUser(string id)
        {
            try
            {
                var result = await _userService.DeleteUserAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user: {UserId}", id);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get all providers
        /// </summary>
        [HttpGet("providers")]
        public async Task<ActionResult<ApiResponse<List<ProviderDto>>>> GetAllProviders()
        {
            try
            {
                var result = await _userService.GetAllProvidersAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all providers");
                return StatusCode(500, new ApiResponse<List<ProviderDto>>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get provider by ID
        /// </summary>
        [HttpGet("providers/{id}")]
        public async Task<ActionResult<ApiResponse<ProviderDto>>> GetProviderById(string id)
        {
            try
            {
                var result = await _userService.GetProviderByIdAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting provider by ID: {ProviderId}", id);
                return StatusCode(500, new ApiResponse<ProviderDto>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Update provider profile
        /// </summary>
        [HttpPut("providers/{id}")]
        [Authorize(Roles = "Provider")]
        public async Task<ActionResult<ApiResponse<ProviderDto>>> UpdateProviderProfile(string id, [FromBody] UpdateProviderDto updateDto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId != id)
                {
                    return Forbid();
                }

                updateDto.Id = id;
                var result = await _userService.UpdateProviderAsync(id, updateDto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating provider profile: {ProviderId}", id);
                return StatusCode(500, new ApiResponse<ProviderDto>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get user medical history
        /// </summary>
        [HttpGet("medical-history")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<MedicalHistoryDto>>> GetMedicalHistory()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponse<MedicalHistoryDto>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                var result = await _userService.GetMedicalHistoryAsync(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting medical history");
                return StatusCode(500, new ApiResponse<MedicalHistoryDto>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Update user medical history
        /// </summary>
        [HttpPut("medical-history")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<MedicalHistoryDto>>> UpdateMedicalHistory([FromBody] UpdateMedicalHistoryDto updateDto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponse<MedicalHistoryDto>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                updateDto.UserId = userId;
                var result = await _userService.UpdateMedicalHistoryAsync(userId, updateDto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating medical history");
                return StatusCode(500, new ApiResponse<MedicalHistoryDto>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get user payment methods
        /// </summary>
        [HttpGet("payment-methods")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<List<PaymentMethodDto>>>> GetPaymentMethods()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponse<List<PaymentMethodDto>>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                var result = await _userService.GetPaymentMethodsAsync(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment methods");
                return StatusCode(500, new ApiResponse<List<PaymentMethodDto>>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Add payment method
        /// </summary>
        [HttpPost("payment-methods")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<PaymentMethodDto>>> AddPaymentMethod([FromBody] CreatePaymentMethodDto createDto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponse<PaymentMethodDto>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                var addPaymentMethodDto = new AddPaymentMethodDto
                {
                    IsDefault = createDto.IsDefault
                };
                var result = await _userService.AddPaymentMethodAsync(userId, addPaymentMethodDto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding payment method");
                return StatusCode(500, new ApiResponse<PaymentMethodDto>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Delete payment method
        /// </summary>
        [HttpDelete("payment-methods/{id}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<bool>>> DeletePaymentMethod(string id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                var result = await _userService.DeletePaymentMethodAsync(userId, id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting payment method");
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Set default payment method
        /// </summary>
        [HttpPut("payment-methods/{id}/default")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<bool>>> SetDefaultPaymentMethod(string id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                var result = await _userService.SetDefaultPaymentMethodAsync(userId, id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting default payment method");
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }
    }
} 