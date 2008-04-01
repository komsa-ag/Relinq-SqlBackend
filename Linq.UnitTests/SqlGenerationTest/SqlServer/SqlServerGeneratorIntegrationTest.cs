using System;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using Rubicon.Collections;
using Rubicon.Data.Linq.Parsing;
using Rubicon.Data.Linq.SqlGeneration;
using Rubicon.Data.Linq.SqlGeneration.SqlServer;
using Rubicon.Data.Linq.UnitTests.TestQueryGenerators;

namespace Rubicon.Data.Linq.UnitTests.SqlGenerationTest.SqlServer
{
  [TestFixture]
  public class SqlServerGeneratorIntegrationTest
  {
    private IQueryable<Student> _source;

    [SetUp]
    public void SetUp ()
    {
      _source = ExpressionHelper.CreateQuerySource ();
    }

    [Test]
    public void DefaultParseContext ()
    {
      SqlServerGenerator generator = new SqlServerGenerator (ExpressionHelper.CreateQueryModel (), StubDatabaseInfo.Instance);
      Assert.AreEqual (ParseContext.TopLevelQuery, generator.ParseContext);
    }

    [Test]
    [ExpectedException (typeof (InvalidOperationException), ExpectedMessage = "The query does not select any fields from the data source.")]
    public void SimpleQuery_WithNonDBFieldProjection ()
    {
      IQueryable<Student> query = SelectTestQueryGenerator.CreateSimpleQueryWithNonDBProjection (_source);
      QueryModel parsedQuery = ExpressionHelper.ParseQuery (query);
      new SqlServerGenerator (parsedQuery, StubDatabaseInfo.Instance).BuildCommandString ();
    }

    [Test]
    public void SimpleQuery ()
    {
      IQueryable<Student> query = SelectTestQueryGenerator.CreateSimpleQuery (_source);
      QueryModel parsedQuery = ExpressionHelper.ParseQuery (query);
      SqlServerGenerator sqlGenerator = new SqlServerGenerator (parsedQuery, StubDatabaseInfo.Instance);
      Tuple<string, CommandParameter[]> result = sqlGenerator.BuildCommandString ();
      Assert.AreEqual ("SELECT [s].* FROM [studentTable] [s]", result.A);

      Assert.IsEmpty (result.B);
    }

    [Test]
    public void MultiFromQueryWithProjection ()
    {
      IQueryable<Tuple<string, string, int>> query = MixedTestQueryGenerator.CreateMultiFromQueryWithProjection (_source, _source, _source);
      QueryModel parsedQuery = ExpressionHelper.ParseQuery (query);
      SqlServerGenerator sqlGenerator = new SqlServerGenerator (parsedQuery, StubDatabaseInfo.Instance);
      Tuple<string, CommandParameter[]> result = sqlGenerator.BuildCommandString ();
      Assert.AreEqual ("SELECT [s1].[FirstColumn], [s2].[LastColumn], [s3].[IDColumn] FROM [studentTable] [s1], [studentTable] [s2], [studentTable] [s3]",
          result.A);

      Assert.IsEmpty (result.B);
    }

    [Test]
    public void SimpleWhereQuery ()
    {
      IQueryable<Student> query = WhereTestQueryGenerator.CreateSimpleWhereQuery (_source);
      QueryModel parsedQuery = ExpressionHelper.ParseQuery (query);
      SqlServerGenerator sqlGenerator = new SqlServerGenerator (parsedQuery, StubDatabaseInfo.Instance);

      Tuple<string, CommandParameter[]> result = sqlGenerator.BuildCommandString ();
      Assert.AreEqual ("SELECT [s].* FROM [studentTable] [s] WHERE ([s].[LastColumn] = @1)", result.A);

      CommandParameter[] parameters = result.B;
      Assert.That (parameters, Is.EqualTo (new object[] { new CommandParameter ("@1", "Garcia") }));
    }

    [Test]
    public void MultiWhereQuery ()
    {
      IQueryable<Student> query = WhereTestQueryGenerator.CreateMultiWhereQuery (_source);
      QueryModel parsedQuery = ExpressionHelper.ParseQuery (query);
      SqlServerGenerator sqlGenerator = new SqlServerGenerator (parsedQuery, StubDatabaseInfo.Instance);

      Tuple<string, CommandParameter[]> result = sqlGenerator.BuildCommandString ();
      Assert.AreEqual ("SELECT [s].* FROM [studentTable] [s] WHERE ((([s].[LastColumn] = @1) AND ([s].[FirstColumn] = @2)) AND ([s].[IDColumn] > @3))",
          result.A);

      CommandParameter[] parameters = result.B;
      Assert.That (parameters,
          Is.EqualTo (new object[] {new CommandParameter ("@1", "Garcia"), new CommandParameter ("@2", "Hugo"), new CommandParameter ("@3", 100)}));
    }

    [Test]
    public void WhereQueryWithOrAndNot ()
    {
      IQueryable<Student> query = WhereTestQueryGenerator.CreateWhereQueryWithOrAndNot (_source);
      QueryModel parsedQuery = ExpressionHelper.ParseQuery (query);
      SqlServerGenerator sqlGenerator = new SqlServerGenerator (parsedQuery, StubDatabaseInfo.Instance);

      Tuple<string, CommandParameter[]> result = sqlGenerator.BuildCommandString ();
      Assert.AreEqual ("SELECT [s].* FROM [studentTable] [s] "
          + "WHERE ((NOT ([s].[FirstColumn] = @1) OR ([s].[FirstColumn] = @2)) AND ([s].[FirstColumn] = @3))",
          result.A);

      CommandParameter[] parameters = result.B;
      Assert.That (parameters, Is.EqualTo (new object[] { new CommandParameter ("@1", "Garcia"),
          new CommandParameter ("@2", "Garcia"), new CommandParameter ("@3", "Garcia") }));
    }

    [Test]
    public void WhereQueryWithComparisons ()
    {
      IQueryable<Student> query = WhereTestQueryGenerator.CreateWhereQueryWithDifferentComparisons (_source);
      QueryModel parsedQuery = ExpressionHelper.ParseQuery (query);
      SqlServerGenerator sqlGenerator = new SqlServerGenerator (parsedQuery, StubDatabaseInfo.Instance);

      Tuple<string, CommandParameter[]> result = sqlGenerator.BuildCommandString ();
      Assert.AreEqual ("SELECT [s].* FROM [studentTable] [s] WHERE ("
          + "((((([s].[FirstColumn] IS NULL OR [s].[FirstColumn] <> @1) "
          + "AND ([s].[IDColumn] > @2)) "
          + "AND ([s].[IDColumn] >= @3)) "
          + "AND ([s].[IDColumn] < @4)) "
          + "AND ([s].[IDColumn] <= @5)) "
          + "AND ([s].[IDColumn] = @6)"
          + ")",
          result.A);

      CommandParameter[] parameters = result.B;
      Assert.That (parameters, Is.EqualTo (new object[] { new CommandParameter ("@1", "Garcia"),
          new CommandParameter ("@2", 5), new CommandParameter ("@3", 6), new CommandParameter ("@4", 7),
          new CommandParameter ("@5", 6), new CommandParameter ("@6", 6)}));
    }

    [Test]
    public void WhereQueryWithNullChecks ()
    {
      IQueryable<Student> query = WhereTestQueryGenerator.CreateWhereQueryNullChecks (_source);
      QueryModel parsedQuery = ExpressionHelper.ParseQuery (query);
      SqlServerGenerator sqlGenerator = new SqlServerGenerator (parsedQuery, StubDatabaseInfo.Instance);

      Tuple<string, CommandParameter[]> result = sqlGenerator.BuildCommandString ();
      Assert.AreEqual ("SELECT [s].* FROM [studentTable] [s] WHERE ([s].[FirstColumn] IS NULL OR [s].[LastColumn] IS NOT NULL)",
          result.A);

      CommandParameter[] parameters = result.B;
      Assert.That (parameters, Is.Empty);
    }

    [Test]
    public void WhereQueryWithBooleanConstantTrue ()
    {
      IQueryable<Student> query = WhereTestQueryGenerator.CreateWhereQueryBooleanConstantTrue (_source);
      QueryModel parsedQuery = ExpressionHelper.ParseQuery (query);
      SqlServerGenerator sqlGenerator = new SqlServerGenerator (parsedQuery, StubDatabaseInfo.Instance);

      Tuple<string, CommandParameter[]> result = sqlGenerator.BuildCommandString ();
      Assert.AreEqual ("SELECT [s].* FROM [studentTable] [s] WHERE (1=1)",
          result.A);

      CommandParameter[] parameters = result.B;
      Assert.That (parameters, Is.Empty);
    }

    [Test]
    public void WhereQueryWithBooleanConstantFalse ()
    {
      IQueryable<Student> query = WhereTestQueryGenerator.CreateWhereQueryBooleanConstantFalse (_source);
      QueryModel parsedQuery = ExpressionHelper.ParseQuery (query);
      SqlServerGenerator sqlGenerator = new SqlServerGenerator (parsedQuery, StubDatabaseInfo.Instance);

      Tuple<string, CommandParameter[]> result = sqlGenerator.BuildCommandString ();
      Assert.AreEqual ("SELECT [s].* FROM [studentTable] [s] WHERE (1<>1)",
          result.A);

      CommandParameter[] parameters = result.B;
      Assert.That (parameters, Is.Empty);
    }

    [Test]
    public void WhereQueryWithStartsWith ()
    {
      IQueryable<Student> query = WhereTestQueryGenerator.CreateWhereQueryWithStartsWith (_source);
      QueryModel parsedQuery = ExpressionHelper.ParseQuery (query);
      SqlServerGenerator sqlGenerator = new SqlServerGenerator (parsedQuery, StubDatabaseInfo.Instance);
      Tuple<string, CommandParameter[]> result = sqlGenerator.BuildCommandString ();
      Assert.AreEqual ("SELECT [s].* FROM [studentTable] [s] WHERE ([s].[FirstColumn] LIKE @1)",
          result.A);
      CommandParameter[] parameters = result.B;
      Assert.That (parameters, Is.EqualTo (new object[] { new CommandParameter ("@1", "Garcia%") }));
    }

    [Test]
    public void WhereQueryWithEndsWith ()
    {
      IQueryable<Student> query = WhereTestQueryGenerator.CreateWhereQueryWithEndsWith (_source);
      QueryModel parsedQuery = ExpressionHelper.ParseQuery (query);
      SqlServerGenerator sqlGenerator = new SqlServerGenerator (parsedQuery, StubDatabaseInfo.Instance);
      Tuple<string, CommandParameter[]> result = sqlGenerator.BuildCommandString ();
      Assert.AreEqual ("SELECT [s].* FROM [studentTable] [s] WHERE ([s].[FirstColumn] LIKE @1)",
          result.A);
      CommandParameter[] parameters = result.B;
      Assert.That (parameters, Is.EqualTo (new object[] { new CommandParameter ("@1", "%Garcia") }));
    }

    [Test]
    public void SimpleOrderByQuery ()
    {
      IQueryable<Student> query = OrderByTestQueryGenerator.CreateSimpleOrderByQuery (_source);
      QueryModel parsedQuery = ExpressionHelper.ParseQuery (query);
      SqlServerGenerator sqlGenerator = new SqlServerGenerator (parsedQuery, StubDatabaseInfo.Instance);
      Tuple<string, CommandParameter[]> result = sqlGenerator.BuildCommandString ();

      Assert.AreEqual ("SELECT [s1].* FROM [studentTable] [s1] ORDER BY [s1].[FirstColumn] ASC",
          result.A);
    }

    [Test]
    public void ComplexOrderByQuery ()
    {
      IQueryable<Student> query = OrderByTestQueryGenerator.CreateTwoOrderByQuery (_source);
      QueryModel parsedQuery = ExpressionHelper.ParseQuery (query);
      SqlServerGenerator sqlGenerator = new SqlServerGenerator (parsedQuery, StubDatabaseInfo.Instance);
      Tuple<string, CommandParameter[]> result = sqlGenerator.BuildCommandString ();
      Assert.AreEqual ("SELECT [s1].* FROM [studentTable] [s1] ORDER BY [s1].[FirstColumn] ASC, [s1].[LastColumn] DESC",
          result.A);
    }

    [Test]
    public void SimpleImplicitJoin ()
    {
      // from sd in source orderby sd.Student.First select sd
      IQueryable<Student_Detail> query = JoinTestQueryGenerator.CreateSimpleImplicitOrderByJoin (ExpressionHelper.CreateQuerySource_Detail ());
      QueryModel parsedQuery = ExpressionHelper.ParseQuery (query);
      SqlServerGenerator sqlGenerator = new SqlServerGenerator (parsedQuery, StubDatabaseInfo.Instance);
      Tuple<string, CommandParameter[]> result = sqlGenerator.BuildCommandString ();
      Assert.AreEqual ("SELECT [sd].* FROM [detailTable] [sd] LEFT OUTER JOIN [studentTable] [j0] "
          + "ON [sd].[Student_Detail_PK] = [j0].[Student_Detail_to_Student_FK] ORDER BY [j0].[FirstColumn] ASC",
          result.A);
    }

    [Test]
    public void NestedImplicitJoin ()
    {
      // from sdd in source orderby sdd.Student_Detail.Student.First select sdd
      IQueryable<Student_Detail_Detail> query = JoinTestQueryGenerator.CreateDoubleImplicitOrderByJoin (ExpressionHelper.CreateQuerySource_Detail_Detail ());
      QueryModel parsedQuery = ExpressionHelper.ParseQuery (query);
      SqlServerGenerator sqlGenerator = new SqlServerGenerator (parsedQuery, StubDatabaseInfo.Instance);
      Tuple<string, CommandParameter[]> result = sqlGenerator.BuildCommandString ();
      string expectedString = "SELECT [sdd].* FROM [detailDetailTable] [sdd] "
          + "LEFT OUTER JOIN [detailTable] [j0] ON [sdd].[Student_Detail_Detail_PK] = [j0].[Student_Detail_Detail_to_Student_Detail_FK] "
          + "LEFT OUTER JOIN [studentTable] [j1] ON [j0].[Student_Detail_PK] = [j1].[Student_Detail_to_Student_FK] "
          + "ORDER BY [j1].[FirstColumn] ASC";
      Assert.AreEqual (expectedString, result.A);
    }

    [Test]
    public void JoinReuse()
    {
      // from sdd1 in ...
      // from sdd2 in ...
      // order by sdd1.Student_Detail.Student.First
      // order by sdd2.Student_Detail.Student.First
      // order by sdd1.Student_Detail.Student.First
      // select sdd1;

      IQueryable<Student_Detail_Detail> source1 = ExpressionHelper.CreateQuerySource_Detail_Detail ();
      IQueryable<Student_Detail_Detail> source2 = ExpressionHelper.CreateQuerySource_Detail_Detail ();
      IQueryable<Student_Detail_Detail> query = JoinTestQueryGenerator.CreateImplicitOrderByJoinWithJoinReuse (source1, source2);
      QueryModel parsedQuery = ExpressionHelper.ParseQuery (query);
      SqlServerGenerator sqlGenerator = new SqlServerGenerator (parsedQuery, StubDatabaseInfo.Instance);

      string expectedString = "SELECT [sdd1].* "
          + "FROM "
          + "[detailDetailTable] [sdd1] "
          + "LEFT OUTER JOIN [detailTable] [j0] ON [sdd1].[Student_Detail_Detail_PK] = [j0].[Student_Detail_Detail_to_Student_Detail_FK] "
          + "LEFT OUTER JOIN [studentTable] [j1] ON [j0].[Student_Detail_PK] = [j1].[Student_Detail_to_Student_FK], "
          + "[detailDetailTable] [sdd2] "
          + "LEFT OUTER JOIN [detailTable] [j2] ON [sdd2].[Student_Detail_Detail_PK] = [j2].[Student_Detail_Detail_to_Student_Detail_FK] "
          + "LEFT OUTER JOIN [studentTable] [j3] ON [j2].[Student_Detail_PK] = [j3].[Student_Detail_to_Student_FK] "
          + "ORDER BY [j1].[FirstColumn] ASC, [j3].[FirstColumn] ASC, [j1].[FirstColumn] ASC";

      Tuple<string, CommandParameter[]> result = sqlGenerator.BuildCommandString();
      Assert.AreEqual (expectedString, result.A);
    }

    [Test]
    public void JoinPartReuse ()
    {
      //from sdd in ...
      //orderby sdd.Student_Detail.Student.First
      //orderby sdd.Student_Detail.ID
      //select sdd;

      IQueryable<Student_Detail_Detail> source1 = ExpressionHelper.CreateQuerySource_Detail_Detail ();
      IQueryable<Student_Detail_Detail> query = JoinTestQueryGenerator.CreateImplicitOrderByJoinWithJoinPartReuse (source1);
      QueryModel parsedQuery = ExpressionHelper.ParseQuery (query);
      SqlServerGenerator sqlGenerator = new SqlServerGenerator (parsedQuery, StubDatabaseInfo.Instance);

      string expectedString = "SELECT [sdd].* "
          + "FROM "
          + "[detailDetailTable] [sdd] "
          + "LEFT OUTER JOIN [detailTable] [j0] ON [sdd].[Student_Detail_Detail_PK] = [j0].[Student_Detail_Detail_to_Student_Detail_FK] "
          + "LEFT OUTER JOIN [studentTable] [j1] ON [j0].[Student_Detail_PK] = [j1].[Student_Detail_to_Student_FK] "
          + "ORDER BY [j1].[FirstColumn] ASC, [j0].[IDColumn] ASC";

      Tuple<string, CommandParameter[]> result = sqlGenerator.BuildCommandString ();
      Assert.AreEqual (expectedString, result.A);
    }

    [Test]
    public void SelectJoin()
    {
      // from sdd in source 
      // select new Tuple<string,int>{sdd.Student_Detail.Student.First,sdd.IndustrialSector.ID}

      IQueryable<Student_Detail_Detail> source = ExpressionHelper.CreateQuerySource_Detail_Detail ();

      IQueryable<Tuple<string, int>> query = JoinTestQueryGenerator.CreateComplexImplicitSelectJoin (source);
      QueryModel parsedQuery = ExpressionHelper.ParseQuery (query);
      SqlServerGenerator sqlGenerator = new SqlServerGenerator (parsedQuery, StubDatabaseInfo.Instance);

      string expectedString = "SELECT [j1].[FirstColumn], [j2].[IDColumn] "
          + "FROM "
          + "[detailDetailTable] [sdd] "
          + "LEFT OUTER JOIN [detailTable] [j0] ON [sdd].[Student_Detail_Detail_PK] = [j0].[Student_Detail_Detail_to_Student_Detail_FK] "
          + "LEFT OUTER JOIN [studentTable] [j1] ON [j0].[Student_Detail_PK] = [j1].[Student_Detail_to_Student_FK] "
          + "LEFT OUTER JOIN [industrialTable] [j2] ON [sdd].[Student_Detail_Detail_PK] = [j2].[Student_Detail_Detail_to_IndustrialSector_FK]";

      Tuple<string, CommandParameter[]> result = sqlGenerator.BuildCommandString ();
      Assert.AreEqual (expectedString, result.A);
    }

    [Test]
    public void SelectJoin_WithRelationMember()
    {
      IQueryable<Student_Detail> source = ExpressionHelper.CreateQuerySource_Detail ();

      IQueryable<Student> query = SelectTestQueryGenerator.CreateRelationMemberSelectQuery (source);
      QueryModel parsedQuery = ExpressionHelper.ParseQuery (query);

      SqlServerGenerator sqlGenerator = new SqlServerGenerator (parsedQuery, StubDatabaseInfo.Instance);

      const string expectedString = "SELECT [j0].* FROM [detailTable] [sd] LEFT OUTER JOIN "
          + "[studentTable] [j0] ON [sd].[Student_Detail_PK] = [j0].[Student_Detail_to_Student_FK]";

      Tuple<string, CommandParameter[]> result = sqlGenerator.BuildCommandString ();
      Assert.AreEqual (expectedString, result.A);
    }

    [Test]
    public void Select_WithDistinct ()
    {
      IQueryable<Student> source = ExpressionHelper.CreateQuerySource();
      IQueryable<string> query = DistinctTestQueryGenerator.CreateSimpleDistinctQuery (source);

      QueryModel parsedQuery = ExpressionHelper.ParseQuery (query);

      SqlServerGenerator sqlGenerator = new SqlServerGenerator (parsedQuery, StubDatabaseInfo.Instance);

      const string expectedString = "SELECT DISTINCT [s].[FirstColumn] FROM [studentTable] [s]";

      Tuple<string, CommandParameter[]> result = sqlGenerator.BuildCommandString ();
      Assert.AreEqual (expectedString, result.A);
    }

    [Test]
    public void Select_WithDistinctAndWhere ()
    {
      IQueryable<Student> source = ExpressionHelper.CreateQuerySource ();
      IQueryable<string> query = DistinctTestQueryGenerator.CreateDisinctWithWhereQuery (source);

      QueryModel parsedQuery = ExpressionHelper.ParseQuery (query);

      SqlServerGenerator sqlGenerator = new SqlServerGenerator (parsedQuery, StubDatabaseInfo.Instance);

      const string expectedString = "SELECT DISTINCT [s].[FirstColumn] FROM [studentTable] [s] WHERE ([s].[FirstColumn] = @1)";

      Tuple<string, CommandParameter[]> result = sqlGenerator.BuildCommandString ();
      Assert.AreEqual (expectedString, result.A);
    }

    [Test]
    public void WhereJoin_WithRelationMember ()
    {
      IQueryable<Student_Detail> source = ExpressionHelper.CreateQuerySource_Detail ();

      IQueryable<Student_Detail> query = WhereTestQueryGenerator.CreateRelationMemberWhereQuery (source);
      QueryModel parsedQuery = ExpressionHelper.ParseQuery (query);

      SqlServerGenerator sqlGenerator = new SqlServerGenerator (parsedQuery, StubDatabaseInfo.Instance);

      const string expectedString = "SELECT [sd].* FROM [detailTable] [sd] WHERE [sd].[Student_Detail_to_IndustrialSector_FK] IS NOT NULL";

      Tuple<string, CommandParameter[]> result = sqlGenerator.BuildCommandString ();
      Assert.AreEqual (expectedString, result.A);
    }

    [Test]
    public void WhereJoin_WithRelationMember_VirtualSide ()
    {
      IQueryable<IndustrialSector> source = ExpressionHelper.CreateQuerySource_IndustrialSector ();

      IQueryable<IndustrialSector> query = WhereTestQueryGenerator.CreateRelationMemberVirtualSideWhereQuery (source);
      QueryModel parsedQuery = ExpressionHelper.ParseQuery (query);

      SqlServerGenerator sqlGenerator = new SqlServerGenerator (parsedQuery, StubDatabaseInfo.Instance);

      const string expectedString = "SELECT [industrial].* FROM [industrialTable] [industrial] "
          + "LEFT OUTER JOIN [detailTable] [j0] ON [industrial].[IndustrialSector_PK] = [j0].[Student_Detail_to_IndustrialSector_FK] "
          + "WHERE [j0].[IDColumn] IS NOT NULL";

      Tuple<string, CommandParameter[]> result = sqlGenerator.BuildCommandString ();
      Assert.AreEqual (expectedString, result.A);
    }

    [Test]
    [ExpectedException (typeof (NotSupportedException), ExpectedMessage = "Ordering by 'Rubicon.Data.Linq.UnitTests.Student_Detail.Student' is not "
        + "supported because it is a relation member.")]
    public void OrderingJoin_WithRelationMember ()
    {
      IQueryable<Student_Detail> source = ExpressionHelper.CreateQuerySource_Detail ();

      IQueryable<Student_Detail> query = OrderByTestQueryGenerator.CreateRelationMemberOrderByQuery (source);
      QueryModel parsedQuery = ExpressionHelper.ParseQuery (query);

      SqlServerGenerator sqlGenerator = new SqlServerGenerator (parsedQuery, StubDatabaseInfo.Instance);
      sqlGenerator.BuildCommandString ();
    }

    [Test]
    public void SimpleSubQueryInAdditionalFromClause ()
    {
      IQueryable<Student> source = ExpressionHelper.CreateQuerySource ();

      IQueryable<Student> query = SubQueryTestQueryGenerator.CreateSimpleSubQueryInAdditionalFromClause (source);
      QueryModel parsedQuery = ExpressionHelper.ParseQuery (query);

      SqlServerGenerator sqlGenerator = new SqlServerGenerator (parsedQuery, StubDatabaseInfo.Instance);
      Tuple<string, CommandParameter[]> result = sqlGenerator.BuildCommandString ();

      Assert.AreEqual ("SELECT [s].* FROM [studentTable] [s] CROSS APPLY (SELECT [s3].* FROM [studentTable] [s3]) [s2]", result.A);
    }

    [Test]
    public void ComplexSubQueryInAdditionalFromClause ()
    {
      IQueryable<Student> source = ExpressionHelper.CreateQuerySource ();

      IQueryable<Student> query = SubQueryTestQueryGenerator.CreateComplexSubQueryInAdditionalFromClause (source);
      QueryModel parsedQuery = ExpressionHelper.ParseQuery (query);


      SqlServerGenerator sqlGenerator = new SqlServerGenerator (parsedQuery, StubDatabaseInfo.Instance);
      Tuple<string, CommandParameter[]> result = sqlGenerator.BuildCommandString ();

      Assert.AreEqual ("SELECT [s2].* FROM [studentTable] [s] CROSS APPLY (SELECT [s3].* FROM [studentTable] [s3] " 
          + "WHERE ((([s3].[IDColumn] IS NULL AND [s].[IDColumn] IS NULL) OR [s3].[IDColumn] = [s].[IDColumn]) AND ([s3].[IDColumn] > @1))) [s2]",
          result.A);
      Assert.That (result.B, Is.EqualTo (new[] {new CommandParameter ("@1", 3)}));
    }

    [Test]
    public void SimpleSubQueryInWhereClause ()
    {
      IQueryable<Student> source = ExpressionHelper.CreateQuerySource ();

      IQueryable<Student> query = SubQueryTestQueryGenerator.CreateSimpleSubQueryInWhereClause (source);
      QueryModel parsedQuery = ExpressionHelper.ParseQuery (query);

      SqlServerGenerator sqlGenerator = new SqlServerGenerator (parsedQuery, StubDatabaseInfo.Instance);
      Tuple<string, CommandParameter[]> result = sqlGenerator.BuildCommandString ();

      Assert.AreEqual ("SELECT [s].* FROM [studentTable] [s] WHERE [s].[IDColumn] IN (SELECT [s2].[IDColumn] FROM [studentTable] [s2])", result.A);
      Assert.That (result.B, Is.Empty);
    }

    [Test]
    public void SubQueryWithConstantInWhereClause ()
    {
      IQueryable<Student> source = ExpressionHelper.CreateQuerySource ();

      IQueryable<Student> query = SubQueryTestQueryGenerator.CreateSubQueryWithConstantInWhereClause (source);
      QueryModel parsedQuery = ExpressionHelper.ParseQuery (query);

      SqlServerGenerator sqlGenerator = new SqlServerGenerator (parsedQuery, StubDatabaseInfo.Instance);
      Tuple<string, CommandParameter[]> result = sqlGenerator.BuildCommandString ();

      Assert.AreEqual ("SELECT [s].* FROM [studentTable] [s] WHERE @1 IN (SELECT [s2].[IDColumn] FROM [studentTable] [s2])", result.A);
      Assert.That (result.B, Is.EqualTo (new[] { new CommandParameter ("@1", 5) }));
    }

    [Test]
    public void SubQuerySelectingColumnsWithConstantInWhereClause ()
    {
      IQueryable<Student> source = ExpressionHelper.CreateQuerySource ();

      IQueryable<Student> query = SubQueryTestQueryGenerator.CreateSubQuerySelectingColumnsWithConstantInWhereClause (source);
      QueryModel parsedQuery = ExpressionHelper.ParseQuery (query);

      SqlServerGenerator sqlGenerator = new SqlServerGenerator (parsedQuery, StubDatabaseInfo.Instance);
      Tuple<string, CommandParameter[]> result = sqlGenerator.BuildCommandString ();

      Assert.AreEqual ("SELECT [s].* FROM [studentTable] [s] WHERE @1 IN (SELECT [s2].[FirstColumn] FROM [studentTable] [s2])", result.A);
      Assert.That (result.B, Is.EqualTo (new[] { new CommandParameter ("@1", "Hugo") }));
    }
  }
}