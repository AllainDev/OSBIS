Add-Type -AssemblyName "System.Data.SqlClient"

$connStr = "Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=master;Integrated Security=SSPI"
$cn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$cn.Open()

# Force drop the OSBISDB database
$cmd = $cn.CreateCommand()
$cmd.CommandText = "IF EXISTS (SELECT * FROM sys.databases WHERE name = 'OSBISDB') BEGIN ALTER DATABASE [OSBISDB] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [OSBISDB]; END"
$cmd.ExecuteNonQuery()
Write-Host "Dropped OSBISDB (if existed)"

$cn.Close()
