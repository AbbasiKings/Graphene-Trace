using GrapheneTrace.Core.Enums;
using GrapheneTrace.Core.Models;
using GrapheneTrace.Core.Utils;
using Microsoft.EntityFrameworkCore;

namespace GrapheneTrace.Api.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext context, CancellationToken cancellationToken = default)
    {
        await context.Database.EnsureCreatedAsync(cancellationToken);

        var adminId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var clinicianId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var patientId = Guid.Parse("a521b16d-55fe-49cf-a128-30ac05491a97");

        var admin = await context.Users.FirstOrDefaultAsync(u => u.Email == "admin@graphene-trace.com", cancellationToken);
        if (admin is null)
        {
            admin = new User
            {
                Id = adminId,
                FullName = "Super Admin",
                Email = "admin@graphene-trace.com",
                Role = UserRole.Admin,
                PasswordHash = SecurityUtils.HashPassword("Admin@123")
            };
            context.Users.Add(admin);
        }

        var clinician = await context.Users.FirstOrDefaultAsync(u => u.Email == "clinician@graphene-trace.com", cancellationToken);
        if (clinician is null)
        {
            clinician = new User
            {
                Id = clinicianId,
                FullName = "Lead Clinician",
                Email = "clinician@graphene-trace.com",
                Role = UserRole.Clinician,
                PasswordHash = SecurityUtils.HashPassword("Clinician@123")
            };
            context.Users.Add(clinician);
        }

        // First check if patient exists with the correct ID
        var patient = await context.Users.FirstOrDefaultAsync(u => u.Id == patientId, cancellationToken);
        
        if (patient is null)
        {
            // Check if patient exists with same email but different ID
            var existingPatient = await context.Users.FirstOrDefaultAsync(u => u.Email == "patient@graphene-trace.com" && u.Role == UserRole.Patient, cancellationToken);
            
            if (existingPatient is not null)
            {
                // Delete old patient and create new one with correct ID
                context.Users.Remove(existingPatient);
                await context.SaveChangesAsync(cancellationToken);
            }
            
            // Create new patient with correct ID
            patient = new User
            {
                Id = patientId,
                FullName = "Primary Patient",
                Email = "patient@graphene-trace.com",
                Role = UserRole.Patient,
                PasswordHash = SecurityUtils.HashPassword("Patient@123"),
                AssignedClinicianId = clinician.Id
            };
            context.Users.Add(patient);
        }
        else
        {
            // Patient exists with correct ID, just ensure assignment
            if (patient.AssignedClinicianId != clinician.Id)
            {
                patient.AssignedClinicianId = clinician.Id;
            }
            // Ensure email matches
            if (patient.Email != "patient@graphene-trace.com")
            {
                patient.Email = "patient@graphene-trace.com";
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}

