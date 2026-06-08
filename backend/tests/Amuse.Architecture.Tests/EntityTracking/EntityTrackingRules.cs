using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Amuse.Architecture.Tests.EntityTracking;

internal static class EntityTrackingRules
{
    internal const string NoTrackingMutationRuleId = "AMUSE001";
    internal const string ProfileServiceUnsafeGetRuleId = "AMUSE003";

    private static readonly HashSet<string> AllowedReadOnlyInstanceMethods = new(StringComparer.Ordinal)
    {
        "Where",
        "Select",
        "SelectMany",
        "Any",
        "All",
        "First",
        "FirstOrDefault",
        "Single",
        "SingleOrDefault",
        "Last",
        "LastOrDefault",
        "Count",
        "LongCount",
        "Contains",
        "OrderBy",
        "OrderByDescending",
        "ThenBy",
        "ThenByDescending",
        "ToList",
        "ToArray",
        "AsEnumerable",
        "Distinct",
        "GroupBy",
        "Skip",
        "Take",
    };

    private static readonly string[] SafeProfileServiceMethodPrefixes =
    [
        "TryGet",
        "GetForRead",
        "GetOrCreate",
        "Ensure",
    ];

    private static readonly string[] SafeReturnTypeTokens =
    [
        "Response",
        "Dto",
        "Row",
        "Summary",
        "Snapshot",
        "Request",
        "bool",
        "int",
        "long",
        "string",
        "Guid",
        "void",
        "IReadOnlyList",
        "IEnumerable",
        "Dictionary",
        "Result",
        "ValueTask",
        "Task",
    ];

    internal static IReadOnlyList<EntityTrackingViolation> AnalyzeFile(string filePath, string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source, path: filePath);
        var root = tree.GetRoot();
        var violations = new List<EntityTrackingViolation>();

        foreach (var type in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
        {
            foreach (var method in type.Members.OfType<MethodDeclarationSyntax>())
            {
                violations.AddRange(AnalyzeNoTrackingMutationsInSaveChangesMethod(filePath, method));
                violations.AddRange(AnalyzeProfileServiceUnsafeGetForMethods(filePath, type, method));
            }
        }

        return violations;
    }

    internal static IReadOnlyList<EntityTrackingViolation> AnalyzeDirectory(
        string directoryPath,
        Func<string, bool>? includeFile = null)
    {
        var violations = new List<EntityTrackingViolation>();

        foreach (var file in Directory.EnumerateFiles(directoryPath, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.EndsWith(".Designer.cs", StringComparison.Ordinal))
            {
                continue;
            }

            if (includeFile is not null && !includeFile(file))
                continue;

            var source = File.ReadAllText(file);
            violations.AddRange(AnalyzeFile(file, source));
        }

        return violations;
    }

    private static IEnumerable<EntityTrackingViolation> AnalyzeNoTrackingMutationsInSaveChangesMethod(
        string filePath,
        MethodDeclarationSyntax method)
    {
        if (!MethodCallsSaveChanges(method))
            yield break;

        var noTrackingVariables = CollectNoTrackingAssignedVariables(method);
        if (noTrackingVariables.Count == 0)
            yield break;

        foreach (var invocation in method.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
                continue;

            if (memberAccess.Expression is not IdentifierNameSyntax receiver)
                continue;

            if (AllowedReadOnlyInstanceMethods.Contains(memberAccess.Name.Identifier.Text))
                continue;

            if (!noTrackingVariables.Contains(receiver.Identifier.Text))
                continue;

            var line = receiver.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            yield return new EntityTrackingViolation(
                NoTrackingMutationRuleId,
                filePath,
                line,
                $"Method '{method.Identifier.Text}' calls SaveChangesAsync but mutates '{receiver.Identifier.Text}' "
                + "loaded via AsNoTracking(). Use a tracked query (e.g. GetFor*ForUpdateAsync) instead.");
        }
    }

    private static IEnumerable<EntityTrackingViolation> AnalyzeProfileServiceUnsafeGetForMethods(
        string filePath,
        TypeDeclarationSyntax type,
        MethodDeclarationSyntax method)
    {
        if (!type.Identifier.Text.EndsWith("ProfileService", StringComparison.Ordinal))
            yield break;

        var methodName = method.Identifier.Text;
        if (!methodName.StartsWith("GetFor", StringComparison.Ordinal))
            yield break;

        if (methodName.Contains("ForRead", StringComparison.Ordinal)
            || methodName.Contains("ForUpdate", StringComparison.Ordinal)
            || methodName.Contains("ForMutation", StringComparison.Ordinal))
        {
            yield break;
        }

        if (SafeProfileServiceMethodPrefixes.Any(prefix =>
                methodName.StartsWith(prefix, StringComparison.Ordinal)))
        {
            yield break;
        }

        if (!ReturnsLikelyDomainEntity(method))
            yield break;

        var line = method.Identifier.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
        yield return new EntityTrackingViolation(
            ProfileServiceUnsafeGetRuleId,
            filePath,
            line,
            $"Profile service method '{type.Identifier.Text}.{methodName}' returns a domain entity via GetFor*. "
            + "Use TryGet*/GetForRead* for read models or *ForUpdate* for tracked mutations.");
    }

    private static bool MethodCallsSaveChanges(MethodDeclarationSyntax method) =>
        method.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Any(invocation =>
                invocation.Expression is MemberAccessExpressionSyntax memberAccess
                && memberAccess.Name.Identifier.Text is "SaveChangesAsync" or "SaveChanges");

    private static HashSet<string> CollectNoTrackingAssignedVariables(MethodDeclarationSyntax method)
    {
        var variables = new HashSet<string>(StringComparer.Ordinal);

        foreach (var declaration in method.DescendantNodes().OfType<VariableDeclarationSyntax>())
        {
            if (!ExpressionContainsAsNoTracking(declaration))
                continue;

            foreach (var variable in declaration.Variables)
            {
                if (variable.Identifier.Text.Length > 0)
                    variables.Add(variable.Identifier.Text);
            }
        }

        foreach (var assignment in method.DescendantNodes().OfType<AssignmentExpressionSyntax>())
        {
            if (!ExpressionContainsAsNoTracking(assignment.Right))
                continue;

            if (assignment.Left is IdentifierNameSyntax identifier)
                variables.Add(identifier.Identifier.Text);
        }

        return variables;
    }

    private static bool ExpressionContainsAsNoTracking(SyntaxNode node) =>
        node.DescendantNodesAndSelf()
            .OfType<MemberAccessExpressionSyntax>()
            .Any(memberAccess => memberAccess.Name.Identifier.Text == "AsNoTracking");

    private static bool ReturnsLikelyDomainEntity(MethodDeclarationSyntax method)
    {
        var unwrapped = UnwrapTaskAndResult(method.ReturnType.ToString());
        if (unwrapped.Contains("Amuse.Domain.", StringComparison.Ordinal))
            return true;

        if (SafeReturnTypeTokens.Any(token => unwrapped.Contains(token, StringComparison.Ordinal)))
            return false;

        return unwrapped.Contains('(') || (unwrapped.Length > 0 && char.IsUpper(unwrapped[0]));
    }

    private static string UnwrapTaskAndResult(string returnType)
    {
        var current = returnType.Trim();

        while (true)
        {
            if (current.StartsWith("Task<", StringComparison.Ordinal) && current.EndsWith('>'))
            {
                current = current["Task<".Length..^1].Trim();
                continue;
            }

            if (current.StartsWith("ValueTask<", StringComparison.Ordinal) && current.EndsWith('>'))
            {
                current = current["ValueTask<".Length..^1].Trim();
                continue;
            }

            if (current.StartsWith("Result<", StringComparison.Ordinal) && current.EndsWith('>'))
            {
                current = current["Result<".Length..^1].Trim();
                continue;
            }

            break;
        }

        return current;
    }
}
