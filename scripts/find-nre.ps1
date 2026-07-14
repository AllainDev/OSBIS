Select-String -Path 'C:\Users\Admin\source\repos\OSBIS\Logs\osbis-20260713.log' -Pattern 'NullReferenceException' -SimpleMatch | Select-Object -Last 30 | ForEach-Object { Write-Host ($_.LineNumber.ToString() + ': ' + $_.Line) }
Write-Host "---"
Write-Host "Done"