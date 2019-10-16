<#
    .SYNOPSIS
        Set all installed instances of SQL server to dynamic ports
#>
Get-ChildItem -Path 'HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\' |
    Where-Object {
        $_.Name -imatch 'MSSQL[_\d]+\.SQL.*'
    } |
    ForEach-Object {

        Write-Host "Setting $((Get-ItemProperty $_.PSPath).'(default)') to dynamic ports"
        Set-ItemProperty (Join-Path $_.PSPath 'mssqlserver\supersocketnetlib\tcp\ipall') -Name TcpDynamicPorts -Value '0'
        Set-ItemProperty (Join-Path $_.PSPath 'mssqlserver\supersocketnetlib\tcp\ipall') -Name TcpPort -Value ([string]::Empty)
    }