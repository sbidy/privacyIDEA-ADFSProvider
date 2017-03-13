# Install the provider

Set-location "C:\Windows\System32\privacyIDEAprovider"
[System.Reflection.Assembly]::Load("System.EnterpriseServices, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")
$publish = New-Object System.EnterpriseServices.Internal.Publish
$publish.GacInstall("C:\Windows\System32\privacyIDEAprovider\privacyIDEAADFSProvider.dll")

$typeName = "privacyIDEAADFSProvider.Adapter, privacyIDEAADFSProvider, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b6483f285cb7b6eb"
Register-AdfsAuthenticationProvider -TypeName $typeName -Name "privacyIDEAADFSProvider" -ConfigurationFilePath "C:\Windows\System32\privacyIDEAprovider\config.xml" -Verbose

# Remove the provider

Unregister-AdfsAuthenticationProvider -Name "privacyIDEAADFSProvider"
Set-location "C:\Windows\System32\privacyIDEAprovider"
[System.Reflection.Assembly]::Load("System.EnterpriseServices, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")
$publish = New-Object System.EnterpriseServices.Internal.Publish
$publish.GacRemove("C:\Windows\System32\privacyIDEAprovider\privacyIDEAADFSProvider.dll")

Restart-Service adfssrv