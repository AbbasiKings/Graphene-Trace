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

        if (!await context.Users.AnyAsync(cancellationToken))
        {
            var admin = new User
            {
                FullName = "Super Admin",
                Email = "admin@graphene-trace.com",
                Role = UserRole.Admin,
                PasswordHash = SecurityUtils.HashPassword("Admin@123")
            };

            var clinician = new User
            {
                FullName = "Lead Clinician",
                Email = "clinician@graphene-trace.com",
                Role = UserRole.Clinician,
                PasswordHash = SecurityUtils.HashPassword("Clinician@123")
            };

            var patient = new User
            {
                FullName = "Primary Patient",
                Email = "patient@graphene-trace.com",
                Role = UserRole.Patient,
                PasswordHash = SecurityUtils.HashPassword("Patient@123"),
                AssignedClinician = clinician
            };

            context.Users.AddRange(admin, clinician, patient);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}

