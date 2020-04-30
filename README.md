## [Version 1.3.7 (30.04.2020 // Current)](https://github.com/sbidy/privacyIDEA-ADFSProvider/releases/tag/1.3.7)
  - Add support for more languages and also a dynamic bindings (thanks to @jinshin and @Webserve)
  - "Submit" button also customizable
  - BugFix for #39 - thank you again @Webserve
  - Some other improvements
  
PLEASE update the config.xml! You can use your old config file but you will miss some cool new features! Please migrate!

DON'T use the DEBUG version in your production environment!

## Version 1.3.6
  - Change support for different languages - the provider supports FR, ES, DE, EN (US & GB)
  - The HTML title field can also now changed

## Version 1.3.5
  - Now UPNs can be used as Loginname Attribute. See the UPN-Parameter in the config.ini. Default is SAMaccountName. 

## Version 1.3.4 
  - Bug fixes for #14, #15 and #19 - authContext now used
  - Please use the new PowerShell installer for 1.3.4 !!
  - EventLog implemented for error handling. See in the Applications and Service Logs -> AD FS -> Admin

## Abstract
A Microsoft Active Directory Federation Service (ADFS) provider for the open source authentication system [privacyIDEA](https://www.privacyidea.org/).

In some it-compliance or best practice papers, it is highly recommend adding a second factor on top of the username and password combination to increase the security level.
The implementation of this type of advanced authentication can challenging the it-infrastructure and administrators.
This open source project provides an easy way to connect the authentication components and the on-premises open source privacyIDEA.
You don't need an additional RADIUS-Server or other components to connect these systems. Only this provider has to be registered at the ADFS.
After that, you can use the TOTP, HOTP, SMS or E-Mail authentication method to authenticate the user for products like Microsoft Exchange, Microsoft Dynamics, Office 365 or ohter services.

This ADFSProvider gives you nearly the same capabilities as a cloud based authentication service. However, this provider and the on-prem privacyIDEA authentication system is open source and free. 

## Need help? 
If you have any further questions or you need help for a enterprise implementation please don't hesitate to contact me at st[äd]audius.de !

## Contributing
I need some code review and help to make this provider better! If you find some bugs or the code is "creepy" -> feel free to contribute :)

To contribute, please fork this repository and make pull requests to the master branch.

*The repo optimized for Visual Studio*

## Features
- Works with ADFSv3 (Windows Server 2012 R2)
- Office365
- Easy to implement
- Trigger automatically a challenge (mail, SMS) on logon
- Seamless integration into the ADFS interface
- Free to use
- Support HOTP, TOTP, SMS or E-Mail
- Don’t require a reboot (on install and uninstall)
- Localization

## Tested
- Windows Server 2012 and R2
- Windows Server 2016
- Windows Server 2019 (please see the Microsoft documentaion - there are some changes to the MFA config.)

### Windows Server 2019
If you use a Windows Server 2019 please activate TLS 1.x for your .NET because TLS 1.0 is deprecated.
Adding `"SchUseStrongCrypto"=dword:00000001` to `HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft.NETFramework\v4.0.30319`
and `HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft.NETFramework\v4.0.30319` fixes the problem (see Issue #32).

## Installation / usage
To install the provider you have to download the pre-compiled binary (click on "[releases](https://github.com/sbidy/privacyIDEA-ADFSProvider/releases/)"), add some information to the config.xml and run the PowerShell script at the ADFS server. Now you can use the privacyIDEA-ADFSPovider at the pre-authentication options in the ADFS settings menu.

### Step-by-step
1. Download the zip from [releases](https://github.com/sbidy/privacyIDEA-ADFSProvider/releases/) or compile the binaries by your own
2. Create a folder at the ADFS under "C:\Program Files\privacyIDEAProvider\"
3. Extract the zip and copy all files (PowerShells, config.xml and the provider-dll) to this folder at the ADFS server
4. Eventually unblock the powershell scripts (under file properties)
5. Open the PowerShell script and check the "StartPath" variable - this should be "C:\Program Files\privacyIDEAProvider\"
6. Open the config.xml file and update the information in it
7. Run the PowerShell with administrator privileges
8. Set the execution policy temporary to "unrestricted" or use `PowerShell.exe -ExecutionPolicy Bypass -File <path to installer>.ps1`
9. After the script runs successfully, you can find in the ADFS management gui at "Pre-Authentication" the new privacyIDEA_ADFSProvider
10. Mark the checkbox
11. Now you should see an OTP textbox after the normal username/password form (consider the changes in Windows Server 2019) 

Test: https://fqdn.domain.com/adfs/ls/IdpInitiatedSignon.aspx (change the FQDN)

Check the EventLog (Custom Views -> Server Roles -> Active Directory Federation Services) for errors!

## EventLog (version 1.3.4)
All errors or exceptions from the provider are logged to the Microsoft Windows internal EventLog.
See in the Applications and Service Logs -> AD FS -> Admin. The source will be registered with the PowerShell installer (1.3.4).
The EventID is 9901.

## Debug - Don't use this in production!!
If you have some issues with the provider, you can install a DEBUG version. The Debug Version provides some additional information and messages.

To readout this messages you have to run the [DebugView](https://docs.microsoft.com/en-us/sysinternals/downloads/debugview) from Sysinternals/Microsoft (no installation required).

1. Download the [DebugView.exe](https://docs.microsoft.com/en-us/sysinternals/downloads/debugview)
2. Run it and configure it

![Debug1](https://github.com/sbidy/privacyIDEA-ADFSProvider/blob/master/Debug_Cap.PNG)

3. Install and run the provider.
4. Look in the Debug-Output for the following message:

![Debug2](https://github.com/sbidy/privacyIDEA-ADFSProvider/blob/master/Debug_Cap2.PNG)

## Configuration changes 
If you change the configuration, you have to reinstall the authentication provider.
It is not sufficient to restart the service.

## Office 365
If you plan to use a on-prem ADFS to authenticate your Office 365 user, you can also use this provider.
Install a ADFS on-prem; implement these provider and configure your Office 365 tenant in the federation mode.

![Schema](https://raw.githubusercontent.com/sbidy/privacyIDEA-ADFSProvider/master/drawing.png)

More info see the Microsoft documentation.

## Authors
Stephan Traub - Sbidy -> https://github.com/sbidy

## Credits
Thanks to Cornelius from privacyIDEA -> https://www.privacyidea.org/

## License
The MIT License (MIT)
