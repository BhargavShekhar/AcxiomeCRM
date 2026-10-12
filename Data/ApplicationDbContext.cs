using AcxiomCRM.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
namespace AcxiomCRM.Data;
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options) {
 public DbSet<Customer> Customers => Set<Customer>(); public DbSet<Lead> Leads => Set<Lead>(); public DbSet<Opportunity> Opportunities => Set<Opportunity>(); public DbSet<FollowUp> FollowUps => Set<FollowUp>(); public DbSet<Activity> Activities => Set<Activity>(); public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
 protected override void OnModelCreating(ModelBuilder b) { base.OnModelCreating(b); b.Entity<Customer>().HasIndex(x=>x.Email).IsUnique(); b.Entity<Customer>().HasIndex(x=>x.Phone).IsUnique(); b.Entity<Customer>().HasIndex(x=>x.CustomerCode).IsUnique(); b.Entity<Lead>().HasIndex(x=>x.LeadCode).IsUnique(); b.Entity<Opportunity>().Property(x=>x.Amount).HasPrecision(18,2); b.Entity<Lead>().Property(x=>x.ExpectedValue).HasPrecision(18,2); }
}
