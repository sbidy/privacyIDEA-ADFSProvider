using System.Collections.Generic;
using System.Globalization;
using Microsoft.IdentityServer.Web.Authentication.External;

namespace privacyIDEAADFSProvider
{
    class AdapterMetadata : IAuthenticationAdapterMetadata
    {
        public string adapterversion { get; set; }

        public ADFSinterface[] inter;

        public void AdapterMetadataInit(ADFSinterface[] adfsinter)
        {
            this.inter = adfsinter;
        }

        //Returns the name of the provider that will be shown in the AD FS management UI (not visible to end users)
        public string AdminName
        {
            get { return "privacyIDEA-ADFSProvider_"+adapterversion; }
        }

        //Returns an array of strings containing URIs indicating the set of authentication methods implemented by the adapter 
        /// AD FS requires that, if authentication is successful, the method actually employed will be returned by the
        /// final call to TryEndAuthentication(). If no authentication method is returned, or the method returned is not
        /// one of the methods listed in this property, the authentication attempt will fail.
        public virtual string[] AuthenticationMethods
        {
            get { return new[] { "http://schemas.microsoft.com/ws/2012/12/authmethod/otp" }; }
        }

        /// Returns an array indicating which languages are supported by the provider. AD FS uses this information
        /// to determine the best language\locale to display to the user.
        public int[] AvailableLcids
        {
            get
            {
                List<int> LCIDS = new List<int>();
                bool HasDefault = false;
                if (inter != null)
                {
                    foreach (ADFSinterface adfsui in inter)
                    {
                        LCIDS.Add(adfsui.LCID);
                        if (adfsui.LCID == 1033) HasDefault = true;
                    }
                } 
                //Fallback and fixup
                if ((LCIDS.Count == 0) | (!HasDefault)) 
                    LCIDS.Add(1033);
                return LCIDS.ToArray();
            }
        }

        /// Returns a Dictionary containing the set of localized friendly names of the provider, indexed by lcid. 
        /// These Friendly Names are displayed in the "choice page" offered to the user when there is more than 
        /// one secondary authentication provider available.
        public Dictionary<int, string> FriendlyNames
        {
            get
            {
                Dictionary<int, string> _friendlyNames = new Dictionary<int, string>();
                bool HasDefault = false;
                if (inter != null)
                {
                    foreach (ADFSinterface adfsui in inter)
                    {
                        _friendlyNames.Add(adfsui.LCID, adfsui.friendlyname);
                        if (adfsui.LCID == 1033) HasDefault = true;
                    }
                }
                
                if ((_friendlyNames.Count == 0)|(!HasDefault)) 
                    _friendlyNames.Add(1033, "privacyIDEA ADFS authentication provider");
                
                return _friendlyNames;
            }
        }

        /// Returns a Dictionary containing the set of localized descriptions (hover over help) of the provider, indexed by lcid. 
        /// These descriptions are displayed in the "choice page" offered to the user when there is more than one 
        /// secondary authentication provider available.
        public Dictionary<int, string> Descriptions
        {
            get
            {
                Dictionary<int, string> _descriptions = new Dictionary<int, string>();
                bool HasDefault = false;
                if (inter != null)
                {
                    foreach (ADFSinterface adfsui in inter)
                    {
                        _descriptions.Add(adfsui.LCID, adfsui.description);
                        if (adfsui.LCID == 1033) HasDefault = true;
                    }
                }
                if ((_descriptions.Count == 0)|(!HasDefault))
                    _descriptions.Add(1033, "privacyIDEA ADFS provider to access the api");
                return _descriptions;
            }
        }

        /// Returns an array indicating the type of claim that that the adapter uses to identify the user being authenticated.
        /// Note that although the property is an array, only the first element is currently used.
        /// MUST BE ONE OF THE FOLLOWING
        /// "http://schemas.microsoft.com/ws/2008/06/identity/claims/windowsaccountname"
        /// "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn"
        /// "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"
        /// "http://schemas.microsoft.com/ws/2008/06/identity/claims/primarysid"
        public string[] IdentityClaims
        {
            get { return new[] { "http://schemas.microsoft.com/ws/2008/06/identity/claims/windowsaccountname" }; }
        }

        //All external providers must return a value of "true" for this property.
        public bool RequiresIdentity
        {
            get { return true; }
        }
    }
}
