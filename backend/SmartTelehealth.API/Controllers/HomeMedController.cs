using Microsoft.AspNetCore.Mvc;
using SmartTelehealth.Application.DTOs;
using SmartTelehealth.Application.Interfaces;

namespace SmartTelehealth.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HomeMedController : ControllerBase
{
    private readonly IHomeMedService _homeMedService;
    private readonly ILogger<HomeMedController> _logger;

    public HomeMedController(IHomeMedService homeMedService, ILogger<HomeMedController> logger)
    {
        _homeMedService = homeMedService;
        _logger = logger;
    }

    // Prescription Management
    [HttpPost("prescriptions")]
    public async Task<ActionResult<ApiResponse<PrescriptionDto>>> CreatePrescription([FromBody] CreatePrescriptionDto createDto)
    {
        var response = await _homeMedService.CreatePrescriptionAsync(createDto);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("prescriptions/{id}")]
    public async Task<ActionResult<ApiResponse<PrescriptionDto>>> GetPrescription(Guid id)
    {
        var response = await _homeMedService.GetPrescriptionAsync(id);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("users/{userId}/prescriptions")]
    public async Task<ActionResult<ApiResponse<IEnumerable<PrescriptionDto>>>> GetUserPrescriptions(Guid userId)
    {
        var response = await _homeMedService.GetUserPrescriptionsAsync(userId);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("providers/{providerId}/prescriptions")]
    public async Task<ActionResult<ApiResponse<IEnumerable<PrescriptionDto>>>> GetProviderPrescriptions(Guid providerId)
    {
        var response = await _homeMedService.GetProviderPrescriptionsAsync(providerId);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPut("prescriptions/{id}")]
    public async Task<ActionResult<ApiResponse<PrescriptionDto>>> UpdatePrescription(Guid id, [FromBody] UpdatePrescriptionDto updateDto)
    {
        var response = await _homeMedService.UpdatePrescriptionAsync(id, updateDto);
        return StatusCode(response.StatusCode, response);
    }

    [HttpDelete("prescriptions/{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeletePrescription(Guid id)
    {
        var response = await _homeMedService.DeletePrescriptionAsync(id);
        return StatusCode(response.StatusCode, response);
    }

    // Prescription Workflow
    [HttpPost("prescriptions/{id}/send-to-pharmacy")]
    public async Task<ActionResult<ApiResponse<bool>>> SendPrescriptionToPharmacy(Guid id)
    {
        var response = await _homeMedService.SendPrescriptionToPharmacyAsync(id);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost("prescriptions/{id}/confirm")]
    public async Task<ActionResult<ApiResponse<bool>>> ConfirmPrescription(Guid id, [FromQuery] string pharmacyReference)
    {
        var response = await _homeMedService.ConfirmPrescriptionAsync(id, pharmacyReference);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost("prescriptions/{id}/dispense")]
    public async Task<ActionResult<ApiResponse<bool>>> DispensePrescription(Guid id)
    {
        var response = await _homeMedService.DispensePrescriptionAsync(id);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost("prescriptions/{id}/ship")]
    public async Task<ActionResult<ApiResponse<bool>>> ShipPrescription(Guid id, [FromQuery] string trackingNumber)
    {
        var response = await _homeMedService.ShipPrescriptionAsync(id, trackingNumber);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost("prescriptions/{id}/deliver")]
    public async Task<ActionResult<ApiResponse<bool>>> DeliverPrescription(Guid id)
    {
        var response = await _homeMedService.DeliverPrescriptionAsync(id);
        return StatusCode(response.StatusCode, response);
    }

    // Medication Shipment Management
    [HttpPost("shipments")]
    public async Task<ActionResult<ApiResponse<MedicationShipmentDto>>> CreateShipment([FromBody] CreateMedicationShipmentDto createDto)
    {
        var response = await _homeMedService.CreateShipmentAsync(createDto);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("shipments/{id}")]
    public async Task<ActionResult<ApiResponse<MedicationShipmentDto>>> GetShipment(Guid id)
    {
        var response = await _homeMedService.GetShipmentAsync(id);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("users/{userId}/shipments")]
    public async Task<ActionResult<ApiResponse<IEnumerable<MedicationShipmentDto>>>> GetUserShipments(Guid userId)
    {
        var response = await _homeMedService.GetUserShipmentsAsync(userId);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPut("shipments/{id}")]
    public async Task<ActionResult<ApiResponse<MedicationShipmentDto>>> UpdateShipment(Guid id, [FromBody] UpdateMedicationShipmentDto updateDto)
    {
        var response = await _homeMedService.UpdateShipmentAsync(id, updateDto);
        return StatusCode(response.StatusCode, response);
    }

    [HttpDelete("shipments/{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteShipment(Guid id)
    {
        var response = await _homeMedService.DeleteShipmentAsync(id);
        return StatusCode(response.StatusCode, response);
    }

    // Shipment Workflow
    [HttpPost("shipments/{id}/process")]
    public async Task<ActionResult<ApiResponse<bool>>> ProcessShipment(Guid id)
    {
        var response = await _homeMedService.ProcessShipmentAsync(id);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost("shipments/{id}/ship")]
    public async Task<ActionResult<ApiResponse<bool>>> ShipMedication(Guid id, [FromQuery] string trackingNumber, [FromQuery] string carrier)
    {
        var response = await _homeMedService.ShipMedicationAsync(id, trackingNumber, carrier);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost("shipments/{id}/deliver")]
    public async Task<ActionResult<ApiResponse<bool>>> DeliverMedication(Guid id)
    {
        var response = await _homeMedService.DeliverMedicationAsync(id);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost("shipments/{id}/return")]
    public async Task<ActionResult<ApiResponse<bool>>> ReturnShipment(Guid id, [FromQuery] string reason)
    {
        var response = await _homeMedService.ReturnShipmentAsync(id, reason);
        return StatusCode(response.StatusCode, response);
    }

    // Tracking and Status
    [HttpGet("tracking/{trackingNumber}")]
    public async Task<ActionResult<ApiResponse<string>>> GetTrackingStatus(string trackingNumber)
    {
        var response = await _homeMedService.GetTrackingStatusAsync(trackingNumber);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("shipments/{id}/estimated-delivery")]
    public async Task<ActionResult<ApiResponse<DateTime>>> GetEstimatedDelivery(Guid id)
    {
        var response = await _homeMedService.GetEstimatedDeliveryAsync(id);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPut("shipments/{id}/tracking")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateTrackingInfo(Guid id, [FromQuery] string trackingNumber, [FromQuery] string status)
    {
        var response = await _homeMedService.UpdateTrackingInfoAsync(id, trackingNumber, status);
        return StatusCode(response.StatusCode, response);
    }

    // Analytics and Reporting
    [HttpGet("analytics/prescriptions")]
    public async Task<ActionResult<ApiResponse<PrescriptionAnalyticsDto>>> GetPrescriptionAnalytics([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var response = await _homeMedService.GetPrescriptionAnalyticsAsync(startDate, endDate);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("analytics/shipments")]
    public async Task<ActionResult<ApiResponse<MedicationShipmentAnalyticsDto>>> GetShipmentAnalytics([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var response = await _homeMedService.GetShipmentAnalyticsAsync(startDate, endDate);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("reports/prescriptions")]
    public async Task<ActionResult<ApiResponse<byte[]>>> GeneratePrescriptionReport([FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] string format = "pdf")
    {
        var response = await _homeMedService.GeneratePrescriptionReportAsync(startDate, endDate, format);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("reports/shipments")]
    public async Task<ActionResult<ApiResponse<byte[]>>> GenerateShipmentReport([FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] string format = "pdf")
    {
        var response = await _homeMedService.GenerateShipmentReportAsync(startDate, endDate, format);
        return StatusCode(response.StatusCode, response);
    }

    // Pharmacy Integration
    [HttpGet("pharmacy/integration")]
    public async Task<ActionResult<ApiResponse<PharmacyIntegrationDto>>> GetPharmacyIntegration()
    {
        var response = await _homeMedService.GetPharmacyIntegrationAsync();
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost("pharmacy/test-connection")]
    public async Task<ActionResult<ApiResponse<bool>>> TestPharmacyConnection()
    {
        var response = await _homeMedService.TestPharmacyConnectionAsync();
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost("pharmacy/sync/prescriptions")]
    public async Task<ActionResult<ApiResponse<bool>>> SyncPrescriptions()
    {
        var response = await _homeMedService.SyncPrescriptionsAsync();
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost("pharmacy/sync/shipments")]
    public async Task<ActionResult<ApiResponse<bool>>> SyncShipments()
    {
        var response = await _homeMedService.SyncShipmentsAsync();
        return StatusCode(response.StatusCode, response);
    }

    // Refill Management
    [HttpPost("prescriptions/{id}/refill-request")]
    public async Task<ActionResult<ApiResponse<PrescriptionDto>>> CreateRefillRequest(Guid id)
    {
        var response = await _homeMedService.CreateRefillRequestAsync(id);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("users/{userId}/refill-requests")]
    public async Task<ActionResult<ApiResponse<IEnumerable<PrescriptionDto>>>> GetRefillRequests(Guid userId)
    {
        var response = await _homeMedService.GetRefillRequestsAsync(userId);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost("prescriptions/{id}/approve-refill")]
    public async Task<ActionResult<ApiResponse<bool>>> ApproveRefillRequest(Guid id)
    {
        var response = await _homeMedService.ApproveRefillRequestAsync(id);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost("prescriptions/{id}/deny-refill")]
    public async Task<ActionResult<ApiResponse<bool>>> DenyRefillRequest(Guid id, [FromQuery] string reason)
    {
        var response = await _homeMedService.DenyRefillRequestAsync(id, reason);
        return StatusCode(response.StatusCode, response);
    }

    // Inventory Management
    [HttpGet("inventory/availability")]
    public async Task<ActionResult<ApiResponse<bool>>> CheckMedicationAvailability([FromQuery] string medicationName, [FromQuery] string dosage)
    {
        var response = await _homeMedService.CheckMedicationAvailabilityAsync(medicationName, dosage);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("inventory/price")]
    public async Task<ActionResult<ApiResponse<decimal>>> GetMedicationPrice([FromQuery] string medicationName, [FromQuery] string dosage)
    {
        var response = await _homeMedService.GetMedicationPriceAsync(medicationName, dosage);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost("inventory/reserve")]
    public async Task<ActionResult<ApiResponse<bool>>> ReserveMedication([FromQuery] string medicationName, [FromQuery] string dosage, [FromQuery] int quantity)
    {
        var response = await _homeMedService.ReserveMedicationAsync(medicationName, dosage, quantity);
        return StatusCode(response.StatusCode, response);
    }

    // Auto-dispatch
    [HttpPost("prescriptions/{id}/auto-dispatch")]
    public async Task<ActionResult<ApiResponse<bool>>> TriggerAutoDispatch(Guid id)
    {
        var response = await _homeMedService.TriggerAutoDispatchAsync(id);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost("auto-dispatch/process-queue")]
    public async Task<ActionResult<ApiResponse<bool>>> ProcessAutoDispatchQueue()
    {
        var response = await _homeMedService.ProcessAutoDispatchQueueAsync();
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("auto-dispatch/queue")]
    public async Task<ActionResult<ApiResponse<IEnumerable<PrescriptionDto>>>> GetAutoDispatchQueue()
    {
        var response = await _homeMedService.GetAutoDispatchQueueAsync();
        return StatusCode(response.StatusCode, response);
    }
} 