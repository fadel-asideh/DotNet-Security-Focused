namespace DotNetSecurityFocused.Guardrails;

public class GuardrailViolation
{
    public string Pattern { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
}

public class GuardrailResult
{
    public IReadOnlyList<GuardrailViolation> Violations { get; init; } = [];
    public bool IsAllowed => Violations.Count == 0;
}