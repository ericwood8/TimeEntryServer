using TimeEntry.ApiService.Apis;
using TimeEntry.ApiService.Security;
using TimeEntry.Common.Entities;

namespace TimeEntry.ApiService.Extensions
{
    public static class ApiRegisterExtension
    {
        public static WebApplication MapApis(this WebApplication app)
        {
            // Every endpoint needs a signed-in user (the fallback policy); only /auth/login is open.
            new AuthApi().Register(app);

            // Reference data: everyone signed in reads it, only Admin and Human Resources change it.
            var reference = app.MapGroup("").AddEndpointFilter<ManageWritesFilter>().AddEndpointFilterFactory(ValidateBodyFilter.Factory);
            new DepartmentApi().Register(reference);
            new DepartmentTeamApi().Register(reference);
            new EmployeeApi().Register(reference);
            new HolidayApi().Register(reference);
            new ProjectsApi().Register(reference);
            new ProjectTaskApi().Register(reference);
            new RestrictLeaveApi().Register(reference);

            // Managers answer requests, so they may write responses as well.
            var responses = app.MapGroup("").AddEndpointFilter<ManagerWritesFilter>().AddEndpointFilterFactory(ValidateBodyFilter.Factory);
            new ResponseApi().Register(responses);

            // User maintenance: Admin only.
            var admin = app.MapGroup("").AddEndpointFilter<AdminOnlyFilter>().AddEndpointFilterFactory(ValidateBodyFilter.Factory);
            new TimeEntryUserApi().Register(admin);

            // An employee's own rows: each of these checks, row by row, that the caller may see or change that employee.
            var owned = app.MapGroup("").AddEndpointFilterFactory(ValidateBodyFilter.Factory);
            new E_DonateLeaveApi().Register(owned);
            new E_RequestApi().Register(owned);
            new E_RequestExpenseDetailApi().Register(owned);
            new E_RequestExpenseSheetApi().Register(owned);
            new E_TimeSheetApi().Register(owned);
            new E_TimeSheetDetailApi().Register(owned);
            return app;
        }
    }
}
