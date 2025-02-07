using System;
using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace Gameboard.Api.Common;

public abstract class Visitor
{
    private readonly string _prefix;
    private readonly Expression _node;

    protected Visitor(Expression node) => _node = node;
    protected Visitor(Expression node, string logPrefix) => (_node, _prefix) = (node, logPrefix);

    public abstract void Visit();

    public ExpressionType NodeType => _node.NodeType;
    public static Visitor CreateFromExpression(Expression node)
    {
        return node.NodeType switch
        {
            ExpressionType.Constant => new ConstantVisitor((ConstantExpression)node),
            ExpressionType.Lambda => new LambdaVisitor((LambdaExpression)node),
            ExpressionType.Parameter => new ParameterVisitor((ParameterExpression)node),
            ExpressionType.NotEqual => new BinaryVisitor((BinaryExpression)node),
            ExpressionType.MemberAccess => new MemberVisitor((MemberExpression)node),
            _ => throw new NotImplementedException($"Node not processed yet: {node.NodeType}")
        };
    }

    protected void Log(string message) => System.Diagnostics.Debug.WriteLine($"{(string.IsNullOrWhiteSpace(_prefix) ? string.Empty : _prefix)}{message}");
}

public class LambdaVisitor : Visitor
{
    public static ReadOnlyCollection<ParameterExpression> Parameters;
    private readonly LambdaExpression _node;

    public LambdaVisitor(LambdaExpression node) : base(node) => _node = node;
    public LambdaVisitor(LambdaExpression node, string logPrefix) : base(node, logPrefix) { }

    public override void Visit()
    {
        Log($"This expression is a {NodeType} expression type");
        Log($"The name of the lambda is {_node.Name ?? "<null>"}");
        Log($"The return type is {_node.ReturnType}");
        Log($"The expression has {_node.Parameters.Count} argument(s).");

        // Visit each parameter:
        Parameters = _node.Parameters;
        foreach (var argumentExpression in _node.Parameters)
        {
            var argumentVisitor = CreateFromExpression(argumentExpression);
            argumentVisitor.Visit();
        }

        // Visit the body:
        var bodyVisitor = CreateFromExpression(_node.Body);
        bodyVisitor.Visit();
    }
}

public class BinaryVisitor(BinaryExpression node) : Visitor(node)
{
    private readonly BinaryExpression _node = node;

    public override void Visit()
    {
        Log($"This binary expression is a {NodeType} expression");
        var left = CreateFromExpression(_node.Left);
        Log($"The Left argument is:");
        left.Visit();
        var right = CreateFromExpression(_node.Right);
        Log($"The Right argument is:");
        right.Visit();
    }
}

public class ParameterVisitor : Visitor
{
    private readonly ParameterExpression node;
    public ParameterVisitor(ParameterExpression node) : base(node)
    {
        this.node = node;
    }

    public override void Visit()
    {
        Log($"This is an {NodeType} expression type");
        Log($"Type: {node.Type}, Name: {node.Name}, ByRef: {node.IsByRef}");
    }
}

public class ConstantVisitor : Visitor
{
    private readonly ConstantExpression node;
    public ConstantVisitor(ConstantExpression node) : base(node) => this.node = node;

    public override void Visit()
    {
        Log($"This is an {NodeType} expression type");
        Log($"The type of the constant value is {node.Type}");
        Log($"The value of the constant value is {node.Value}");
    }
}

public class MemberVisitor : Visitor
{
    public readonly MemberExpression _node;
    public MemberVisitor(MemberExpression node) : base(node) => _node = node;

    public override void Visit()
    {
        Log($"Node type: {NodeType} expression");
        Log($"The member is {_node.Member.MemberType} {_node.Member.Name}");
        Log($"Its value is {GetValue(_node)}");
    }

    private object GetValue(MemberExpression member)
    {
        var playerExpression = Expression.Convert(LambdaVisitor.Parameters[0], typeof(object));
        var playerGetterLambda = Expression.Lambda<Func<object>>(playerExpression);
        var playerGetter = playerGetterLambda.Compile();

        var objectMember = Expression.Convert(member, typeof(object));
        var getterLambda = Expression.Lambda<Func<Player, object>>(objectMember, LambdaVisitor.Parameters[0]);
        var getter = getterLambda.Compile();
        return getter(playerGetter() as Player);
    }
}
