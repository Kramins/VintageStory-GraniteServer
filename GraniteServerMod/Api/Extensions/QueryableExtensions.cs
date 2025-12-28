using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GraniteServer.Api.Extensions;

public static class QueryableExtensions
{
    /// <summary>
    /// Applies dynamic sorting to an IQueryable based on a property name, with an optional leading '-' indicating descending.
    /// Falls back to the provided default property when the requested property is not found.
    /// </summary>
    public static IQueryable<T> ApplySort<T>(
        this IQueryable<T> source,
        string? sort,
        string defaultProperty = "Id"
    )
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        var descending = false;
        var sortName = string.IsNullOrWhiteSpace(sort) ? defaultProperty : sort.Trim();

        if (
            !string.IsNullOrWhiteSpace(sortName)
            && sortName.StartsWith("-", StringComparison.Ordinal)
        )
        {
            descending = true;
            sortName = sortName[1..];
        }

        var property = typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(p => p.Name.Equals(sortName, StringComparison.OrdinalIgnoreCase));

        if (property == null)
        {
            property = typeof(T).GetProperty(
                defaultProperty,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase
            );
        }

        if (property == null)
        {
            // No valid property found; return unmodified source
            return source;
        }

        var parameter = Expression.Parameter(typeof(T), "x");
        var propertyAccess = Expression.Property(parameter, property);
        var keySelector = Expression.Lambda(propertyAccess, parameter);

        var methodName = descending ? "OrderByDescending" : "OrderBy";
        var method = typeof(Queryable)
            .GetMethods()
            .First(m => m.Name == methodName && m.GetParameters().Length == 2);
        var genericMethod = method.MakeGenericMethod(typeof(T), property.PropertyType);

        return (IQueryable<T>)genericMethod.Invoke(null, new object[] { source, keySelector })!;
    }

    /// <summary>
    /// Applies paging to an IQueryable using zero-based page and pageSize.
    /// Clamps negative page to 0 and non-positive pageSize to a minimum of 1.
    /// </summary>
    public static IQueryable<T> ApplyPaging<T>(this IQueryable<T> source, int page, int pageSize)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        var safePage = page < 0 ? 0 : page;
        var safePageSize = pageSize <= 0 ? 1 : pageSize;

        return source.Skip(safePage * safePageSize).Take(safePageSize);
    }
}
