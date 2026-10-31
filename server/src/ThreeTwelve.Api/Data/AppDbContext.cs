using Microsoft.EntityFrameworkCore;

namespace ThreeTwelve.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options);
