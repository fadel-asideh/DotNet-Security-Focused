using DotNetSecurityFocused.Guardrails;

namespace DotNetSecurityFocused.Tests.Tests;

public class CodeGuardrailTests
{
    private readonly ICodeGuardrail _guardrail = new CodeGuardrail();

    [Fact]
    public void Scan_WithMd5Usage_FlagsViolation()
    {
        var result = _guardrail.Scan("var hash = MD5.Create();");

        Assert.False(result.IsAllowed);
        Assert.Contains(result.Violations, v => v.Reason.Contains("MD5"));
    }

    [Fact]
    public void Scan_WithThreadAbort_FlagsViolation()
    {
        var result = _guardrail.Scan("workerThread.Abort();");

        Assert.False(result.IsAllowed);
        Assert.Contains(result.Violations, v => v.Reason.Contains("Thread.Abort"));
    }

    [Fact]
    public void Scan_WithBinaryFormatter_FlagsViolation()
    {
        var result = _guardrail.Scan("var formatter = new BinaryFormatter();");

        Assert.False(result.IsAllowed);
        Assert.Contains(result.Violations, v => v.Reason.Contains("insecure-deserialization"));
    }

    [Fact]
    public void Scan_WithHardcodedPassword_FlagsViolation()
    {
        var result = _guardrail.Scan("var password = \"hunter2\";");

        Assert.False(result.IsAllowed);
        Assert.Contains(result.Violations, v => v.Reason.Contains("hardcoded"));
    }

    [Fact]
    public void Scan_WithSafeCode_ReturnsNoViolations()
    {
        var result = _guardrail.Scan("using var sha256 = SHA256.Create(); var token = _configuration[\"Jwt:SecretKey\"];");

        Assert.True(result.IsAllowed);
        Assert.Empty(result.Violations);
    }
}