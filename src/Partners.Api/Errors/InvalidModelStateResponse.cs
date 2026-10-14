using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Partners.Api.Errors;

/// <summary>
/// Turns model-binding failures (bad JSON, wrong JSON types, missing PUT properties, bad query/route values)
/// into the same 400 shape and field names used by domain validation, without leaking .NET type names.
/// </summary>
internal static partial class InvalidModelStateResponse
{
    private const string BodyKey = "Body";

    public static IActionResult Create(ActionContext context)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

        foreach (var (key, entry) in context.ModelState)
        {
            if (entry.Errors.Count == 0)
            {
                continue;
            }

            var message = entry.Errors[0].ErrorMessage;

            if (key.StartsWith('$'))
            {
                var missing = MissingRequiredProperties().Match(message);
                if (missing.Success)
                {
                    var names = QuotedName().Matches(missing.Groups["names"].Value).Select(m => Pascal(m.Groups[1].Value));
                    errors[BodyKey] = [$"Missing required properties: {string.Join(", ", names)}."];
                }
                else if (message.Contains("could not be converted", StringComparison.Ordinal) && key.Length > 2)
                {
                    var field = Pascal(key[2..]);
                    errors[field] = [$"The value of {field} is not valid or has the wrong JSON type."];
                }
                else
                {
                    errors[BodyKey] = ["The request body is not valid JSON."];
                }
            }
            else if (key.Length > 0)
            {
                errors[Pascal(key)] = entry.Errors.Select(e => e.ErrorMessage).ToArray();
            }
            else
            {
                errors[BodyKey] = ["A JSON request body is required."];
            }
        }

        // "The request field is required." only restates that the body could not be bound.
        if (errors.Count > 1 || errors.ContainsKey(BodyKey))
        {
            errors.Remove("Request");
        }

        if (errors.Remove("Request"))
        {
            errors[BodyKey] = ["A JSON request body is required."];
        }

        var factory = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
        var problem = factory.CreateProblemDetails(context.HttpContext, StatusCodes.Status400BadRequest,
            title: "One or more validation errors occurred.", detail: "See the errors property for details.");

        var validationProblem = new ValidationProblemDetails(errors)
        {
            Type = problem.Type,
            Title = problem.Title,
            Status = problem.Status,
            Detail = problem.Detail
        };
        foreach (var (name, value) in problem.Extensions)
        {
            validationProblem.Extensions[name] = value;
        }

        return new BadRequestObjectResult(validationProblem) { ContentTypes = { "application/problem+json" } };
    }

    private static string Pascal(string name) => name.Length == 0 ? name : char.ToUpperInvariant(name[0]) + name[1..];

    [GeneratedRegex("missing required properties including: (?<names>.*)$")]
    private static partial Regex MissingRequiredProperties();

    [GeneratedRegex("'([^']+)'")]
    private static partial Regex QuotedName();
}
