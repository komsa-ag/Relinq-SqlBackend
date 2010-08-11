// This file is part of the re-motion Core Framework (www.re-motion.org)
// Copyright (C) 2005-2009 rubicon informationstechnologie gmbh, www.rubicon.eu
// 
// The re-motion Core Framework is free software; you can redistribute it 
// and/or modify it under the terms of the GNU Lesser General Public License 
// as published by the Free Software Foundation; either version 2.1 of the 
// License, or (at your option) any later version.
// 
// re-motion is distributed in the hope that it will be useful, 
// but WITHOUT ANY WARRANTY; without even the implied warranty of 
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the 
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public License
// along with re-motion; if not, see http://www.gnu.org/licenses.
// 
using System;
using System.Linq.Expressions;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using Remotion.Data.Linq.Clauses;
using Remotion.Data.Linq.Clauses.ResultOperators;
using Remotion.Data.Linq.LinqToSqlAdapter;
using Remotion.Data.Linq.SqlBackend.MappingResolution;
using Remotion.Data.Linq.SqlBackend.SqlGeneration;
using Remotion.Data.Linq.SqlBackend.SqlStatementModel.Resolved;
using Remotion.Data.Linq.SqlBackend.SqlStatementModel.Unresolved;
using Rhino.Mocks;

namespace Remotion.Data.Linq.UnitTests.LinqToSqlAdapter
{
  [TestFixture]
  public class QueryExecutorTest
  {
    private MainFromClause _mainFromClause;
    private SelectClause _selectClause;
    private QueryModel _queryModel;
    private IMappingResolver _resolverStub;

    [SetUp]
    public void SetUp ()
    {
      // var query = from c in Customers select null
      _mainFromClause = new MainFromClause ("c", typeof (DataContextTestClass.Customer), Expression.Constant (new DataContextTestClass.Customer[0]));
      _selectClause = new SelectClause (Expression.Constant (null, typeof (DataContextTestClass.Customer)));
      _queryModel = new QueryModel (_mainFromClause, _selectClause);

      _resolverStub = MockRepository.GenerateStub<IMappingResolver> ();
      _resolverStub
          .Stub (stub => stub.ResolveTableInfo (Arg<UnresolvedTableInfo>.Is.Anything, Arg<UniqueIdentifierGenerator>.Is.Anything))
          .Return (new ResolvedSimpleTableInfo (typeof (DataContextTestClass.Customer), "CustomerTable", "t0"));
      _resolverStub
          .Stub (stub => stub.ResolveConstantExpression ((ConstantExpression) _selectClause.Selector))
          .Return (_selectClause.Selector);
    }

    [Test] 
    public void ExecuteScalar()
    {
      var mainFromClause= new MainFromClause ("c", typeof (DataContextTestClass.Customer), Expression.Constant (new DataContextTestClass.Customer[0]));
      var selectClause = new SelectClause (Expression.Constant (null, typeof (DataContextTestClass.Customer)));
      var queryModel = new QueryModel (mainFromClause, selectClause);
      queryModel.ResultOperators.Add (new CountResultOperator());

      var resolverStub = MockRepository.GenerateStub<IMappingResolver> ();
      resolverStub
          .Stub (stub => stub.ResolveTableInfo (Arg<UnresolvedTableInfo>.Is.Anything, Arg<UniqueIdentifierGenerator>.Is.Anything))
          .Return (new ResolvedSimpleTableInfo (typeof (DataContextTestClass.Customer), "CustomerTable", "t0"));
      resolverStub
          .Stub (stub => stub.ResolveConstantExpression ((ConstantExpression) selectClause.Selector))
          .Return (selectClause.Selector);

      object fakeResult = 10;

      var retrieverMock = QueryExecutorTest.GetRetrieverMockStrictScalar (fakeResult);

      var executor = new QueryExecutor (retrieverMock, resolverStub);
      var result = executor.ExecuteScalar<object> (queryModel);

      retrieverMock.VerifyAllExpectations ();
      Assert.That (result, Is.SameAs (fakeResult));
    }

    [Test]
    public void ExecuteSingle ()
    {
      var fakeResult = new[] { new DataContextTestClass.Customer () };

      var retrieverMock = QueryExecutorTest.GetRetrieverMockStrict (fakeResult);

      var executor = new QueryExecutor (retrieverMock, _resolverStub);
      var result = executor.ExecuteSingle<DataContextTestClass.Customer> (_queryModel, true);

      retrieverMock.VerifyAllExpectations ();
      Assert.That (result, Is.SameAs (fakeResult[0]));
    }

    [Test]
    public void ExecuteSingle_Empty_ShouldGetDefault ()
    {
      DataContextTestClass.Customer[] fakeResult = new DataContextTestClass.Customer[0];

      var retrieverMock = QueryExecutorTest.GetRetrieverMockStrict (fakeResult);

      var executor = new QueryExecutor (retrieverMock, _resolverStub);
      var result = executor.ExecuteSingle<DataContextTestClass.Customer> (_queryModel, true);

      retrieverMock.VerifyAllExpectations ();
      Assert.That (result, Is.SameAs (default(DataContextTestClass.Customer)));
    }

    [Test]
    [ExpectedException(ExceptionType = typeof(System.InvalidOperationException), ExpectedMessage = "Sequence contains no elements")]
    public void ExecuteSingle_Empty_ShouldThrowException ()
    {
      DataContextTestClass.Customer[] fakeResult = new DataContextTestClass.Customer[0];

      var retrieverMock = QueryExecutorTest.GetRetrieverMockStrict (fakeResult);

      var executor = new QueryExecutor (retrieverMock, _resolverStub);
      executor.ExecuteSingle<DataContextTestClass.Customer> (_queryModel, false);
    }

    [Test]
    [ExpectedException (ExceptionType = typeof (System.InvalidOperationException), ExpectedMessage = "Sequence contains more than one element")]
    public void ExecuteSingle_Many_ShouldThrowException ()
    {
      DataContextTestClass.Customer[] fakeResult = new[] {new DataContextTestClass.Customer(),new DataContextTestClass.Customer()};

      var retrieverMock = QueryExecutorTest.GetRetrieverMockStrict (fakeResult);

      var executor = new QueryExecutor (retrieverMock, _resolverStub);
      executor.ExecuteSingle<DataContextTestClass.Customer> (_queryModel, false);
    }

    [Test]
    public void ExecuteCollection ()
    {
      var fakeResult = new[] { new DataContextTestClass.Customer(), new DataContextTestClass.Customer() };

      var retrieverMock = QueryExecutorTest.GetRetrieverMockStrict (fakeResult);

      var executor = new QueryExecutor (retrieverMock, _resolverStub);
      var result = executor.ExecuteCollection<DataContextTestClass.Customer> (_queryModel);

      retrieverMock.VerifyAllExpectations();
      Assert.That (result, Is.SameAs (fakeResult));
    }


    #region staticMethods
    private static IQueryResultRetriever GetRetrieverMockStrict(DataContextTestClass.Customer[] fakeResult)
    {
      var retrieverMock = MockRepository.GenerateStrictMock<IQueryResultRetriever> ();
      retrieverMock
          .Expect (stub => stub.GetResults (
              Arg<Func<IDatabaseResultRow, DataContextTestClass.Customer>>.Is.Anything,
              Arg.Is ("SELECT NULL AS [value] FROM [CustomerTable] AS [t0]"),
              Arg<CommandParameter[]>.List.Equal (new CommandParameter[0])))
          .Return (fakeResult);
      retrieverMock.Replay ();
      return retrieverMock;
    }

    private static IQueryResultRetriever GetRetrieverMockStrictScalar (object fakeResult)
    {
      var retrieverMock = MockRepository.GenerateStrictMock<IQueryResultRetriever> ();
      retrieverMock
          .Expect (stub => stub.GetScalar<object> (
              Arg.Is ("SELECT COUNT(*) AS [value] FROM [CustomerTable] AS [t0]"),
              Arg<CommandParameter[]>.List.Equal (new CommandParameter[0])))
          .Return (fakeResult);
      retrieverMock.Replay ();
      return retrieverMock;
    }
    #endregion
  }
}