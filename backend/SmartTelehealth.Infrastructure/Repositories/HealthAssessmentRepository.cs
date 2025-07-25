using Microsoft.EntityFrameworkCore;
using SmartTelehealth.Core.Entities;
using SmartTelehealth.Core.Interfaces;
using SmartTelehealth.Infrastructure.Data;

namespace SmartTelehealth.Infrastructure.Repositories;

public class HealthAssessmentRepository : IHealthAssessmentRepository
{
    private readonly ApplicationDbContext _context;
    
    public HealthAssessmentRepository(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<HealthAssessment> GetByIdAsync(Guid id)
    {
        return await _context.HealthAssessments
            .Include(h => h.User)
            .Include(h => h.Category)
            .Include(h => h.Provider)
            .FirstOrDefaultAsync(h => h.Id == id);
    }
    
    public async Task<IEnumerable<HealthAssessment>> GetByUserIdAsync(Guid userId)
    {
        return await _context.HealthAssessments
            .Include(h => h.Category)
            .Include(h => h.Provider)
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync();
    }
    
    public async Task<IEnumerable<HealthAssessment>> GetByCategoryIdAsync(Guid categoryId)
    {
        return await _context.HealthAssessments
            .Include(h => h.User)
            .Include(h => h.Provider)
            .Where(h => h.CategoryId == categoryId)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync();
    }
    
    public async Task<IEnumerable<HealthAssessment>> GetPendingAssessmentsAsync()
    {
        return await _context.HealthAssessments
            .Include(h => h.User)
            .Include(h => h.Category)
            .Include(h => h.Provider)
            .Where(h => h.Status == HealthAssessment.AssessmentStatus.Pending)
            .OrderBy(h => h.CreatedAt)
            .ToListAsync();
    }
    
    public async Task<HealthAssessment> CreateAsync(HealthAssessment assessment)
    {
        _context.HealthAssessments.Add(assessment);
        await _context.SaveChangesAsync();
        return assessment;
    }
    
    public async Task<HealthAssessment> UpdateAsync(HealthAssessment assessment)
    {
        _context.HealthAssessments.Update(assessment);
        await _context.SaveChangesAsync();
        return assessment;
    }
    
    public async Task<bool> DeleteAsync(Guid id)
    {
        var assessment = await _context.HealthAssessments.FindAsync(id);
        if (assessment == null)
            return false;
            
        _context.HealthAssessments.Remove(assessment);
        await _context.SaveChangesAsync();
        return true;
    }
} 