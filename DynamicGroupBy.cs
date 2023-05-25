using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

public static class DynamicGroupByExtension
{
    public static IQueryable<GroupResult<TSource>> GroupByDynamic<TSource>(
        this IQueryable<TSource> source,
        params string[] properties)
    {
        var parameter = Expression.Parameter(typeof(TSource), "x");

        var keySelectors = properties
            .Select(property =>
            {
                var propertyExpression = Expression.Property(parameter, property);
                return Expression.Lambda<Func<TSource, object>>(Expression.Convert(propertyExpression, typeof(object)), parameter);
            })
            .ToArray();

        var anonymousType = CreateAnonymousType<TSource>(properties);
        var groupType = typeof(IGrouping<,>).MakeGenericType(anonymousType, typeof(TSource));

        var groupByExpression = BuildGroupByExpression(source.Expression, keySelectors, groupType);

        var resultSelector = CreateGroupResultSelector<TSource>(groupType);

        var selectExpression = Expression.Call(
            typeof(Queryable),
            "Select",
            new[] { typeof(TSource), groupType },
            groupByExpression,
            resultSelector);

        return source.Provider.CreateQuery<GroupResult<TSource>>(selectExpression);
    }

    private static Type CreateAnonymousType<TSource>(string[] properties)
    {
        var propertiesInfo = properties
            .Select(property => typeof(TSource).GetProperty(property))
            .ToArray();

        var dynamicAnonymousTypeBuilder = new AnonymousTypeBuilder(propertiesInfo);

        return dynamicAnonymousTypeBuilder.CreateType();
    }

    private static Expression BuildGroupByExpression(Expression sourceExpression, LambdaExpression[] keySelectors, Type groupType)
    {
        var keyType = keySelectors.Length == 1 ? keySelectors[0].ReturnType : CreateAnonymousType<object>(keySelectors.Select(k => ((MemberExpression)k.Body).Member.Name).ToArray());

        var groupByMethodInfo = typeof(Queryable).GetMethods()
            .Where(m => m.Name == "GroupBy")
            .Select(m => new
            {
                Method = m,
                Parameters = m.GetParameters(),
                Arguments = m.GetGenericArguments()
            })
            .FirstOrDefault(m => m.Parameters.Length == 3 && m.Arguments.Length == 2 && m.Parameters[2].ParameterType.GetGenericTypeDefinition() == typeof(Expression<>));

        var groupByMethodInfoGeneric = groupByMethodInfo.Method.MakeGenericMethod(typeof(TSource), keyType, groupType);

        var groupByExpression = Expression.Call(
            null,
            groupByMethodInfoGeneric,
            sourceExpression,
            Expression.NewArrayInit(typeof(LambdaExpression), keySelectors));

        return groupByExpression;
    }

    private static LambdaExpression CreateGroupResultSelector<TSource>(Type groupType)
    {
        var parameter = Expression.Parameter(groupType, "g");

        var constructorInfo = typeof(GroupResult<TSource>).GetConstructors()[0];

        var propertiesInfo = typeof(GroupResult<TSource>).GetProperties();

        var propertyBindings = propertiesInfo
            .Select(p =>
            {
                var propertyExpression = Expression.Property(parameter, p.Name);
                return Expression.Bind(p, propertyExpression);
            });

        var anonymousType = CreateAnonymousType<GroupResult<TSource>>(propertiesInfo.Select(p => p.Name).ToArray());

        var memberInitExpression = Expression.MemberInit(Expression.New(anonymousType), propertyBindings);

        return Expression.Lambda(memberInitExpression, parameter);
    }
}

public class GroupResult<TSource>
{
    public object Key { get; set; }
    public IEnumerable<TSource> Values { get; set; }
}
