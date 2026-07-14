# Test URLs với follow-redirect
$ErrorActionPreference = 'Stop'

$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$loginPage = Invoke-WebRequest -Uri 'http://localhost:5121/Account/Login' -WebSession $session -UseBasicParsing
$token = ($loginPage.Content | Select-String -Pattern 'name="__RequestVerificationToken".*?value="([^"]+)"' -AllMatches).Matches[0].Groups[1].Value
Write-Host "Token: $($token.Substring(0,30))..."

$loginBody = @{
    '__RequestVerificationToken' = $token
    'UsernameOrEmail' = 'admin'
    'Password' = 'Admin@123'
    'RememberMe' = 'false'
}
$loginResp = Invoke-WebRequest -Uri 'http://localhost:5121/Account/Login' -Method Post -WebSession $session -Body $loginBody -UseBasicParsing
Write-Host "Login final URL: $($loginResp.BaseResponse.ResponseUri.AbsolutePath)"
Write-Host "Login status: $($loginResp.StatusCode)"

Write-Host "Cookies:"
foreach ($c in $session.Cookies.GetCookies('http://localhost:5121')) {
    Write-Host "  $($c.Name)"
}

$urls = @(
    '/Admin/Dashboard',
    '/Admin/Report/Dashboard',
    '/Admin/Report/Revenue',
    '/Admin/Report/Inventory',
    '/Admin/User',
    '/Admin/Voucher',
    '/Admin/Category',
    '/Admin/SystemConfig',
    '/Admin/Notification',
    '/Staff/Dashboard',
    '/Staff/Order',
    '/Staff/Product',
    '/Staff/Shipment',
    '/Staff/Voucher',
    '/Staff/Notification'
)

Write-Host "`n--- URL test ---"
foreach ($u in $urls) {
    try {
        $r = Invoke-WebRequest -Uri "http://localhost:5121$u" -WebSession $session -UseBasicParsing -MaximumRedirection 0 -ErrorAction SilentlyContinue
        $code = $r.StatusCode
        $color = if ($code -eq 200) { 'Green' } elseif ($code -eq 302) { 'Yellow' } else { 'Red' }
        Write-Host ("{0,-3} {1,-30}" -f $code, $u) -ForegroundColor $color
    } catch {
        $resp = $_.Exception.Response
        $code = if ($resp) { [int]$resp.StatusCode } else { 0 }
        $loc = if ($resp) { $resp.Headers['Location'] } else { '' }
        $color = if ($code -eq 302 -and $loc -like '*Account/Login*') { 'Yellow' } elseif ($code -eq 302) { 'Cyan' } else { 'Red' }
        Write-Host ("{0,-3} {1,-30} -> {2}" -f $code, $u, $loc) -ForegroundColor $color
    }
}