using MyOrganIzer.Entites;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace MyOrganIzer.MyDbContext
{
    public class AppDbContext : DbContext
    {
        public AppDbContext() : base("name=AppDbContext") { }
        public DbSet<Client>  Clients { get; set; }
        public DbSet<Product>   Products { get; set; }
        public DbSet<Tooth>   Teeths { get; set; }
    }

    
}
