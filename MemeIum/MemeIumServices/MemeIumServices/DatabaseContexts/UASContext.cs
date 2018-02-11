using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MemeIumServices.Models;
using Microsoft.EntityFrameworkCore;

namespace MemeIumServices.DatabaseContexts
{
    public class UASContext : DbContext
    {
        public UASContext (): base()
        {

        }

        public DbSet<User> Users { get; set; }
        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<HistoricalTransaction> HistoricalTransactions { get; set; }
        public DbSet<UserToken> UserTokens { get; set; }

        public UASContext(DbContextOptions<UASContext> options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<Wallet>().ToTable("Wallets");
            modelBuilder.Entity<HistoricalTransaction>().ToTable("HistoricalTransactions");
            modelBuilder.Entity<UserToken>().ToTable("UserTokens");
        }
    }
}
