using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MemeIumServices.Models;
using Microsoft.EntityFrameworkCore;

namespace MemeIumServices.DatabaseContexts
{
    public class MemeOffContext : DbContext
    {
        public MemeOffContext(): base()
        {

        }

        public DbSet<Competition> Competitions { get; set; }
        public DbSet<Application> Applications { get; set; }
        public DbSet<Vote> Votes { get; set; }


        public MemeOffContext(DbContextOptions<MemeOffContext> options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Competition>().ToTable("Competitions");
            modelBuilder.Entity<Application>().ToTable("Applications");
            modelBuilder.Entity<Vote>().ToTable("Votes");

        }
    }
}
