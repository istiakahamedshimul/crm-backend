using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Data;

public class CrmDbContext(DbContextOptions<CrmDbContext> options) : DbContext(options)
{
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<FollowUp> FollowUps => Set<FollowUp>();
    public DbSet<FollowUpProof> FollowUpProofs => Set<FollowUpProof>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<SubGroup> SubGroups => Set<SubGroup>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Commission> Commissions => Set<Commission>();
    public DbSet<CommissionRule> CommissionRules => Set<CommissionRule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().HasIndex(x => x.Name).IsUnique();
        modelBuilder.Entity<User>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<Customer>().HasIndex(x => x.LeadId).IsUnique();
        modelBuilder.Entity<Invoice>().HasIndex(x => x.InvoiceNumber).IsUnique();
        modelBuilder.Entity<SubGroup>().HasIndex(x => x.Name).IsUnique();
        modelBuilder.Entity<Project>().HasOne(x => x.SubGroup).WithMany(x => x.Projects).HasForeignKey(x => x.SubGroupId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Customer>().HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<Customer>().HasOne(x => x.Lead).WithMany().HasForeignKey(x => x.LeadId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Commission>().HasOne(x => x.Payment).WithMany().HasForeignKey(x => x.PaymentId);
        modelBuilder.Entity<Payment>().HasOne(x => x.VerifiedBy).WithMany().HasForeignKey(x => x.VerifiedById).OnDelete(DeleteBehavior.Restrict);
    }
}
