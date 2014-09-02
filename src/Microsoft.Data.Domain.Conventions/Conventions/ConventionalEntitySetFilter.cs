﻿using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Data.Domain.Query;
using Microsoft.OData.Edm;

namespace Microsoft.Data.Domain.Conventions
{
    public class ConventionalEntitySetFilter : IQueryExpressionFilter
    {
        private Type _targetType;

        private ConventionalEntitySetFilter(Type targetType)
        {
            this._targetType = targetType;
        }

        public static void ApplyTo(
            DomainConfiguration configuration,
            Type targetType)
        {
            Ensure.NotNull(configuration, "configuration");
            Ensure.NotNull(targetType, "targetType");
            configuration.AddHookPoint(typeof(IQueryExpressionFilter),
                new ConventionalEntitySetFilter(targetType));
        }

        public Expression Filter(QueryExpressionContext context)
        {
            if (context.ModelReference == null)
            {
                return null;
            }
            var domainDataReference = context.ModelReference as DomainDataReference;
            if (domainDataReference == null)
            {
                return null;
            }
            var entitySet = domainDataReference.Element as IEdmEntitySet;
            if (entitySet == null)
            {
                return null;
            }
            var returnType = context.VisitedNode.Type
                .FindGenericType(typeof(IQueryable<>));
            var elementType = returnType.GetGenericArguments()[0];
            var method = this._targetType.GetMethod(
                "OnFilter" + entitySet.Name,
                BindingFlags.NonPublic |
                BindingFlags.Static |
                BindingFlags.Instance |
                BindingFlags.IgnoreCase |
                BindingFlags.DeclaredOnly);
            if (method != null && method.IsPrivate &&
                method.ReturnType == returnType)
            {
                object target = null;
                if (!method.IsStatic)
                {
                    target = context.QueryContext.DomainContext.GetProperty(
                        this._targetType.AssemblyQualifiedName);
                    if (target == null ||
                        !this._targetType.IsAssignableFrom(target.GetType()))
                    {
                        return null;
                    }
                }
                var parameters = method.GetParameters();
                if (parameters.Length == 1 &&
                    parameters[0].ParameterType == returnType)
                {
                    var queryType = typeof(EnumerableQuery<>)
                        .MakeGenericType(elementType);
                    var query = Activator.CreateInstance(
                        queryType, context.VisitedNode);
                    var result = method.Invoke(target,
                        new object[] { query }) as IQueryable;
                    if (result != null && result != query)
                    {
                        return result.Expression;
                    }
                }
            }
            return null;
        }
    }
}
