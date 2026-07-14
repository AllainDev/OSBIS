# Update Admin views: Layout = "_LayoutAdmin" -> "_LayoutAdminLTE"
$adminFiles = Get-ChildItem -Path 'Views\Admin' -Recurse -Filter '*.cshtml'
foreach ($f in $adminFiles) {
    $content = Get-Content $f.FullName -Raw
    if ($content -match 'Layout\s*=\s*"_LayoutAdmin"') {
        $newContent = $content -replace 'Layout\s*=\s*"_LayoutAdmin"', 'Layout = "_LayoutAdminLTE"'
        Set-Content -Path $f.FullName -Value $newContent -NoNewline
        Write-Host "[Admin] Updated: $($f.FullName)"
    }
}

# Update Staff views: Layout = "_LayoutAdmin" -> "_LayoutStaff"
$staffFiles = Get-ChildItem -Path 'Views\Staff' -Recurse -Filter '*.cshtml'
foreach ($f in $staffFiles) {
    $content = Get-Content $f.FullName -Raw
    if ($content -match 'Layout\s*=\s*"_LayoutAdmin"') {
        $newContent = $content -replace 'Layout\s*=\s*"_LayoutAdmin"', 'Layout = "_LayoutStaff"'
        Set-Content -Path $f.FullName -Value $newContent -NoNewline
        Write-Host "[Staff] Updated: $($f.FullName)"
    }
}

Write-Host "Done."