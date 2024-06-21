using System;
using System.Configuration;
using Microsoft.EntityFrameworkCore;
using AccountingManagement.DataAccess.Entities;
using Task = AccountingManagement.DataAccess.Entities.Task;

namespace AccountingManagement.DataAccess
{
    public class AccountingManagementDbContext : DbContext
    {
        public DbSet<UserAccount> UserAccounts { get; set; }
        public DbSet<Business> Businesses { get; set; }
        public DbSet<Owner> Owners { get; set; }
        public DbSet<BusinessOwner> BusinessOwners { get; set; }
        public DbSet<Note> Notes { get; set; }
        public DbSet<BusinessInfo> BusinessInfos { get; set; }
        public DbSet<BankAccount> BankAccounts { get; set; }
        public DbSet<PayrollAccount> PayrollAccounts { get; set; }
        public DbSet<PayrollPeriodLookup> PayrollPeriodLookups { get; set; }
        public DbSet<PayrollAccountRecord> PayrollAccountRecords { get; set; }
        public DbSet<PayrollYearEndRecord> PayrollYearEndRecords { get; set; }
        public DbSet<PayrollPayoutRecord> PayrollPayoutRecords { get; set; }
        public DbSet<TaxAccount> TaxAccounts { get; set; }
        public DbSet<TaxAccountWithInstalment> TaxAccountWithInstalments { get; set; }
        public DbSet<TaxFilingLog> TaxFilingLogs { get; set; }
        public DbSet<TaxInstalmentLog> TaxInstalmentLogs { get; set; }
        public DbSet<PersonalTaxAccount> PersonalTaxAccounts { get; set; }
        public DbSet<PersonalTaxAccountLog> PersonalTaxAccountLogs { get; set; }
        public DbSet<ClientPayment> ClientPayments { get; set; }
        public DbSet<ClientPaymentLog> ClientPaymentLogs { get; set; }
        public DbSet<Work> Works { get; set; }
        public DbSet<Task> Tasks { get; set; }
        public DbSet<EmailSender> EmailSenders { get; set; }
        public DbSet<EmailTemplate> EmailTemplates { get; set; }


        public AccountingManagementDbContext()
        { }

        public AccountingManagementDbContext(DbContextOptions options) : base(options)
        { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["HRKAccounting"].ConnectionString;
            if (optionsBuilder.IsConfigured == false)
            {
                optionsBuilder.UseSqlServer(connectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserAccount>(a =>
            {
                a.HasKey(x => x.Id);
                a.ToTable("UserAccount");
            });

            modelBuilder.Entity<Business>(b => 
            {
                b.HasKey(x => x.Id);
                b.ToTable("Business");
            });

            modelBuilder.Entity<Owner>(o => 
            {
                o.HasKey(x => x.Id);
                o.ToTable("Owner");
            });

            modelBuilder.Entity<BusinessOwner>(bo => 
            {
                bo.HasKey(x => new { x.BusinessId, x.OwnerId } );

                bo.Property(x => x.Id).ValueGeneratedOnAdd();

                bo.HasOne(bo => bo.Business).WithMany(b => b.BusinessOwners)
                    .HasForeignKey(bo => bo.BusinessId);

                bo.HasOne(bo => bo.Owner).WithMany(o => o.BusinessOwners)
                    .HasForeignKey(bo => bo.OwnerId);

                bo.ToTable("BusinessOwner");
            });

            modelBuilder.Entity<BusinessInfo>(i =>
            {
                i.HasKey(x => x.Id);
                i.Property(x => x.Id).ValueGeneratedOnAdd();

                i.HasIndex(x => x.BusinessId);

                i.ToTable("BusinessInfo");
            });

            modelBuilder.Entity<Note>(x => 
            {
                x.HasKey(x => x.Id);
                x.Property(x => x.Id).ValueGeneratedOnAdd();

                x.ToTable("Note");
            });

            modelBuilder.Entity<BankAccount>(x => 
            {
                x.HasKey(x => x.Id);

                x.ToTable("BankAccount");
            });

            modelBuilder.Entity<PayrollAccount>(pa => 
            {
                pa.HasKey(x => x.Id);

                pa.HasOne(pa => pa.Business).WithOne(b => b.PayrollAccount);

                pa.ToTable("PayrollAccount");
            });

            modelBuilder.Entity<PayrollPeriodLookup>(x => 
            {
                x.HasKey(x => x.Id);
                x.Property(x => x.Id).ValueGeneratedOnAdd();

                x.ToTable("PayrollPeriodLookup");
            });

            modelBuilder.Entity<PayrollAccountRecord>(r => 
            {
                r.HasKey(r => r.Id);

                r.HasOne(r => r.PayrollAccount).WithMany(pa => pa.PayrollAccountRecords)
                    .HasForeignKey(r => r.PayrollAccountId);

                r.ToTable("PayrollAccountRecord");
            });

            modelBuilder.Entity<PayrollYearEndRecord>(r =>
            {
                r.HasKey(r => r.Id);
                r.Property(r => r.Id).ValueGeneratedOnAdd();

                r.ToTable("PayrollYearEndRecord");
            });

            modelBuilder.Entity<PayrollPayoutRecord>(r =>
            {
                r.HasKey(r => r.Id);

                r.HasOne(r => r.PayrollAccount).WithMany(pa => pa.PayrollPayoutRecords)
                    .HasForeignKey(r => r.PayrollAccountId);

                r.ToTable("PayrollPayoutRecord");
            });

            modelBuilder.Entity<TaxAccount>(a =>
            {
                a.HasKey(x => x.Id);
                a.ToTable("TaxAccount");

                a.Property("AccountType").HasColumnType("varchar(15)");
            });

            modelBuilder.Entity<TaxAccountWithInstalment>(a => 
            {
                a.HasKey(x => x.Id);
                a.ToTable("TaxAccountWithInstalment");

                a.Property("AccountType").HasColumnType("varchar(15)");
            });

            modelBuilder.Entity<TaxFilingLog>(l =>
            {
                l.HasKey(x => x.Id);
                l.Property(x => x.Id).ValueGeneratedOnAdd();

                l.Property("AccountType").HasColumnType("varchar(15)");

                l.ToTable("TaxFilingLog");
            });

            modelBuilder.Entity<TaxInstalmentLog>(l => 
            {
                l.HasKey(x => x.Id);
                l.Property(x => x.Id).ValueGeneratedOnAdd();

                l.ToTable("TaxInstalmentLog");
            });

            modelBuilder.Entity<PersonalTaxAccount>(a =>
            {
                a.HasKey(x => x.Id);
                a.ToTable("PersonalTaxAccount");
                a.Property("TaxType").HasColumnType("varchar(15)");
            });

            modelBuilder.Entity<PersonalTaxAccountLog>(l =>
            {
                l.HasKey(x => x.Id);
                l.Property(x => x.Id).ValueGeneratedOnAdd();
                l.Property("TaxType").HasColumnType("varchar(15)");

                l.HasOne(x => x.Owner).WithMany();

                l.ToTable("PersonalTaxAccountLog");
            });

            modelBuilder.Entity<Work>(w => 
            {
                w.HasKey(x => x.Id);

                w.HasOne(x => x.Business).WithOne(b => b.Work)
                    .HasForeignKey<Work>(x => x.BusinessId);

                w.ToTable("Work");
            });

            modelBuilder.Entity<Task>(t => 
            {
                t.HasKey(x => x.Id);

                t.HasOne(x => x.Work).WithMany(w => w.Tasks)
                    .HasForeignKey(x => x.WorkId);

                t.ToTable("Task");
            });

            modelBuilder.Entity<EmailSender>(t =>
            {
                t.HasKey(x => x.Id);

                t.ToTable("EmailSender");
            });

            modelBuilder.Entity<EmailTemplate>(t =>
            {
                t.HasKey(x => x.Id);
                t.Property(x => x.Id).ValueGeneratedOnAdd();

                t.ToTable("EmailTemplate");
            });

            modelBuilder.Entity<ClientPayment>(p =>
            {
                p.HasKey(x => x.Id);
                p.ToTable("ClientPayment");

                p.Property("PaymentType").HasColumnType("varchar(15)");
            });

            modelBuilder.Entity<ClientPaymentLog>(l =>
            {
                l.HasKey(x => x.Id);
                l.Property(x => x.Id).ValueGeneratedOnAdd();

                l.ToTable("ClientPaymentLog");
            });
        }
    }
}
