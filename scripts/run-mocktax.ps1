param(
    [string]$Client = "Margaret Buttle",
    [int]$Year = 2025,
    [string]$ReadonlyField = ""
)

if ($ReadonlyField) {
    dotnet run --project src/MockTax -- "$Client" $Year --readonly $ReadonlyField
}
else {
    dotnet run --project src/MockTax -- "$Client" $Year
}
