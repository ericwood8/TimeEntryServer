using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace TimeEntry.ApiService.Apis;

/// <summary>
/// Minimal APIs do not check DataAnnotations ([Range], [StringLength], [Required], ...) by themselves. This filter checks every
/// [FromBody] argument, including the items of any list inside it (a department's teams), and answers 400 with a
/// validation problem that names each field.
/// </summary>
public static class ValidateBodyFilter
{
    /// <summary> Adds the filter to an endpoint only when it has a [FromBody] parameter. </summary>
    public static EndpointFilterDelegate Factory(EndpointFilterFactoryContext factory, EndpointFilterDelegate next)
    {
        int[] bodyIndexes = factory.MethodInfo.GetParameters()
            .Select((p, i) => (p, i))
            .Where(x => x.p.GetCustomAttribute<FromBodyAttribute>() != null)
            .Select(x => x.i)
            .ToArray();
        if (bodyIndexes.Length == 0)
            return next;

        return async invocation =>
        {
            Dictionary<string, string[]> errors = new();
            foreach (int i in bodyIndexes)
                Validate(invocation.Arguments[i], "", errors);
            return errors.Count > 0 ? Results.ValidationProblem(errors) : await next(invocation);
        };
    }

    public static void Validate(object? value, string prefix, Dictionary<string, string[]> errors)
    {
        if (value == null)
            return;

        List<ValidationResult> results = [];
        Validator.TryValidateObject(value, new ValidationContext(value), results, validateAllProperties: true);
        foreach (var result in results)
        {
            string[] members = result.MemberNames.Any() ? result.MemberNames.ToArray() : [""];
            foreach (string member in members)
            {
                string key = prefix + member;
                errors[key] = [.. errors.GetValueOrDefault(key, []), result.ErrorMessage ?? "Invalid value."];
            }
        }

        // lists of rows inside the body (Department.Teams, Project.Tasks) are checked item by item
        foreach (var property in value.GetType().GetProperties())
        {
            if (property.GetIndexParameters().Length > 0 || property.PropertyType == typeof(string) || !typeof(IEnumerable).IsAssignableFrom(property.PropertyType))
                continue;
            if (property.GetValue(value) is not IEnumerable items)
                continue;

            int index = 0;
            foreach (var item in items)
            {
                if (item != null && item.GetType().Namespace?.StartsWith("TimeEntry") == true)
                    Validate(item, $"{prefix}{property.Name}[{index}].", errors);
                index++;
            }
        }
    }
}
