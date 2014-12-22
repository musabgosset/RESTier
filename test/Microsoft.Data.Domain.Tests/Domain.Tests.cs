﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Library;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Core.Tests
{
    [TestClass]
    public class DomainTests
    {
        private class TestModelHandler : IModelHandler
        {
            public DomainContext DomainContext { get; set; }

            public IEdmModel Model { get; set; }

            public Task<IEdmModel> GetModelAsync(
                ModelContext context,
                CancellationToken cancellationToken)
            {
                Assert.AreSame(DomainContext, context.DomainContext);
                return Task.FromResult(Model);
            }
        }

        private class TestModelMapper : IModelMapper
        {
            public bool TryGetRelevantType(
                DomainContext context,
                string name, out Type relevantType)
            {
                relevantType = typeof(string);
                return true;
            }

            public bool TryGetRelevantType(
                DomainContext context,
                string namespaceName, string name,
                out Type relevantType)
            {
                relevantType = typeof(DateTime);
                return true;
            }
        }

        private class TestQueryHandler : IQueryHandler
        {
            public DomainContext DomainContext { get; set; }

            public IEnumerable Results { get; set; }

            public Task<QueryResult> QueryAsync(
                QueryContext context,
                CancellationToken cancellationToken)
            {
                Assert.AreSame(DomainContext, context.DomainContext);
                return Task.FromResult(new QueryResult(Results));
            }
        }

        private class TestSubmitHandler : ISubmitHandler
        {
            public DomainContext DomainContext { get; set; }

            public ChangeSet ChangeSet { get; set; }

            public Task<SubmitResult> SubmitAsync(
                SubmitContext context,
                CancellationToken cancellationToken)
            {
                Assert.AreSame(DomainContext, context.DomainContext);
                return Task.FromResult(new SubmitResult(ChangeSet));
            }
        }

        private class TestDomain : IDomain
        {
            private DomainContext _context;

            public IEdmModel Model { get; private set; }

            public IEnumerable Results { get; private set; }

            public ChangeSet ChangeSet { get; private set; }

            public DomainContext Context
            {
                get
                {
                    if (_context == null)
                    {
                        var configuration = new DomainConfiguration();
                        var modelHandler = new TestModelHandler();
                        var modelMapper = new TestModelMapper();
                        var queryHandler = new TestQueryHandler();
                        var submitHandler = new TestSubmitHandler();
                        configuration.SetHookPoint(
                            typeof(IModelHandler), modelHandler);
                        configuration.SetHookPoint(
                            typeof(IModelMapper), modelMapper);
                        configuration.SetHookPoint(
                            typeof(IQueryHandler), queryHandler);
                        configuration.SetHookPoint(
                            typeof(ISubmitHandler), submitHandler);
                        configuration.EnsureCommitted();
                        _context = new DomainContext(configuration);
                        modelHandler.DomainContext = _context;
                        Model = modelHandler.Model = new EdmModel();
                        queryHandler.DomainContext = _context;
                        Results = queryHandler.Results = new string[] { "Test" };
                        submitHandler.DomainContext = _context;
                        ChangeSet = submitHandler.ChangeSet = new ChangeSet();
                    }
                    return _context;
                }
            }
        }

        [TestMethod]
        public async Task DomainGetModelAsyncForwardsCorrectly()
        {
            var domain = new TestDomain();
            var domainModel = await domain.GetModelAsync();
            var domainModelType = typeof(Domain).Assembly.GetType(
                "Microsoft.Restier.Core.Model.DomainModel");
            Assert.IsTrue(domainModelType.IsAssignableFrom(domainModel.GetType()));
            Assert.AreSame(domain.Model, domainModelType
                .GetProperty("InnerModel").GetValue(domainModel));
        }

        [TestMethod]
        public async Task GetModelAsyncCorrectlyUsesModelHandler()
        {
            var configuration = new DomainConfiguration();
            var modelHandler = new TestModelHandler();
            configuration.SetHookPoint(typeof(IModelHandler), modelHandler);
            configuration.EnsureCommitted();
            var context = new DomainContext(configuration);
            modelHandler.DomainContext = context;
            modelHandler.Model = new EdmModel();

            var domainModel = await Domain.GetModelAsync(context);
            var domainModelType = typeof(Domain).Assembly.GetType(
                "Microsoft.Restier.Core.Model.DomainModel");
            Assert.IsTrue(domainModelType.IsAssignableFrom(domainModel.GetType()));
            Assert.AreSame(modelHandler.Model, domainModelType
                .GetProperty("InnerModel").GetValue(domainModel));
        }

        [TestMethod]
        public void DomainSourceOfEntityContainerElementIsCorrect()
        {
            var domain = new TestDomain();
            var arguments = new object[0];

            var source = domain.Source("Test", arguments);
            Assert.AreEqual(typeof(string), source.ElementType);
            Assert.IsTrue(source.Expression is MethodCallExpression);
            var methodCall = source.Expression as MethodCallExpression;
            Assert.IsNull(methodCall.Object);
            Assert.AreEqual(typeof(DomainData), methodCall.Method.DeclaringType);
            Assert.AreEqual("Source", methodCall.Method.Name);
            Assert.AreEqual(typeof(string), methodCall.Method.GetGenericArguments()[0]);
            Assert.AreEqual(2, methodCall.Arguments.Count);
            Assert.IsTrue(methodCall.Arguments[0] is ConstantExpression);
            Assert.AreEqual("Test", (methodCall.Arguments[0] as ConstantExpression).Value);
            Assert.IsTrue(methodCall.Arguments[1] is ConstantExpression);
            Assert.AreEqual(arguments, (methodCall.Arguments[1] as ConstantExpression).Value);
            Assert.AreEqual(source.Expression.ToString(), source.ToString());
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void SourceOfEntityContainerElementThrowsIfNotMapped()
        {
            var configuration = new DomainConfiguration();
            configuration.EnsureCommitted();
            var context = new DomainContext(configuration);
            var arguments = new object[0];

            Domain.Source(context, "Test", arguments);
        }

        [TestMethod]
        public void SourceOfEntityContainerElementIsCorrect()
        {
            var configuration = new DomainConfiguration();
            var modelMapper = new TestModelMapper();
            configuration.AddHookPoint(typeof(IModelMapper), modelMapper);
            configuration.EnsureCommitted();
            var context = new DomainContext(configuration);
            var arguments = new object[0];

            var source = Domain.Source(context, "Test", arguments);
            Assert.AreEqual(typeof(string), source.ElementType);
            Assert.IsTrue(source.Expression is MethodCallExpression);
            var methodCall = source.Expression as MethodCallExpression;
            Assert.IsNull(methodCall.Object);
            Assert.AreEqual(typeof(DomainData), methodCall.Method.DeclaringType);
            Assert.AreEqual("Source", methodCall.Method.Name);
            Assert.AreEqual(typeof(string), methodCall.Method.GetGenericArguments()[0]);
            Assert.AreEqual(2, methodCall.Arguments.Count);
            Assert.IsTrue(methodCall.Arguments[0] is ConstantExpression);
            Assert.AreEqual("Test", (methodCall.Arguments[0] as ConstantExpression).Value);
            Assert.IsTrue(methodCall.Arguments[1] is ConstantExpression);
            Assert.AreEqual(arguments, (methodCall.Arguments[1] as ConstantExpression).Value);
            Assert.AreEqual(source.Expression.ToString(), source.ToString());
        }

        [TestMethod]
        public void DomainSourceOfComposableFunctionIsCorrect()
        {
            var domain = new TestDomain();
            var arguments = new object[0];

            var source = domain.Source("Namespace", "Function", arguments);
            Assert.AreEqual(typeof(DateTime), source.ElementType);
            Assert.IsTrue(source.Expression is MethodCallExpression);
            var methodCall = source.Expression as MethodCallExpression;
            Assert.IsNull(methodCall.Object);
            Assert.AreEqual(typeof(DomainData), methodCall.Method.DeclaringType);
            Assert.AreEqual("Source", methodCall.Method.Name);
            Assert.AreEqual(typeof(DateTime), methodCall.Method.GetGenericArguments()[0]);
            Assert.AreEqual(3, methodCall.Arguments.Count);
            Assert.IsTrue(methodCall.Arguments[0] is ConstantExpression);
            Assert.AreEqual("Namespace", (methodCall.Arguments[0] as ConstantExpression).Value);
            Assert.IsTrue(methodCall.Arguments[1] is ConstantExpression);
            Assert.AreEqual("Function", (methodCall.Arguments[1] as ConstantExpression).Value);
            Assert.IsTrue(methodCall.Arguments[2] is ConstantExpression);
            Assert.AreEqual(arguments, (methodCall.Arguments[2] as ConstantExpression).Value);
            Assert.AreEqual(source.Expression.ToString(), source.ToString());
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void SourceOfComposableFunctionThrowsIfNotMapped()
        {
            var configuration = new DomainConfiguration();
            configuration.EnsureCommitted();
            var context = new DomainContext(configuration);
            var arguments = new object[0];

            Domain.Source(context, "Namespace", "Function", arguments);
        }

        [TestMethod]
        public void SourceOfComposableFunctionIsCorrect()
        {
            var configuration = new DomainConfiguration();
            var modelMapper = new TestModelMapper();
            configuration.AddHookPoint(typeof(IModelMapper), modelMapper);
            configuration.EnsureCommitted();
            var context = new DomainContext(configuration);
            var arguments = new object[0];

            var source = Domain.Source(context,
                "Namespace", "Function", arguments);
            Assert.AreEqual(typeof(DateTime), source.ElementType);
            Assert.IsTrue(source.Expression is MethodCallExpression);
            var methodCall = source.Expression as MethodCallExpression;
            Assert.IsNull(methodCall.Object);
            Assert.AreEqual(typeof(DomainData), methodCall.Method.DeclaringType);
            Assert.AreEqual("Source", methodCall.Method.Name);
            Assert.AreEqual(typeof(DateTime), methodCall.Method.GetGenericArguments()[0]);
            Assert.AreEqual(3, methodCall.Arguments.Count);
            Assert.IsTrue(methodCall.Arguments[0] is ConstantExpression);
            Assert.AreEqual("Namespace", (methodCall.Arguments[0] as ConstantExpression).Value);
            Assert.IsTrue(methodCall.Arguments[1] is ConstantExpression);
            Assert.AreEqual("Function", (methodCall.Arguments[1] as ConstantExpression).Value);
            Assert.IsTrue(methodCall.Arguments[2] is ConstantExpression);
            Assert.AreEqual(arguments, (methodCall.Arguments[2] as ConstantExpression).Value);
            Assert.AreEqual(source.Expression.ToString(), source.ToString());
        }

        [TestMethod]
        public void GenericDomainSourceOfEntityContainerElementIsCorrect()
        {
            var domain = new TestDomain();
            var arguments = new object[0];

            var source = domain.Source<string>("Test", arguments);
            Assert.AreEqual(typeof(string), source.ElementType);
            Assert.IsTrue(source.Expression is MethodCallExpression);
            var methodCall = source.Expression as MethodCallExpression;
            Assert.IsNull(methodCall.Object);
            Assert.AreEqual(typeof(DomainData), methodCall.Method.DeclaringType);
            Assert.AreEqual("Source", methodCall.Method.Name);
            Assert.AreEqual(typeof(string), methodCall.Method.GetGenericArguments()[0]);
            Assert.AreEqual(2, methodCall.Arguments.Count);
            Assert.IsTrue(methodCall.Arguments[0] is ConstantExpression);
            Assert.AreEqual("Test", (methodCall.Arguments[0] as ConstantExpression).Value);
            Assert.IsTrue(methodCall.Arguments[1] is ConstantExpression);
            Assert.AreEqual(arguments, (methodCall.Arguments[1] as ConstantExpression).Value);
            Assert.AreEqual(source.Expression.ToString(), source.ToString());
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void GenericSourceOfEntityContainerElementThrowsIfWrongType()
        {
            var configuration = new DomainConfiguration();
            var modelMapper = new TestModelMapper();
            configuration.SetHookPoint(typeof(IModelMapper), modelMapper);
            configuration.EnsureCommitted();
            var context = new DomainContext(configuration);
            var arguments = new object[0];

            Domain.Source<object>(context, "Test", arguments);
        }

        [TestMethod]
        public void GenericSourceOfEntityContainerElementIsCorrect()
        {
            var configuration = new DomainConfiguration();
            var modelMapper = new TestModelMapper();
            configuration.SetHookPoint(typeof(IModelMapper), modelMapper);
            configuration.EnsureCommitted();
            var context = new DomainContext(configuration);
            var arguments = new object[0];

            var source = Domain.Source<string>(context, "Test", arguments);
            Assert.AreEqual(typeof(string), source.ElementType);
            Assert.IsTrue(source.Expression is MethodCallExpression);
            var methodCall = source.Expression as MethodCallExpression;
            Assert.IsNull(methodCall.Object);
            Assert.AreEqual(typeof(DomainData), methodCall.Method.DeclaringType);
            Assert.AreEqual("Source", methodCall.Method.Name);
            Assert.AreEqual(typeof(string), methodCall.Method.GetGenericArguments()[0]);
            Assert.AreEqual(2, methodCall.Arguments.Count);
            Assert.IsTrue(methodCall.Arguments[0] is ConstantExpression);
            Assert.AreEqual("Test", (methodCall.Arguments[0] as ConstantExpression).Value);
            Assert.IsTrue(methodCall.Arguments[1] is ConstantExpression);
            Assert.AreEqual(arguments, (methodCall.Arguments[1] as ConstantExpression).Value);
            Assert.AreEqual(source.Expression.ToString(), source.ToString());
        }

        [TestMethod]
        public void GenericDomainSourceOfComposableFunctionIsCorrect()
        {
            var domain = new TestDomain();
            var arguments = new object[0];

            var source = domain.Source<DateTime>(
                "Namespace", "Function", arguments);
            Assert.AreEqual(typeof(DateTime), source.ElementType);
            Assert.IsTrue(source.Expression is MethodCallExpression);
            var methodCall = source.Expression as MethodCallExpression;
            Assert.IsNull(methodCall.Object);
            Assert.AreEqual(typeof(DomainData), methodCall.Method.DeclaringType);
            Assert.AreEqual("Source", methodCall.Method.Name);
            Assert.AreEqual(typeof(DateTime), methodCall.Method.GetGenericArguments()[0]);
            Assert.AreEqual(3, methodCall.Arguments.Count);
            Assert.IsTrue(methodCall.Arguments[0] is ConstantExpression);
            Assert.AreEqual("Namespace", (methodCall.Arguments[0] as ConstantExpression).Value);
            Assert.IsTrue(methodCall.Arguments[1] is ConstantExpression);
            Assert.AreEqual("Function", (methodCall.Arguments[1] as ConstantExpression).Value);
            Assert.IsTrue(methodCall.Arguments[2] is ConstantExpression);
            Assert.AreEqual(arguments, (methodCall.Arguments[2] as ConstantExpression).Value);
            Assert.AreEqual(source.Expression.ToString(), source.ToString());
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void GenericSourceOfComposableFunctionThrowsIfWrongType()
        {
            var configuration = new DomainConfiguration();
            var modelMapper = new TestModelMapper();
            configuration.SetHookPoint(typeof(IModelMapper), modelMapper);
            configuration.EnsureCommitted();
            var context = new DomainContext(configuration);
            var arguments = new object[0];

            Domain.Source<object>(context, "Namespace", "Function", arguments);
        }

        [TestMethod]
        public void GenericSourceOfComposableFunctionIsCorrect()
        {
            var configuration = new DomainConfiguration();
            var modelMapper = new TestModelMapper();
            configuration.SetHookPoint(typeof(IModelMapper), modelMapper);
            configuration.EnsureCommitted();
            var context = new DomainContext(configuration);
            var arguments = new object[0];

            var source = Domain.Source<DateTime>(context,
                "Namespace", "Function", arguments);
            Assert.AreEqual(typeof(DateTime), source.ElementType);
            Assert.IsTrue(source.Expression is MethodCallExpression);
            var methodCall = source.Expression as MethodCallExpression;
            Assert.IsNull(methodCall.Object);
            Assert.AreEqual(typeof(DomainData), methodCall.Method.DeclaringType);
            Assert.AreEqual("Source", methodCall.Method.Name);
            Assert.AreEqual(typeof(DateTime), methodCall.Method.GetGenericArguments()[0]);
            Assert.AreEqual(3, methodCall.Arguments.Count);
            Assert.IsTrue(methodCall.Arguments[0] is ConstantExpression);
            Assert.AreEqual("Namespace", (methodCall.Arguments[0] as ConstantExpression).Value);
            Assert.IsTrue(methodCall.Arguments[1] is ConstantExpression);
            Assert.AreEqual("Function", (methodCall.Arguments[1] as ConstantExpression).Value);
            Assert.IsTrue(methodCall.Arguments[2] is ConstantExpression);
            Assert.AreEqual(arguments, (methodCall.Arguments[2] as ConstantExpression).Value);
            Assert.AreEqual(source.Expression.ToString(), source.ToString());
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void SourceQueryableCannotGenericEnumerate()
        {
            var configuration = new DomainConfiguration();
            var modelMapper = new TestModelMapper();
            configuration.SetHookPoint(typeof(IModelMapper), modelMapper);
            configuration.EnsureCommitted();
            var context = new DomainContext(configuration);

            var source = Domain.Source<string>(context, "Test");
            source.GetEnumerator();
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void SourceQueryableCannotEnumerate()
        {
            var configuration = new DomainConfiguration();
            var modelMapper = new TestModelMapper();
            configuration.SetHookPoint(typeof(IModelMapper), modelMapper);
            configuration.EnsureCommitted();
            var context = new DomainContext(configuration);

            var source = Domain.Source<string>(context, "Test");
            (source as IEnumerable).GetEnumerator();
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void SourceQueryProviderCannotGenericExecute()
        {
            var configuration = new DomainConfiguration();
            var modelMapper = new TestModelMapper();
            configuration.SetHookPoint(typeof(IModelMapper), modelMapper);
            configuration.EnsureCommitted();
            var context = new DomainContext(configuration);

            var source = Domain.Source<string>(context, "Test");
            source.Provider.Execute<string>(null);
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void SourceQueryProviderCannotExecute()
        {
            var configuration = new DomainConfiguration();
            var modelMapper = new TestModelMapper();
            configuration.SetHookPoint(typeof(IModelMapper), modelMapper);
            configuration.EnsureCommitted();
            var context = new DomainContext(configuration);

            var source = Domain.Source<string>(context, "Test");
            source.Provider.Execute(null);
        }

        [TestMethod]
        public async Task DomainQueryAsyncWithQueryReturnsResults()
        {
            var domain = new TestDomain();

            var results = await domain.QueryAsync(
                domain.Source<string>("Test"));
            Assert.IsTrue(results.SequenceEqual(new string[] { "Test" }));
        }

        [TestMethod]
        public async Task DomainQueryAsyncWithSingletonQueryReturnsResult()
        {
            var domain = new TestDomain();

            var result = await domain.QueryAsync(
                domain.Source<string>("Test"), q => q.Single());
            Assert.AreEqual("Test", result);
        }

        [TestMethod]
        public async Task DomainQueryAsyncCorrectlyForwardsCall()
        {
            var domain = new TestDomain();

            var queryRequest = new QueryRequest(
                domain.Source<string>("Test"), true);
            var queryResult = await domain.QueryAsync(queryRequest);
            Assert.IsTrue(queryResult.Results.Cast<string>()
                .SequenceEqual(new string[] { "Test" }));
        }

        [TestMethod]
        public async Task QueryAsyncCorrectlyUsesQueryHandler()
        {
            var configuration = new DomainConfiguration();
            var modelHandler = new TestModelHandler();
            var modelMapper = new TestModelMapper();
            var queryHandler = new TestQueryHandler();
            configuration.SetHookPoint(typeof(IModelHandler), modelHandler);
            configuration.SetHookPoint(typeof(IModelMapper), modelMapper);
            configuration.SetHookPoint(typeof(IQueryHandler), queryHandler);
            configuration.EnsureCommitted();
            var context = new DomainContext(configuration);
            modelHandler.DomainContext = context;
            modelHandler.Model = new EdmModel();
            queryHandler.DomainContext = context;
            queryHandler.Results = new string[] { "Test" };

            var queryRequest = new QueryRequest(
                Domain.Source<string>(context, "Test"), true);
            var queryResult = await Domain.QueryAsync(context, queryRequest);
            Assert.IsTrue(queryResult.Results.Cast<string>()
                .SequenceEqual(new string[] { "Test" }));
        }

        [TestMethod]
        public async Task DomainSubmitAsyncCorrectlyForwardsCall()
        {
            var domain = new TestDomain();

            var submitResult = await domain.SubmitAsync();
            Assert.AreSame(domain.ChangeSet, submitResult.CompletedChangeSet);
        }

        [TestMethod]
        public async Task SubmitAsyncCorrectlyUsesSubmitHandler()
        {
            var configuration = new DomainConfiguration();
            var modelHandler = new TestModelHandler();
            var modelMapper = new TestModelMapper();
            var submitHandler = new TestSubmitHandler();
            configuration.SetHookPoint(typeof(IModelHandler), modelHandler);
            configuration.SetHookPoint(typeof(IModelMapper), modelMapper);
            configuration.SetHookPoint(typeof(ISubmitHandler), submitHandler);
            configuration.EnsureCommitted();
            var context = new DomainContext(configuration);
            modelHandler.DomainContext = context;
            modelHandler.Model = new EdmModel();
            submitHandler.DomainContext = context;
            submitHandler.ChangeSet= new ChangeSet();

            var submitResult = await Domain.SubmitAsync(context);
            Assert.AreSame(submitHandler.ChangeSet,
                submitResult.CompletedChangeSet);
        }
    }
}
