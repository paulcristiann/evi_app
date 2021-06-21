using evi_app.Models;
using EVI_App.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace evi_app.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Message> MessagesItems { get; set; }
        public DbSet<UserIdCorespondence> UserCorespondence { get; set; }
        public DbSet<UploadedCertificate> UploadedCertificates { get; set; }
    }
}
