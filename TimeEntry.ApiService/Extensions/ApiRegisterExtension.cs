using TimeEntry.Common.Entities;

namespace TimeEntry.ApiService.Extensions
{
    public static class ApiRegisterExtension
    {
        public static WebApplication MapApis(this WebApplication app)
        {
            // Call Register on all classes that implement IApi
            new global::TimeEntry.ApiService.Apis.DepartmentApi<Department>().Register(app);
            new global::TimeEntry.ApiService.Apis.DepartmentTeamApi<DepartmentTeam>().Register(app);
            new global::TimeEntry.ApiService.Apis.E_DonateLeaveApi<E_DonateLeave>().Register(app);
            new global::TimeEntry.ApiService.Apis.E_RequestApi<E_Request>().Register(app);
            new global::TimeEntry.ApiService.Apis.E_RequestExpenseDetailApi<E_RequestExpenseDetail>().Register(app);
            new global::TimeEntry.ApiService.Apis.E_RequestExpenseSheetApi<E_RequestExpenseSheet>().Register(app);
            new global::TimeEntry.ApiService.Apis.E_TimeSheetApi<E_TimeSheet>().Register(app);
            new global::TimeEntry.ApiService.Apis.E_TimeSheetDetailApi<E_TimeSheetDetail>().Register(app);
            new global::TimeEntry.ApiService.Apis.EmployeeApi<Employee>().Register(app);
            new global::TimeEntry.ApiService.Apis.HolidayApi<Holiday>().Register(app);
            new global::TimeEntry.ApiService.Apis.ProjectsApi<Project>().Register(app);
            new global::TimeEntry.ApiService.Apis.ProjectTaskApi<ProjectTask>().Register(app);
            new global::TimeEntry.ApiService.Apis.ResponseApi<Response>().Register(app);
            new global::TimeEntry.ApiService.Apis.RestrictLeaveApi<RestrictLeave>().Register(app);
            new global::TimeEntry.ApiService.Apis.TimeEntryUserApi<TimeEntryUser>().Register(app);
            return app;
        }
    }
}
