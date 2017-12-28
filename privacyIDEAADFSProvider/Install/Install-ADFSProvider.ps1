# Install the provider

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

Set-location "C:\Program and Files\privacyIDEAProvider"
Gac-Util "C:\Program and Files\privacyIDEAProvider\privacyIDEA-ADFSProvider.dll"

$typeName = "privacyIDEAADFSProvider.Adapter, privacyIDEA-ADFSProvider, Version=1.1.0.0, Culture=neutral, PublicKeyToken=b6483f285cb7b6eb"
Register-AdfsAuthenticationProvider -TypeName $typeName -Name "privacyIDEA-ADFSProvider" -ConfigurationFilePath "C:\Program and Files\privacyIDEAProvider\config.xml" -Verbose
