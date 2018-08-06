using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Amendment.Model.Infrastructure;

namespace Amendment.Repository.Infrastructure
{
    internal static class QueryableHelper
    {
        public static IQueryable<T> WhereMany<T>(this IQueryable<T> q, params Expression<Func<T, bool>>[] where) where T : class, ITableBase
        {
            if (@where == null) return q;

            var output = q;
            foreach (var expression in where)
            {
                if (expression == null) continue;
                output = output.Where(expression);
            }
            return output;
        }

        public static IQueryable<TSource> OrderByJsonApi<TSource>(
            this IEnumerable<TSource> query, string properties)
        {
            IQueryable<TSource> output = (IQueryable<TSource>)query;
            if (properties == null) return query.AsQueryable();

            var fields = properties.Split(',');
            foreach (var field in fields)
            {
                if (field.StartsWith("-"))
                    output = output.OrderByDescending(FirstLetterToUpper(field.Substring(1)));
                else
                    output = output.OrderBy(FirstLetterToUpper(field));
            }

            return output;
        }

        private static string FirstLetterToUpper(string str)
        {
            if (str == null)
                return null;

            if (str.Length > 1)
                return char.ToUpper(str[0]) + str.Substring(1);

            return str.ToUpper();
        }

        public static IOrderedQueryable<TSource> OrderBy<TSource>(
       this IEnumerable<TSource> query, string propertyName)
        {
            return OrderByPrivate(query, propertyName, "OrderBy");
        }

        public static IOrderedQueryable<TSource> OrderByDescending<TSource>(
       this IEnumerable<TSource> query, string propertyName)
        {
            return OrderByPrivate(query, propertyName, "OrderByDescending");
        }

        //http://stackoverflow.com/questions/31955025/generate-ef-orderby-expression-by-string
        private static IOrderedQueryable<TSource> OrderByPrivate<TSource>(
       IEnumerable<TSource> query, string propertyName, string direction)
        {
            var entityType = typeof(TSource);

            //Create x=>x.PropName
            var propertyInfo = entityType.GetTypeInfo().GetProperty(propertyName);
            if (propertyInfo == null) throw new Exception($"Property '{propertyName}' not found. (sort)");

            ParameterExpression arg = Expression.Parameter(entityType, "x");
            MemberExpression property = Expression.Property(arg, propertyName);
            var selector = Expression.Lambda(property, new ParameterExpression[] { arg });

            //Get System.Linq.Queryable.OrderBy() method.
            var enumarableType = typeof(System.Linq.Queryable);
            var method = enumarableType.GetTypeInfo().GetMethods()
                 .Where(m => m.Name == direction && m.IsGenericMethodDefinition)
                 .Where(m =>
                 {
                     var parameters = m.GetParameters().ToList();
                     //Put more restriction here to ensure selecting the right overload                
                     return parameters.Count == 2;//overload that has 2 parameters
                 }).Single();
            //The linq's OrderBy<TSource, TKey> has two generic types, which provided here
            MethodInfo genericMethod = method
                 .MakeGenericMethod(entityType, propertyInfo.PropertyType);

            /*Call query.OrderBy(selector), with query and selector: x=> x.PropName
              Note that we pass the selector as Expression to the method and we don't compile it.
              By doing so EF can extract "order by" columns and generate SQL for it.*/
            var newQuery = (IOrderedQueryable<TSource>)genericMethod
                 .Invoke(genericMethod, new object[] { query, selector });
            return newQuery;
        }
    }
}
