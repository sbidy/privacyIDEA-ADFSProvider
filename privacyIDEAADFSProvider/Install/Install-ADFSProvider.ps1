#Requires -RunAsAdministrator
# Install the provider

$myPath = Get-Location
$myDll = 'privacyIDEA-ADFSProvider.dll'
$myDllFullName = (get-item $myDll).FullName

function Gac-Util
{
    param (
        [parameter(Mandatory = $true)][string] $assembly
    )
    try
    {
        $Error.Clear()

        [Reflection.Assembly]::LoadWithPartialName("System.EnterpriseServices") | Out-Null
        [System.EnterpriseServices.Internal.Publish] $publish = New-Object System.EnterpriseServices.Internal.Publish

        if (!(Test-Path $assembly -type Leaf) ) 
            { throw "The assembly $assembly does not exist" }

        if ([System.Reflection.Assembly]::LoadFile($assembly).GetName().GetPublicKey().Length -eq 0 ) 
            { throw "The assembly $assembly must be strongly signed" }

        $publish.GacInstall($assembly)

        Write-Host "`t`t$($MyInvocation.InvocationName): Assembly $assembly gacced"
    }
    catch
    {
        Write-Host "`t`t$($MyInvocation.InvocationName): $_"
    }
}

# check event source
if (!([System.Diagnostics.EventLog]::SourceExists("privacyIDEAProvider")))
{
    New-EventLog -LogName "AD FS/Admin" -Source "privacyIDEAProvider"
    Write-Host "Log source created"
}

Gac-Util $myDllFullName

$appFullName = ([system.reflection.assembly]::loadfile($myDllFullName)).FullName

$typeName = "privacyIDEAADFSProvider.Adapter, "+$appFullName

Register-AdfsAuthenticationProvider -TypeName $typeName -Name "privacyIDEA-ADFSProvider" -ConfigurationFilePath $myPath"\config.xml" -Verbose

Restart-Service adfssrv