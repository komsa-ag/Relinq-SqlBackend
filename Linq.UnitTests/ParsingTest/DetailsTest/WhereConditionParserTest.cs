using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using System.Linq;
using Rubicon.Collections;
using Rubicon.Data.Linq.Clauses;
using Rubicon.Data.Linq.DataObjectModel;
using Rubicon.Data.Linq.Parsing;
using System.Linq.Expressions;
using Rubicon.Data.Linq.Parsing.Details;
using Rubicon.Data.Linq.Parsing.FieldResolving;
using NUnit.Framework.SyntaxHelpers;
using Rubicon.Data.Linq.UnitTests.TestQueryGenerators;

namespace Rubicon.Data.Linq.UnitTests.ParsingTest.DetailsTest
{
  [TestFixture]
  public class WhereConditionParserTest
  {
    private IDatabaseInfo _databaseInfo;
    private ParameterExpression _parameter;
    private MainFromClause _fromClause;
    private QueryExpression _queryExpression;
    private JoinedTableContext _context;
    
    [SetUp]
    public void SetUp()
    {
      _databaseInfo = StubDatabaseInfo.Instance;
      _parameter = Expression.Parameter (typeof (Student), "s");
      _fromClause = new MainFromClause (_parameter, ExpressionHelper.CreateQuerySource ());
      _queryExpression = new QueryExpression (_fromClause, ExpressionHelper.CreateQueryBody());
      _context = new JoinedTableContext();
    }
    
    [Test]
    [ExpectedException (typeof (ParserException), ExpectedMessage = "Expected binary expression, constant expression,method call expression or member expression for "
        + "where condition, found ConditionalExpression (IIF(True, True, True)).")]
    public void Invalid ()
    {
      WhereClause whereClause =
          new WhereClause (ExpressionHelper.CreateMainFromClause(),
              Expression.Lambda (Expression.Condition (Expression.Constant (true), Expression.Constant (true), Expression.Constant (true))));
      WhereConditionParser parser = new WhereConditionParser (_queryExpression, whereClause, _databaseInfo, _context, false);
      parser.GetParseResult();
    }

    [Test]
    public void Column()
    {
      PropertyInfo member = typeof (Student).GetProperty ("IsOld");
      Expression condition = Expression.MakeMemberAccess(_parameter,member);
      Tuple<List<FieldDescriptor>, ICriterion> parseResult = CreateAndParseWhereClause(condition);
      List<FieldDescriptor> fieldDescriptors = parseResult.A;
      ICriterion criterion = parseResult.B;
      
      FieldDescriptor expectedField = ExpressionHelper.CreateFieldDescriptor (_fromClause, member);

      Assert.AreEqual (expectedField.Column, criterion);
      Assert.That (fieldDescriptors, Is.EqualTo (new object[] { expectedField }));
    }

    [Test]
    [ExpectedException (typeof (FieldAccessResolveException), ExpectedMessage = "The member 'Rubicon.Data.Linq.UnitTests.Student.NonDBBoolProperty' "
        + "does not identify a queryable column.")]
    public void NonDbField ()
    {
      Expression condition = Expression.MakeMemberAccess (_parameter, typeof (Student).GetProperty ("NonDBBoolProperty"));
      CreateAndParseWhereClause (condition);
    }

    [Test]
    [ExpectedException (typeof (FieldAccessResolveException), ExpectedMessage = "The field access expression "
        + "'value(Rubicon.Data.Linq.UnitTests.Student).IsOld' does not contain a from clause identifier.")]
    public void InvalidMemberAccess ()
    {
      Expression condition = Expression.MakeMemberAccess (Expression.Constant (new Student()), typeof (Student).GetProperty ("IsOld"));
      CreateAndParseWhereClause(condition);
    }

    [Test]
    public void Constant ()
    {
      Expression condition = Expression.Constant(true);
      Tuple<List<FieldDescriptor>, ICriterion> parseResult = CreateAndParseWhereClause(condition);
      List<FieldDescriptor> fieldDescriptors = parseResult.A;
      ICriterion criterion = parseResult.B;

      Assert.AreEqual (new Constant(true), criterion);
      Assert.That (fieldDescriptors, Is.Empty);
    }

    [Test]
    [ExpectedException (typeof (ParserException), ExpectedMessage = "Expected and, or, or comparison expression for binary expression in where "
        + "condition, found ExpressionType (ArrayIndex).")]
    public void InvalidBinary ()
    {
      Expression condition = Expression.ArrayIndex (Expression.Constant (new bool[0]), Expression.Constant(0));
      CreateAndParseWhereClause(condition);  
    }

    [Test]
    public void BinaryAnd ()
    {
      Expression condition = Expression.And(Expression.Constant(true),Expression.Constant(true));
      Tuple<List<FieldDescriptor>, ICriterion> parseResult = CreateAndParseWhereClause(condition);
      ICriterion criterion = parseResult.B;

      Assert.AreEqual (new ComplexCriterion (new Constant (true), new Constant (true),ComplexCriterion.JunctionKind.And), criterion);
    }

    [Test]
    public void BinaryOr ()
    {
      Expression condition = Expression.Or (Expression.Constant (true), Expression.Constant (true));
      Tuple<List<FieldDescriptor>, ICriterion> parseResult = CreateAndParseWhereClause(condition);
      ICriterion criterion = parseResult.B;

      Assert.AreEqual (new ComplexCriterion (new Constant (true), new Constant (true), ComplexCriterion.JunctionKind.Or), criterion);
    }

    [Test]
    public void BinaryEquals ()
    {
      Expression condition = Expression.Equal (Expression.Constant (true), Expression.Constant (true));
      Tuple<List<FieldDescriptor>, ICriterion> parseResult = CreateAndParseWhereClause(condition);
      ICriterion criterion = parseResult.B;

      Assert.AreEqual (new BinaryCondition (new Constant (true), new Constant (true), BinaryCondition.ConditionKind.Equal), criterion);
    }

    [Test]
    public void BinaryEquals_VirtualColumn ()
    {
      IQueryable<IndustrialSector> source = ExpressionHelper.CreateQuerySource_IndustrialSector();
      ParameterExpression identifier = Expression.Parameter (typeof (IndustrialSector), "is");
      MainFromClause fromClause = new MainFromClause (identifier, source);
      PropertyInfo member = typeof (IndustrialSector).GetProperty ("Student_Detail");
      MemberExpression virtualColumnAccess = Expression.MakeMemberAccess (identifier, member);

      Expression condition = Expression.Equal (virtualColumnAccess, Expression.Constant (null));
      WhereClause whereClause = CreateWhereClause (condition);

      QueryExpression queryExpression = new QueryExpression (fromClause, _queryExpression.QueryBody);
      WhereConditionParser parser = new WhereConditionParser (queryExpression, whereClause, _databaseInfo, _context, false);
      Tuple<List<FieldDescriptor>, ICriterion> parseResult = parser.GetParseResult();
      ICriterion criterion = parseResult.B;

      Table relatedTable = new Table ("detailTable", null);

      Column expectedColumn = new Column(relatedTable, "IDColumn");
      Assert.AreEqual (new BinaryCondition (expectedColumn, new Constant (null), BinaryCondition.ConditionKind.Equal), criterion);
    }

    [Test]
    public void ConstantProcessedByDatabaseInfo ()
    {
      Student student = new Student ();
      student.ID = 4;
      MemberExpression memberAccess = Expression.MakeMemberAccess (_parameter, typeof (Student).GetProperty ("OtherStudent"));
      Expression condition = Expression.Equal (Expression.Constant (student), memberAccess);
      Tuple<List<FieldDescriptor>, ICriterion> parseResult = CreateAndParseWhereClause (condition);
      ICriterion criterion = parseResult.B;

      Table table = _fromClause.GetTable (StubDatabaseInfo.Instance);
      Assert.AreEqual (new BinaryCondition (new Constant (4), new Column (table, "OtherStudentColumn"),
          BinaryCondition.ConditionKind.Equal), criterion);
    }

    [Test]
    public void Binary_WithFields()
    {
      MemberExpression memberAccess1 = Expression.MakeMemberAccess (_parameter, typeof (Student).GetProperty ("First"));
      MemberExpression memberAccess2 = Expression.MakeMemberAccess (_parameter, typeof (Student).GetProperty ("Last"));

      Expression condition = Expression.Equal (memberAccess1, memberAccess2);
      Tuple<List<FieldDescriptor>, ICriterion> parseResult = CreateAndParseWhereClause(condition);
      List<FieldDescriptor> fieldDescriptors = parseResult.A;

      FieldDescriptor expectedField1 = ExpressionHelper.CreateFieldDescriptor(_fromClause, memberAccess1.Member);
      FieldDescriptor expectedField2 = ExpressionHelper.CreateFieldDescriptor (_fromClause, memberAccess2.Member);

      Assert.That (fieldDescriptors, Is.EqualTo (new object[] {expectedField1, expectedField2}));
    }

    [Test]
    public void Complex_WithFields ()
    {
      MemberExpression memberAccess1 = Expression.MakeMemberAccess (_parameter, typeof (Student).GetProperty ("IsOld"));
      MemberExpression memberAccess2 = Expression.MakeMemberAccess (_parameter, typeof (Student).GetProperty ("HasDog"));

      Expression condition = Expression.And (memberAccess1, memberAccess2);
      Tuple<List<FieldDescriptor>, ICriterion> parseResult = CreateAndParseWhereClause(condition);
      List<FieldDescriptor> fieldDescriptors = parseResult.A;

      FieldDescriptor expectedField1 = ExpressionHelper.CreateFieldDescriptor (_fromClause, memberAccess1.Member);
      FieldDescriptor expectedField2 = ExpressionHelper.CreateFieldDescriptor (_fromClause, memberAccess2.Member);

      Assert.That (fieldDescriptors, Is.EqualTo (new object[] { expectedField1, expectedField2 }));
    }

    [Test]
    public void MethodCallStartsWith ()
    {
      MemberExpression memberAccess = Expression.MakeMemberAccess (_parameter, typeof (Student).GetProperty ("First"));
      Expression condition = Expression.Call(
          memberAccess,
          typeof(string).GetMethod("StartsWith",new Type[] {typeof (string)}),
          Expression.Constant("Garcia")
          );
      Tuple<List<FieldDescriptor>, ICriterion> parseResult = CreateAndParseWhereClause(condition);
      ICriterion criterion = parseResult.B;

      Column expectedColumn = new Column (new Table ("studentTable", "s"), "FirstColumn");
      Assert.AreEqual (new BinaryCondition (expectedColumn, new Constant ("Garcia%"), 
          BinaryCondition.ConditionKind.Like), criterion);
    }

    [Test]
    public void MethodCallEndsWith ()
    {
      Expression condition = Expression.Call (
          Expression.MakeMemberAccess (_parameter, typeof (Student).GetProperty ("First")),
          typeof (string).GetMethod ("EndsWith", new Type[] { typeof (string) }),
          Expression.Constant ("Garcia")
          );
      Tuple<List<FieldDescriptor>, ICriterion> parseResult = CreateAndParseWhereClause(condition);
      ICriterion criterion = parseResult.B;

      Assert.AreEqual (new BinaryCondition (new Column (new Table ("studentTable", "s"), "FirstColumn"), new Constant ("%Garcia"), BinaryCondition.ConditionKind.Like), criterion);
    }

    [Test]
    public void CreateLike_WithField ()
    {
      MemberExpression memberAccess = Expression.MakeMemberAccess (_parameter, typeof (Student).GetProperty ("First"));
      Expression condition = Expression.Call (
          memberAccess,
          typeof (string).GetMethod ("StartsWith", new Type[] { typeof (string) }),
          Expression.Constant ("Garcia")
          );
      Tuple<List<FieldDescriptor>, ICriterion> parseResult = CreateAndParseWhereClause(condition);
      List<FieldDescriptor> fieldDescriptors = parseResult.A;
      ICriterion criterion = parseResult.B;

      FieldDescriptor expectedField = ExpressionHelper.CreateFieldDescriptor (_fromClause, memberAccess.Member);
      
      Assert.That (fieldDescriptors, Is.EqualTo (new object[] { expectedField }));
      Assert.AreEqual (new BinaryCondition (expectedField.GetMandatoryColumn(),
          new Constant ("Garcia%"), BinaryCondition.ConditionKind.Like), criterion);
    }

    [Test]
    public void UnaryNot ()
    {
      Expression condition = Expression.Not (Expression.Constant (true));
      Tuple<List<FieldDescriptor>, ICriterion> parseResult = CreateAndParseWhereClause(condition);
      ICriterion criterion = parseResult.B;

      Assert.AreEqual (new NotCriterion(new Constant(true)), criterion);
    }

    [Test]
    public void UnaryIgnoredConvert ()
    {
      Expression condition = Expression.Convert (Expression.Constant (true), typeof (bool));
      Tuple<List<FieldDescriptor>, ICriterion> parseResult = CreateAndParseWhereClause (condition);
      ICriterion criterion = parseResult.B;

      Assert.AreEqual (new Constant (true), criterion);
    }

    [Test]
    public void Unary_WithField ()
    {
      PropertyInfo member = typeof (Student).GetProperty ("IsOld");
      MemberExpression memberAccess = Expression.MakeMemberAccess (_parameter, member);
      Expression condition = Expression.Not (memberAccess);
      Tuple<List<FieldDescriptor>, ICriterion> parseResult = CreateAndParseWhereClause(condition);
      List<FieldDescriptor> fieldDescriptors = parseResult.A;

      FieldDescriptor expectedField = ExpressionHelper.CreateFieldDescriptor(_fromClause, member);

      Assert.That (fieldDescriptors, Is.EqualTo (new object[] { expectedField }));
    }

    [Test]
    [ExpectedException (typeof (ParserException), ExpectedMessage = "Expected not or convert expression for unary expression in where condition, found "
        + "ExpressionType (ConvertChecked).")]
    public void InvalidUnary ()
    {
      Expression condition = Expression.ConvertChecked (Expression.Constant (true), typeof (bool));
      CreateAndParseWhereClause(condition);
    }

    [Test]
    public void AllComparisons()
    {
      IQueryable<Student> query = WhereTestQueryGenerator.CreateWhereQueryWithDifferentComparisons (ExpressionHelper.CreateQuerySource());
      QueryExpression parsedQuery = ExpressionHelper.ParseQuery (query);
      WhereClause whereClause = ClauseFinder.FindClause<WhereClause> (parsedQuery.QueryBody.SelectOrGroupClause);
      WhereConditionParser parser = new WhereConditionParser (parsedQuery, whereClause, _databaseInfo, _context, false);
      Tuple<List<FieldDescriptor>, ICriterion> parseResult = parser.GetParseResult ();
      ICriterion result = parseResult.B;
      Column firstColumn = new Column(new Table("studentTable", "s"), "FirstColumn");
      Column idColumn = new Column (new Table ("studentTable", "s"), "IDColumn");
      BinaryCondition comparison1 = new BinaryCondition(firstColumn, new Constant("Garcia"), BinaryCondition.ConditionKind.NotEqual);
      BinaryCondition comparison2 = new BinaryCondition(idColumn, new Constant(5), BinaryCondition.ConditionKind.GreaterThan);
      BinaryCondition comparison3 = new BinaryCondition(idColumn, new Constant(6), BinaryCondition.ConditionKind.GreaterThanOrEqual);
      BinaryCondition comparison4 = new BinaryCondition(idColumn, new Constant(7), BinaryCondition.ConditionKind.LessThan);
      BinaryCondition comparison5 = new BinaryCondition(idColumn, new Constant(6), BinaryCondition.ConditionKind.LessThanOrEqual);
      BinaryCondition comparison6 = new BinaryCondition(idColumn, new Constant(6), BinaryCondition.ConditionKind.Equal);
      ICriterion expected =
          new ComplexCriterion (
              new ComplexCriterion (
                  new ComplexCriterion (
                      new ComplexCriterion (
                          new ComplexCriterion (comparison1, comparison2, ComplexCriterion.JunctionKind.And),
                          comparison3, ComplexCriterion.JunctionKind.And),
                      comparison4, ComplexCriterion.JunctionKind.And),
                  comparison5, ComplexCriterion.JunctionKind.And),
              comparison6, ComplexCriterion.JunctionKind.And);
      Assert.AreEqual (expected, result);
    }
    
    [Test]
    public void Simplify_True()
    {
      IQueryable<Student> query = WhereTestQueryGenerator.CreateWhereQueryWithEvaluatableSubExpression (ExpressionHelper.CreateQuerySource ());
      QueryExpression parsedQuery = ExpressionHelper.ParseQuery (query);
      WhereClause whereClause = ClauseFinder.FindClause<WhereClause> (parsedQuery.QueryBody.SelectOrGroupClause);
      WhereConditionParser parser = new WhereConditionParser (parsedQuery, whereClause, _databaseInfo, _context, true);
      Tuple<List<FieldDescriptor>, ICriterion> parseResult = parser.GetParseResult ();
      ICriterion result = parseResult.B;
      Assert.AreEqual (new BinaryCondition(new Column(new Table("studentTable", "s"), "LastColumn"), new Constant("Garcia"),
          BinaryCondition.ConditionKind.Equal), result);
    }

    [Test]
    [ExpectedException (typeof (ParserException), ExpectedMessage = "Expected and, or, or comparison expression for binary expression in where "
        + "condition, found ExpressionType (Add).")]
    public void Simplify_False ()
    {
      IQueryable<Student> query = WhereTestQueryGenerator.CreateWhereQueryWithEvaluatableSubExpression (ExpressionHelper.CreateQuerySource ());
      QueryExpression parsedQuery = ExpressionHelper.ParseQuery (query);
      WhereClause whereClause = ClauseFinder.FindClause<WhereClause> (parsedQuery.QueryBody.SelectOrGroupClause);
      WhereConditionParser parser = new WhereConditionParser (parsedQuery, whereClause, _databaseInfo, _context, false);
      parser.GetParseResult ();
    }

    [Test]
    public void MakeMemberFromParameter()
    {
      // TODO: Simplify this test when ParseParameterExpression can be tested separately
      //where s = s2;  -> (s.ID = s2.ID)
      //ParameterExpression parameter1 = Expression.Parameter (typeof (Student), "s");
      //ParameterExpression parameter2 = Expression.Parameter (typeof (Student), "s2");

      AdditionalFromClause additionalFromClause = new AdditionalFromClause (_fromClause, Expression.Parameter (typeof (Student), "s2"),
          Expression.Lambda (Expression.Constant (null, typeof (IQueryable<Student>))), ExpressionHelper.CreateLambdaExpression());
      _queryExpression.QueryBody.Add (additionalFromClause);

      ParameterExpression parameter1 = _queryExpression.MainFromClause.Identifier;
      ParameterExpression parameter2 = additionalFromClause.Identifier;

      Expression condition = Expression.Equal (parameter1, parameter2);
      Tuple<List<FieldDescriptor>, ICriterion> parseResult = CreateAndParseWhereClause (condition);

      ICriterion result = parseResult.B;

      PropertyInfo member = typeof (Student).GetProperty ("ID");
      Expression expectedMemberAccess1 = Expression.MakeMemberAccess (parameter1, member);
      Expression expectedMemberAccess2 = Expression.MakeMemberAccess (parameter2, member);
      Expression expectedCondition = Expression.Equal (expectedMemberAccess1, expectedMemberAccess2);
      ICriterion expectedResult = (CreateAndParseWhereClause (expectedCondition)).B;
      Assert.AreEqual (expectedResult,result);
    }

    [Test]
    public void JoinWhereConditions()
    {
      IQueryable<Student_Detail> query = JoinTestQueryGenerator.CreateSimpleImplicitWhereJoin (ExpressionHelper.CreateQuerySource_Detail ());
      
      QueryExpression parsedQuery = ExpressionHelper.ParseQuery (query);
      FromClauseBase fromClause = parsedQuery.MainFromClause;
      WhereClause whereClause = ClauseFinder.FindClause<WhereClause> (parsedQuery.QueryBody.SelectOrGroupClause);
      
      PropertyInfo relationMember = typeof (Student_Detail).GetProperty ("Student");
      Table sourceTable = fromClause.GetTable (StubDatabaseInfo.Instance); // Student_Detail
      Table relatedTable = DatabaseInfoUtility.GetRelatedTable (StubDatabaseInfo.Instance, relationMember); // Student
      Tuple<string, string> columns = DatabaseInfoUtility.GetJoinColumnNames (StubDatabaseInfo.Instance, relationMember);
      
      PropertyInfo member = typeof (Student).GetProperty ("First");
      Column? column = DatabaseInfoUtility.GetColumn (StubDatabaseInfo.Instance, relatedTable, member);

      SingleJoin join = new SingleJoin (new Column (sourceTable, columns.A), new Column (relatedTable, columns.B));
      FieldSourcePath path = new FieldSourcePath(sourceTable,new[] {join});
      FieldDescriptor fieldDescriptor = new FieldDescriptor (member, fromClause, path, column);

      WhereConditionParser parser = new WhereConditionParser (parsedQuery, whereClause, _databaseInfo, _context, false);
      Tuple<List<FieldDescriptor>, ICriterion> parseResult = parser.GetParseResult ();
      Assert.AreEqual (fieldDescriptor, parseResult.A[0]);
    }

    [Test]
    public void ParserUsesContext ()
    {
      Assert.AreEqual (0, _context.Count);
      JoinWhereConditions ();
      Assert.AreEqual (1, _context.Count);
    }

    [Test]
    public void RelationMember ()
    {
      IQueryable<Student_Detail> query = WhereTestQueryGenerator.CreateRelationMemberWhereQuery (ExpressionHelper.CreateQuerySource_Detail ());

      QueryExpression parsedQuery = ExpressionHelper.ParseQuery (query);
      WhereClause whereClause = ClauseFinder.FindClause<WhereClause> (parsedQuery.QueryBody.SelectOrGroupClause);

      PropertyInfo relationMember = typeof (Student_Detail).GetProperty ("IndustrialSector");
      FieldDescriptor expected = ExpressionHelper.CreateFieldDescriptor (parsedQuery.MainFromClause, relationMember);

      WhereConditionParser parser = new WhereConditionParser (parsedQuery, whereClause, _databaseInfo, _context, false);
      Tuple<List<FieldDescriptor>, ICriterion> parseResult = parser.GetParseResult ();
      Assert.AreEqual (expected, parseResult.A[0]);
    }

    private Tuple<List<FieldDescriptor>, ICriterion> CreateAndParseWhereClause (Expression whereCondition)
    {
      WhereClause whereClause = CreateWhereClause(whereCondition);

      WhereConditionParser parser = new WhereConditionParser (_queryExpression, whereClause, _databaseInfo, _context, false);
      return parser.GetParseResult ();
    }

    private WhereClause CreateWhereClause (Expression whereCondition)
    {
      return new WhereClause (_fromClause,
          Expression.Lambda (
              typeof (System.Func<Student, bool>),
              whereCondition,
              _parameter));
    }
  }
}