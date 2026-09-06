using Microsoft.EntityFrameworkCore;
using Npgsql;
using WebYugi.Model;

namespace WebYugi.Infrastructure
{
    public class DbConnection : DbContext
    {

        public DbConnection(DbContextOptions<DbConnection> options) : base(options){
        }

        public DbSet<Users> Users { get; set; }        
    }
}
