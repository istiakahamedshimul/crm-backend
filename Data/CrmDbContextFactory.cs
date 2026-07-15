using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace backend.Data;

public class CrmDbContextFactory : IDesignTimeDbContextFactory<CrmDbContext>
{
    public CrmDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CrmDbContext>()
            .UseMySql("Server=localhost;Database=crm;User=root;Password=design-time", new MySqlServerVersion(new Version(8, 0, 0)))
            .Options;
        return new CrmDbContext(options);
    }
}
