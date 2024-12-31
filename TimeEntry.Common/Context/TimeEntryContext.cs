using Microsoft.Extensions.Configuration;
using TimeEntry.Common.Models;

namespace TimeEntry.Common.Context
{
    public class TimeEntryContext : DbContext
    {
        readonly Type[] dbEntities = [ typeof(Department), typeof(DepartmentTeam),
            typeof(E_DonateLeave), typeof(E_Request), typeof(E_RequestExpenseDetail),
            typeof(E_TimeSheet), typeof(E_TimeSheetDetail), typeof(Employee), typeof(Holiday),
            typeof(Project), typeof(ProjectTask), typeof(Response), typeof(RestrictLeave),
            typeof(TimeEntryUser)
        ];

        public TimeEntryContext(DbContextOptions<TimeEntryContext> options) : base(options)
        {

        }

        public DbSet<Department> Department => Set<Department>();
        public DbSet<DepartmentTeam> DepartmentTeam => Set<DepartmentTeam>();
        public DbSet<E_DonateLeave> E_DonateLeave => Set<E_DonateLeave>();
        public DbSet<E_Request> E_Request => Set<E_Request>();
        public DbSet<E_RequestExpenseDetail> E_RequestExpenseDetail => Set<E_RequestExpenseDetail>();
        public DbSet<E_RequestExpenseSheet> E_RequestExpenseSheet => Set<E_RequestExpenseSheet>();
        public DbSet<E_TimeSheet> E_TimeSheet => Set<E_TimeSheet>();
        public DbSet<E_TimeSheetDetail> E_TimeSheetDetail => Set<E_TimeSheetDetail>();
        public DbSet<Employee> Employee => Set<Employee>();
        //public DbSet<ExpenseType> ExpenseTypes => Set<ExpenseType>();
        public DbSet<Holiday> Holiday => Set<Holiday>();
        public DbSet<Project> Project => Set<Project>();
        public DbSet<ProjectTask> ProjectTask => Set<ProjectTask>();
        public DbSet<Response> Response => Set<Response>();
        public DbSet<RestrictLeave> RestrictLeave => Set<RestrictLeave>();
        public DbSet<TimeEntryUser> TimeEntryUser => Set<TimeEntryUser>();


        /// <summary> Required to handle applying Configuration where missing </summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                IConfigurationRoot configuration = new ConfigurationBuilder()
                   .SetBasePath(Directory.GetCurrentDirectory())
                   .AddJsonFile("appsettings.json")
                   .Build();
                var connectionString = configuration.GetConnectionString("DbConnectionString");
                try
                {
                    optionsBuilder.UseSqlServer(connectionString);
                }
                catch (Exception ex) 
                {
                    throw new Exception("Unable to establish connection OR no attached database.  " + ex.Message); 
                }
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            foreach (var dbSetType in dbEntities)
            {
                modelBuilder.Entity(dbSetType);
            }

            // Configure the keyless entity
            modelBuilder.Entity<DeleteTableResult>().HasNoKey();
        }

        #region Stored Procedures
        /// <summary> Returns all the places that row is used in other tables. </summary>
        /// <param name="deleteFromTable">The table name</param>
        /// <param name="deleteId"> Id </param>
        /// <returns> List of rows in other tables that depend on this row. </returns>
        public async Task<List<DeleteTableResult>> SpCanDeleteAsync(string deleteFromTable, int deleteId)
        {
            var results = await Set<DeleteTableResult>()
                .FromSqlRaw("EXEC [dbo].[spCanDelete] @deleteFromTable = {0}, @deleteId = {1}", deleteFromTable, deleteId)
                .ToListAsync();

            return results;
        }
        #endregion
    }
}
