Add-Type -AssemblyName System.Data.SqlClient

$connStr = "Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=OSBISDB;Integrated Security=SSPI"
$cn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$cn.Open()

# Check users
$cmd = $cn.CreateCommand()
$cmd.CommandText = "SELECT Username, Email, IsActive, FailedLoginCount, LockoutEnd, LEN(PasswordHash) AS HashLen FROM [User]"
$rdr = $cmd.ExecuteReader()
Write-Host "=== USERS ==="
while ($rdr.Read()) {
    $u = $rdr["Username"]
    $e = $rdr["Email"]
    $a = $rdr["IsActive"]
    $f = $rdr["FailedLoginCount"]
    $l = $rdr["LockoutEnd"]
    $h = $rdr["HashLen"]
    Write-Host "user=$u email=$e active=$a failCount=$f lockout=$l hashLen=$h"
}
$rdr.Close()

# Check categories
$cmd2 = $cn.CreateCommand()
$cmd2.CommandText = "SELECT COUNT(*) AS CatCount FROM Category"
$catCount = $cmd2.ExecuteScalar()
Write-Host "=== CATEGORIES === categoryCount=$catCount"

# Check products
$cmd3 = $cn.CreateCommand()
$cmd3.CommandText = "SELECT COUNT(*) AS ProdCount FROM Product"
$prodCount = $cmd3.ExecuteScalar()
Write-Host "=== PRODUCTS === productCount=$prodCount"

# Check tables
$cmd4 = $cn.CreateCommand()
$cmd4.CommandText = "SELECT name FROM sys.tables ORDER BY name"
$rdr4 = $cmd4.ExecuteReader()
Write-Host "=== TABLES ==="
while ($rdr4.Read()) {
    Write-Host "  $($rdr4['name'])"
}
$rdr4.Close()

$cn.Close()
