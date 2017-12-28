# Remove the provider

Unregister-AdfsAuthenticationProvider -Name "privacyIDEA-ADFSProvider"
Set-location "C:\Program Files\privacyIDEAProvider\"
[System.Reflection.Assembly]::Load("System.EnterpriseServices, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")
$publish = New-Object System.EnterpriseServices.Internal.Publish
$publish.GacRemove("C:\Program Files\privacyIDEAProvider\privacyIDEA-ADFSProvider.dll")

Restart-Service adfssrv
