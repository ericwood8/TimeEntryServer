namespace TimeEntry.ApiService.Apis;

/// <summary> The error answers the APIs share, each an RFC 9457 problem with a message the UI can show. </summary>
public static class ApiProblems
{
    public static IResult BadName() =>
        Results.Problem(statusCode: 400, title: "Invalid name",
            detail: "The name must not be empty and may have only letters, digits, spaces, apostrophes and hyphens.");

    public static IResult DuplicateName() =>
        Results.Problem(statusCode: 422, title: "Duplicate name", detail: "An active row with that name already exists.");

    public static IResult IdMismatch() =>
        Results.Problem(statusCode: 400, title: "Id mismatch", detail: "The id in the URL and the id in the body must be the same.");

    public static IResult NotFound(string what = "row") =>
        Results.Problem(statusCode: 404, title: "Not found", detail: $"There is no {what} with that id.");

    public static IResult InUse() =>
        Results.Problem(statusCode: 400, title: "In use", detail: "It cannot be deleted because it is in use.");

    public static IResult Invalid(string detail) =>
        Results.Problem(statusCode: 400, title: "Invalid request", detail: detail);

    public static IResult Unprocessable(string detail) =>
        Results.Problem(statusCode: 422, title: "Cannot be saved", detail: detail);

    public static IResult Forbidden(string detail) =>
        Results.Problem(statusCode: 403, title: "Not allowed", detail: detail);
}
