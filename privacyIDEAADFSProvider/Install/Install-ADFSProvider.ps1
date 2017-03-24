# Install the provider

Set-location "C:\Program and Files\privacyIDEAProvider\"
[System.Reflection.Assembly]::Load("System.EnterpriseServices, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")
$publish = New-Object System.EnterpriseServices.Internal.Publish
$publish.GacInstall("C:\Program and Files\privacyIDEAProvider\privacyIDEA-ADFSProvider.dll")

$typeName = "privacyIDEAADFSProvider.Adapter, privacyIDEAADFSProvider, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b6483f285cb7b6eb"
Register-AdfsAuthenticationProvider -TypeName $typeName -Name "privacyIDEA-ADFSProvider" -ConfigurationFilePath "C:\Program and Files\privacyIDEAProvider\config.xml" -Verbose