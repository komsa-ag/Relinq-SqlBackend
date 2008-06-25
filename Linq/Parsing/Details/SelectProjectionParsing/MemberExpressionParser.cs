using System.Collections.Generic;
using System.Linq.Expressions;
using Remotion.Data.Linq.DataObjectModel;
using Remotion.Data.Linq.Parsing.FieldResolving;
using Remotion.Utilities;

namespace Remotion.Data.Linq.Parsing.Details.SelectProjectionParsing
{
  public class MemberExpressionParser : ISelectProjectionParser
  {
    // member expression parsing is the same for where conditions and select projections, so delegate to that implementation
    private readonly WhereConditionParsing.MemberExpressionParser _innerParser;

    public MemberExpressionParser (QueryModel queryModel, ClauseFieldResolver resolver)
    {
      ArgumentUtility.CheckNotNull ("queryModel", queryModel);
      ArgumentUtility.CheckNotNull ("resolver", resolver);
      _innerParser = new WhereConditionParsing.MemberExpressionParser (queryModel, resolver);
    }

    public virtual List<IEvaluation> Parse (MemberExpression memberExpression, ParseContext parseContext)
    {
      ArgumentUtility.CheckNotNull ("memberExpression", memberExpression);
      ArgumentUtility.CheckNotNull ("parseContext", parseContext);
      return new List<IEvaluation> { _innerParser.Parse (memberExpression, parseContext) };
    }

    List<IEvaluation> ISelectProjectionParser.Parse (Expression expression, ParseContext parseContext)
    {
      ArgumentUtility.CheckNotNull ("expression", expression);
      ArgumentUtility.CheckNotNull ("parseContext", parseContext);
      return Parse ((MemberExpression) expression, parseContext);
    }

    public bool CanParse(Expression expression)
    {
      ArgumentUtility.CheckNotNull ("expression", expression);
      return expression is MemberExpression;
    }
  }
}