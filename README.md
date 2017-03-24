## Contributing
I need some code review and help to make this provider better! If you find some bugs or the code is "creepy" -> feel free to contribute :)

To contribute, please fork this repository and make pull requests to the master branch.

*The repo optimized for Visual Studio*

## Abstract
A Microsoft Active Directory Federation Service (ADFS) provider for the open source authentication system privacyIDEA.

In some it-compliance or best practice papers, it is highly recommend adding a second factor to the username and password combination to increase the security level.
The implementation of this type of advanced authentication can be very challenging the it-infrastructure and administrators.
This open source project provides an easy way to connect the authentication components and the on-premises free-to-use privacyIDEA.
You do not need an additional RADIU-Server or other components to connect these two systems together. Only this provider hast to be registered at the ADFS.
After that, you can use the TOTP, HOTP, SMS or E-Mail authentication method to authenticate the user for products like Microsoft Exchange, Microsoft Dynamics or Office 365.

This ADFSProvider gives you nearly the same capabilities as the cloud based Azure Authentication Service. However, this provider and the on-prem privacyIDEA authentication system is open source and free. 

## Features
- Works with ADFSv3 (Windows Server 2012 R2)
- Easy to implement
- Trigger automatically a challenge (mail, SMS) on logon
- Seamless integration into the ADFS interface
- Free to use
- Support HOTP, TOTP, SMS or E-Mail
- Don’t require a reboot (on install and uninstall)

## Installation / usage
To install the provider you have to download the pre-compiled binary (click on "releases"), add some information to the config.xml and run the PowerShell script at the ADFS server. Now you can use the privacyIDEA_ADFSPovider at the pre-authentication options in the ADFS settings menu.

### Step-by-step
1. Download the zip from releases or compile the binaries by your own
2. Create a folder at the ADFS under "C:\Program and Files\privacyIDEAProvider\"
3. Extract the zip and copy all files to this folder at the ADFS server
4. Open the PowerShell script and check the "StartPath" variable - this should be "C:\Program and Files\privacyIDEAProvider\"
5. Open the config.xml file and update the information in it
!!! The privacyIDEA user should have permissions to authenticate users!!!
6. Run the PowerShell with administrator privileges
7. After the script runs successfully, you can find in the ADFS management gui at "Pre-Authentication" the new privacyIDEAADFSProvider
8. Mark the checkbox
9. Now you should see an OTP textbox after the normal username/password form

## Office 365
If you plan to use a on-prem ADFS to authenticate your Office 365 user, you can also use this provider.
Install a ADFS on-prem; implement these provider and configure your Office 365 tenant with the federation mode.

More info see the Microsoft documentation.

## TBD

- Update the json parsing
- Advanced documentation
- Code review
- Security review
- Some paperwork and references
...

## Authors
Stephan Traub - Sbidy -> https://github.com/sbidy

## Credits
Cornelius - privacyIDEA -> https://www.privacyidea.org/

## License
The MIT License (MIT)
