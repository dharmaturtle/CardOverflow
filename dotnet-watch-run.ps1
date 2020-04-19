Start-Process -NoNewWindow pwsh.exe -ArgumentList "-command", "dotnet watch --project CardOverflow.UserContentApi    run"
Start-Process -NoNewWindow pwsh.exe -ArgumentList "-command", "dotnet watch --project CardOverflow.Server            run"
Start-Process -NoNewWindow pwsh.exe -ArgumentList "-command", "dotnet watch --project ThoughtDesign.IdentityProvider run"
Start "https://localhost:44315/"