using Microsoft.EntityFrameworkCore;
using Orion.DB.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Orion.DB
{
    public class SqlContext: DbContext
    {
        public DbSet<TripDataModel> TripData { get; set; }
        //public DbSet<TripRouteModel> TripRoute { get; set; }
        public SqlContext(DbContextOptions<SqlContext> options)
           : base(options)
        {
            this.Database.SetCommandTimeout(6000);
        }

        public SqlContext()
        {
            this.Database.SetCommandTimeout(6000);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {

            //#warning To protect potentially sensitive information in your connection string, you should move it out of source code. See http://go.microsoft.com/fwlink/?LinkId=723263 for guidance on storing connection strings.
            optionsBuilder.UseSqlServer(@"Server=DESKTOP-IM96RCK; Initial Catalog = Orion; Integrated Security = True; TrustServerCertificate = True; ApplicationIntent = ReadWrite; MultipleActiveResultSets=true;");
            
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<TripRouteModel>()
            //    .HasOne(t => t.TripData)
            //    .WithMany(d => d.TripRoutes);
            //modelBuilder.Entity<TripDataModel>()
            //    .HasIndex(o => o.Trip_Date);
        }
    }
}
