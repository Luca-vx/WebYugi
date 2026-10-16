using Microsoft.EntityFrameworkCore;
using Npgsql;
using WebYugi.Model;

namespace WebYugi.Infrastructure
{
    public class AppDbContext : DbContext
    {

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options){
        }

        public DbSet<Users> Users { get; set; }        
    }
}
