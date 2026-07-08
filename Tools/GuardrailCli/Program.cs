using DotNetSecurityFocused.Guardrails;

var guardrail = new CodeGuardrail();
var hadViolations = false;

foreach (var filePath in args)
{
    if (!File.Exists(filePath)) continue;

    var code = File.ReadAllText(filePath);
    var result = guardrail.Scan(code);

    if (!result.IsAllowed)
    {
        hadViolations = true;
        Console.WriteLine($"Guardrail violations in {filePath}:");
        foreach (var violation in result.Violations)
            Console.WriteLine($"  - {violation.Reason}");
    }
}

return hadViolations ? 1 : 0;