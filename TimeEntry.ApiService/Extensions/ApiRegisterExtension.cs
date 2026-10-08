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
            var reference = app.MapGroup("").AddEndpointFilter<ManageWritesFilter>();
            new DepartmentApi<Department>().Register(reference);
            new DepartmentTeamApi<DepartmentTeam>().Register(reference);
            new EmployeeApi<Employee>().Register(reference);
            new HolidayApi<Holiday>().Register(reference);
            new ProjectsApi<Project>().Register(reference);
            new ProjectTaskApi<ProjectTask>().Register(reference);
            new RestrictLeaveApi<RestrictLeave>().Register(reference);

            // Managers answer requests, so they may write responses as well.
            var responses = app.MapGroup("").AddEndpointFilter<ManagerWritesFilter>();
            new ResponseApi<Response>().Register(responses);

            // User maintenance: Admin only.
            var admin = app.MapGroup("").AddEndpointFilter<AdminOnlyFilter>();
            new TimeEntryUserApi<TimeEntryUser>().Register(admin);

            // An employee's own rows: each of these checks, row by row, that the caller may see or change that employee.
            new E_DonateLeaveApi<E_DonateLeave>().Register(app);
            new E_RequestApi<E_Request>().Register(app);
            new E_RequestExpenseDetailApi<E_RequestExpenseDetail>().Register(app);
            new E_RequestExpenseSheetApi<E_RequestExpenseSheet>().Register(app);
            new E_TimeSheetApi<E_TimeSheet>().Register(app);
            new E_TimeSheetDetailApi<E_TimeSheetDetail>().Register(app);
            return app;
        }
    }
}
