using System.Text.RegularExpressions;

namespace DotNetSecurityFocused.Guardrails;

public interface ICodeGuardrail
{
    GuardrailResult Scan(string code);
}

// Regex-based pattern matching, not a type-aware syntax analysis — cheap to run, but can
// false-positive (e.g. HttpWebRequest.Abort() is legitimate, unlike Thread.Abort()). A more
// rigorous version would parse the code with Roslyn and check actual types.
public class CodeGuardrail : ICodeGuardrail
{
    private static readonly (Regex Pattern, string Reason)[] Rules =
    [
        (new Regex(@"\bMD5\b"),
            "MD5 is cryptographically broken. Use SHA-256 for hashing, or a dedicated password hasher (PBKDF2/Argon2/bcrypt) for credentials."),
        (new Regex(@"\b(DES|TripleDES|3DES)\b"),
            "DES/TripleDES use insufficient key lengths for modern security requirements. Use AES instead."),
        (new Regex(@"\bRC4\b"),
            "RC4 has known cryptographic weaknesses and should not be used."),
        (new Regex(@"\bSHA1\b"),
            "SHA-1 is deprecated for security-sensitive use due to collision attacks. Use SHA-256 or higher."),
        (new Regex(@"\.Abort\s*\("),
            "Thread.Abort can corrupt shared state and is unsupported on modern .NET; use cooperative cancellation (CancellationToken) instead."),
        (new Regex(@"\bBinaryFormatter\b"),
            "BinaryFormatter is a known insecure-deserialization vector and is obsoleted by Microsoft; use System.Text.Json or another safe serializer."),
        (new Regex(@"(?i)\b(password|secret|apikey|api_key|connectionstring)\b\s*=\s*""[^""]+"""),
            "Possible hardcoded secret/credential literal. Secrets must come from configuration/User Secrets/environment variables, never source code.")
    ];
    public GuardrailResult Scan(string code)
    {
        var violations = new List<GuardrailViolation>();

        foreach (var (pattern, reason) in Rules)
        {
            if (pattern.IsMatch(code))
                violations.Add(new GuardrailViolation { Pattern = pattern.ToString(), Reason = reason });
        }

        return new GuardrailResult { Violations = violations };
    }
}