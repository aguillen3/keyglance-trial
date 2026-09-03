$base = "http://localhost:5080"

$jobs = @(
    @{
        id = "demo-late"
        client = "Margaret Buttle"
        year = 2025
        dueDate = (Get-Date).ToUniversalTime().AddHours(2).ToString("o")
        fields = @{ Box2 = "200"; Box14 = "300"; Box22 = "400" }
    },
    @{
        id = "demo-soon"
        client = "Margaret Buttle"
        year = 2025
        dueDate = (Get-Date).ToUniversalTime().AddMinutes(1).ToString("o")
        fields = @{ Box2 = "111"; Box14 = "222"; Box22 = "333" }
    }
)

foreach ($job in $jobs) {
    $json = $job | ConvertTo-Json -Depth 5
    Invoke-RestMethod -Method Post -Uri "$base/jobs" -ContentType "application/json" -Body $json
}

Write-Host "Demo jobs added."
