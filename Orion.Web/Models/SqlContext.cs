using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Orion.Web.Models
{
    public class SqlContext: DbContext
    {
        public DbSet<TripDataModel> TripData { get; set; }
        public DbSet<TripRoutesModel> TripRoutes { get; set; }
        public DbSet<TrafficDataModel> TrafficData { get; set; }
        public SqlContext(DbContextOptions<SqlContext> options)
           : base(options)
        {

        }

        public SqlContext()
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                //#warning To protect potentially sensitive information in your connection string, you should move it out of source code. See http://go.microsoft.com/fwlink/?LinkId=723263 for guidance on storing connection strings.
                optionsBuilder.UseSqlServer(@"Server=(localdb)\MSSQLLocalDB; Initial Catalog = Orion; Integrated Security = True; TrustServerCertificate = True; ApplicationIntent = ReadWrite;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TripRoutesModel>()
                .HasOne(t => t.TripData)
                .WithMany(d => d.TripRoutes);
        }
    }
}
