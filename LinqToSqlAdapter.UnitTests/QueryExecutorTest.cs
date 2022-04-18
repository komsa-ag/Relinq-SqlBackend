// This file is part of the re-linq project (relinq.codeplex.com)
// Copyright (c) rubicon IT GmbH, www.rubicon.eu
// 
// re-linq is free software; you can redistribute it and/or modify it under 
// the terms of the GNU Lesser General Public License as published by the 
// Free Software Foundation; either version 2.1 of the License, 
// or (at your option) any later version.
// 
// re-linq is distributed in the hope that it will be useful, 
// but WITHOUT ANY WARRANTY; without even the implied warranty of 
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the 
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public License
// along with re-linq; if not, see http://www.gnu.org/licenses.
// 

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Moq;
using NUnit.Framework;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.ResultOperators;
using Remotion.Linq.LinqToSqlAdapter.UnitTests.TestDomain;
using Remotion.Linq.SqlBackend.MappingResolution;
using Remotion.Linq.SqlBackend.SqlGeneration;
using Remotion.Linq.SqlBackend.SqlPreparation;
using Remotion.Linq.SqlBackend.SqlStatementModel.Resolved;
using Remotion.Linq.SqlBackend.SqlStatementModel.Unresolved;

namespace Remotion.Linq.LinqToSqlAdapter.UnitTests
{
  [TestFixture]
  public class QueryExecutorTest
  {
    private MainFromClause _mainFromClause;
    private SelectClause _selectClause;
    private QueryModel _queryModel;
    private Mock<IMappingResolver> _resolverStub;

    [SetUp]
    public void SetUp ()
    {
      // var query = from c in Customers select null
      _mainFromClause = new MainFromClause ("c", typeof (DataContextTestClass.Customer), Expression.Constant (new DataContextTestClass.Customer[0]));
      _selectClause = new SelectClause (Expression.Constant (null, typeof (DataContextTestClass.Customer)));
      _queryModel = new QueryModel (_mainFromClause, _selectClause);

      _resolverStub = new Mock<IMappingResolver>();
      _resolverStub
          .Setup (stub => stub.ResolveTableInfo (It.IsAny<UnresolvedTableInfo>(), It.IsAny<UniqueIdentifierGenerator>()))
          .Returns (new ResolvedSimpleTableInfo (typeof (DataContextTestClass.Customer), "CustomerTable", "t0"));
      _resolverStub
          .Setup (stub => stub.ResolveConstantExpression ((ConstantExpression) _selectClause.Selector))
          .Returns (_selectClause.Selector);
    }

    [Test] 
    public void ExecuteScalar()
    {
      _queryModel.ResultOperators.Add (new CountResultOperator());

      object fakeResult = 10;

      var retrieverMock = GetRetrieverMockStrictScalar (fakeResult);

      var executor = CreateQueryExecutor (retrieverMock);
      var result = executor.ExecuteScalar<object> (_queryModel);

      retrieverMock.Verify();
      Assert.That (result, Is.SameAs (fakeResult));
    }

    [Test]
    public void ExecuteSingle ()
    {
      var fakeResult = new[] { new DataContextTestClass.Customer () };

      var retrieverMock = GetRetrieverMockStrict (fakeResult);

      var executor = CreateQueryExecutor (retrieverMock);
      var result = executor.ExecuteSingle<DataContextTestClass.Customer> (_queryModel, true);

      retrieverMock.Verify();
      Assert.That (result, Is.SameAs (fakeResult[0]));
    }

    [Test]
    public void ExecuteSingle_Empty_ShouldGetDefault ()
    {
      var fakeResult = new DataContextTestClass.Customer[0];

      var retrieverMock = GetRetrieverMockStrict (fakeResult);

      var executor = CreateQueryExecutor (retrieverMock);
      var result = executor.ExecuteSingle<DataContextTestClass.Customer> (_queryModel, true);

      retrieverMock.Verify();
      Assert.That (result, Is.EqualTo (default (DataContextTestClass.Customer)));
    }

    [Test]
    public void ExecuteSingle_Empty_ShouldThrowException ()
    {
      var fakeResult = new DataContextTestClass.Customer[0];

      var retrieverMock = GetRetrieverMockStrict (fakeResult);

      var executor = CreateQueryExecutor (retrieverMock);
      Assert.That (
          () => executor.ExecuteSingle<DataContextTestClass.Customer> (_queryModel, false),
          Throws.InvalidOperationException
              .With.Message.EqualTo ("Sequence contains no elements"));
    }

    [Test]
    public void ExecuteSingle_Many_ShouldThrowException ()
    {
      var fakeResult = new[] { new DataContextTestClass.Customer(),new DataContextTestClass.Customer() };

      var retrieverMock = GetRetrieverMockStrict (fakeResult);

      var executor = CreateQueryExecutor (retrieverMock);
      Assert.That (
          () => executor.ExecuteSingle<DataContextTestClass.Customer> (_queryModel, false),
          Throws.InvalidOperationException
              .With.Message.EqualTo ("Sequence contains more than one element"));
    }

    [Test]
    public void ExecuteCollection ()
    {
      var fakeResult = new[] { new DataContextTestClass.Customer(), new DataContextTestClass.Customer() };

      var retrieverMock = GetRetrieverMockStrict (fakeResult);

      var executor = CreateQueryExecutor (retrieverMock);
      var result = executor.ExecuteCollection<DataContextTestClass.Customer> (_queryModel);

      retrieverMock.Verify();
      Assert.That (result, Is.SameAs (fakeResult));
    }

    private QueryExecutor CreateQueryExecutor(Mock<IQueryResultRetriever> retrieverMock)
    {
      return new QueryExecutor (_resolverStub.Object, retrieverMock.Object, ResultOperatorHandlerRegistry.CreateDefault (), CompoundMethodCallTransformerProvider.CreateDefault (), false);
    }

    private static Mock<IQueryResultRetriever> GetRetrieverMockStrict(IEnumerable<DataContextTestClass.Customer> fakeResult)
    {
      var retrieverMock = new Mock<IQueryResultRetriever> (MockBehavior.Strict);
      retrieverMock
          .Setup (stub => stub.GetResults (
              It.IsAny<Func<IDatabaseResultRow, DataContextTestClass.Customer>>(),
              "SELECT NULL AS [value] FROM [CustomerTable] AS [t0]",
              new CommandParameter[0]))
          .Returns (fakeResult)
          .Verifiable();
      return retrieverMock;
    }

    private static Mock<IQueryResultRetriever> GetRetrieverMockStrictScalar (object fakeResult)
    {
      var retrieverMock = new Mock<IQueryResultRetriever> (MockBehavior.Strict);
      retrieverMock
          .Setup (stub => stub.GetScalar (It.IsAny< Func<IDatabaseResultRow, object>>(), 
              "SELECT COUNT(*) AS [value] FROM [CustomerTable] AS [t0]",
              new CommandParameter[0]))
          .Returns (fakeResult)
          .Verifiable();
      return retrieverMock;
    }
  }
}