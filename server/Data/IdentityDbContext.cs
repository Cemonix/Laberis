using System;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using server.Models;

namespace server.Data;

public class IdentityDbContext : IdentityDbContext<ApplicationUser>
{   
    public const string IdentitySchema = "identity";

    public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
           : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasDefaultSchema(IdentitySchema);
    }
}
