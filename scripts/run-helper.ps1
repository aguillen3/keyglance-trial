param(
    [switch]$Once
)

$argsList = @("--server", "http://localhost:5080")
if ($Once) { $argsList += "--once" }

dotnet run --project src/KeyGlance.Helper -- @argsList
