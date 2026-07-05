using DotNetSecurityFocused.Data;
using DotNetSecurityFocused.Models;
using Microsoft.EntityFrameworkCore;


namespace DotNetSecurityFocused.Services;

// Demonstrates SQL injection in EF Core: a vulnerable query built via string
// interpolation next to two safe equivalents. Only the safe methods are ever
// wired to a controller — SearchByNameVulnerableAsync exists purely as a
// reference example, invoked directly from tests.
//
// Why the vulnerable version breaks: $"..." is plain C# string interpolation,
// evaluated before FromSqlRaw ever sees it. The caller's input is baked
// directly into the SQL text, so quotes/operators in the input become part
// of the command grammar the database parses — e.g. a name containing
// `' OR '1'='1' -- ` closes the intended string literal early and appends a
// clause that's always true, turning a per-row filter into "return everything".
//
// Why the safe versions don't: LINQ's Where(...) never produces SQL text in
// application code at all — EF Core translates the expression tree into a
// parameterized command. FromSqlInterpolated looks similar to string
// interpolation but isn't: it takes a FormattableString, so the compiler
// keeps the literal text and the {name} value separate instead of merging
// them into one string. Either way, the database parses the command's
// structure once, with a placeholder where the value goes; the value is
// bound afterward and can never alter that already-parsed structure, no
// matter what characters it contains.
//
// (Separately: escaping LIKE wildcards % and _ in the input is a correctness
// concern, not a security one — out of scope here.)
public class ProductSearchService
{
    private readonly AppDBContext _context;

    public ProductSearchService(AppDBContext appDBContext)
    {
        _context = appDBContext;
    }

    // VULNERABLE - reference example only. Never called from a controller.
    public async Task<List<Product>> SearchByNameVulnerableAsync(string name)
    {
        return await _context.Products
            .FromSqlRaw($"SELECT * FROM Products WHERE Name LIKE '%{name}%'")
            .ToListAsync();
    }

    // SAFE - preferred approach, plain LINQ
    public async Task<List<Product>> SearchByNameSafeAsync(string name)
    {
        return await _context.Products.Where(p => p.Name.Contains(name)).ToListAsync();
    }

    // SAFE - alternative when raw SQL is unavoidable
    public async Task<List<Product>> SearchByNameSafeRawAsync(string name)
    {
        return await _context.Products
            .FromSqlInterpolated($"SELECT * FROM Products WHERE Name LIKE '%' || {name} || '%'")
            .ToListAsync();
    }

}