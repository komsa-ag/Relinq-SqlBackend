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
using System.Linq.Expressions;
using Remotion.Utilities;

namespace Remotion.Linq.SqlBackend.SqlStatementModel.SqlSpecificExpressions
{
  /// <summary>
  /// Represents a SQL "a OPERATOR b" expression.
  /// </summary>
  public class SqlBinaryOperatorExpression : Expression
  {
    private readonly Type _type;
    private readonly string _binaryOperator;
    private readonly Expression _leftExpression;
    private readonly Expression _rightExpression;

    public SqlBinaryOperatorExpression(Type type, string binaryOperator, Expression leftExpression, Expression rightExpression)
    {
      ArgumentUtility.CheckNotNull(nameof(type), type);
      ArgumentUtility.CheckNotNull(nameof(binaryOperator), binaryOperator);
      ArgumentUtility.CheckNotNull(nameof(leftExpression), leftExpression);
      ArgumentUtility.CheckNotNull(nameof(rightExpression), rightExpression);
      _type = type;
      _binaryOperator = binaryOperator;
      _leftExpression = leftExpression;
      _rightExpression = rightExpression;
    }

    public override ExpressionType NodeType
    {
      get { return ExpressionType.Extension; }
    }

    public override Type Type
    {
      get { return _type; }
    }

    public Expression LeftExpression
    {
      get { return _leftExpression; }
    }

    public Expression RightExpression
    {
      get { return _rightExpression; }
    }

    public string BinaryOperator
    {
      get { return _binaryOperator; }
    }

    protected override Expression VisitChildren(ExpressionVisitor visitor)
    {
      var newLeftExpression = visitor.Visit(_leftExpression);
      var newRightExpression = visitor.Visit(_rightExpression);

      if (newLeftExpression != _leftExpression || newRightExpression != _rightExpression)
        return new SqlBinaryOperatorExpression(typeof(bool), _binaryOperator, newLeftExpression, newRightExpression);
      else
        return this;
    }

    protected override Expression Accept(ExpressionVisitor visitor)
    {
      var specificVisitor = visitor as ISqlBinaryOperatorExpressionVisitor;
      if (specificVisitor != null)
        return specificVisitor.VisitSqlBinaryOperator(this);
      else
        return base.Accept(visitor);
    }

    public override string ToString()
    {
      return string.Format("{0} {1} {2}", _leftExpression, _binaryOperator, _rightExpression);
    }
  }
}