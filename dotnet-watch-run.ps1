$sp = "split-pane -p `"Command Prompt`""
$d = Get-Location
$_1 = "-d $d dotnet watch --project CardOverflow.Server            run;"
$_2 = "-d $d dotnet watch --project ThoughtDesign.IdentityProvider run;"
$_3 = "-d $d dotnet watch --project CardOverflow.UserContentApi    run"

Start-Process -NoNewWindow wt -ArgumentList "$_1 $sp $_2 $sp $_3" # requires Windows Terminal

Start "https://localhost:44315/"