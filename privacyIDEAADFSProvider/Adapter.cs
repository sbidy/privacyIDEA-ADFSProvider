using System.Net;
using Microsoft.IdentityServer.Web.Authentication.External;
using Claim = System.Security.Claims.Claim;
using System.IO;
using System.Text;
using System.Xml;
using System.Diagnostics;

// b6483f285cb7b6eb
namespace privacyIDEAADFSProvider
{
    public class Adapter : IAuthenticationAdapter
    {

        // TODO: Create a property class
        private string privacyIDEAurl;
        private string privacyIDEArealm;
        private string username;
        private bool ssl = true;
        private string erromessage;
        private string wellcomemessage;
        private string token;
        private string admin_user;
        private string admin_pw;

        public IAuthenticationAdapterMetadata Metadata
        {
            //get { return new <instance of IAuthenticationAdapterMetadata derived class>; }
            get { return new AdapterMetadata(); }
        }
        /// <summary>
        /// Initiates a new authentication process and returns to the ADFS system.
        /// </summary>
        /// <param name="identityClaim">Claim information from the ADFS</param>
        /// <param name="request">The http request</param>
        /// <param name="authContext">The context for the authentication</param>
        /// <returns>new instance of IAdapterPresentationForm</returns>
        public IAdapterPresentation BeginAuthentication(Claim identityClaim, HttpListenerRequest request, IAuthenticationContext authContext)
        {
            // seperates the username from the domain
            // TODO: Map the domain to the PI3A realm
            string[] tmp = identityClaim.Value.Split('\\');
            if(tmp.Length > 1) username = tmp[1];
            else username = tmp[0];
            // check if ssl is disabled in the config
            // TODO: Delete for security reasons 
            if (!ssl) ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;

            // trigger challenge
            OTPprovider otp_prov = new OTPprovider(privacyIDEAurl);
            // get a new admin token for all requests
            token = otp_prov.getAuthToken(admin_user, admin_pw);
            // trigger a challenge (SMS, Mail ...) for the the user
            otp_prov.triggerChellenge(username,privacyIDEArealm,token);

            return new AdapterPresentationForm(wellcomemessage);
        }

        // TODO remove ?
        public bool IsAvailableForUser(Claim identityClaim, IAuthenticationContext authContext)
        {
            return true; //its all available for now
        }

        public void OnAuthenticationPipelineLoad(IAuthenticationMethodConfigData configData)
        {
            //this is where AD FS passes us the config data, if such data was supplied at registration of the adapter
            if (configData != null)
            {
                if (configData.Data != null)
                {
                    // load the config file
                    // TODO: Handle errors and exceptions
                    using (StreamReader reader = new StreamReader(configData.Data, Encoding.UTF8))
                    {
                        string config = reader.ReadToEnd();
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(config);
                        // parse the xml document
                        // TODO: check for the correctness ??
                        XmlNode node = doc.DocumentElement.SelectSingleNode("/server/url");
                        privacyIDEAurl = node.InnerText;
                        node = doc.DocumentElement.SelectSingleNode("/server/realm");
                        privacyIDEArealm = node.InnerText;
                        node = doc.DocumentElement.SelectSingleNode("/server/ssl");
                        // SSL check activate or disabel
                        // TODO: Should be removed !?
                        ssl = node.InnerText.ToLower() == "false" ? false : true;
                        // Text in html document
                        node = doc.DocumentElement.SelectSingleNode("/server/interface/wellcomemessage");
                        wellcomemessage = node.InnerText;
                        node = doc.DocumentElement.SelectSingleNode("/server/interface/errormessage");
                        erromessage = node.InnerText;
                        // admin credentials
                        node = doc.DocumentElement.SelectSingleNode("/server/adminuser");
                        admin_user = node.InnerText;
                        node = doc.DocumentElement.SelectSingleNode("/server/adminpw");
                        admin_pw = node.InnerText;
#if DEBUG
                        Debug.WriteLine("Server:" + privacyIDEAurl + " Realm:" + privacyIDEArealm + " SSL status: " + ssl);
#endif
                    }
                }
            }
        }
        /// <summary>
        /// cleanup function - nothing to do her
        /// </summary>
        public void OnAuthenticationPipelineUnload()
        {
        }
        /// <summary>
        /// Called on error and represents the authform with a error message
        /// </summary>
        /// <param name="request">the http request object</param>
        /// <param name="ex">exception message</param>
        /// <returns>new instance of IAdapterPresentationForm derived class</returns>
        public IAdapterPresentation OnError(HttpListenerRequest request, ExternalAuthenticationException ex)
        {
            return new AdapterPresentationForm(true, erromessage);
        }
        /// <summary>
        /// Function call after the user hits submit - it proofs the values (OTP pin)
        /// </summary>
        /// <param name="authContext"></param>
        /// <param name="proofData"></param>
        /// <param name="request"></param>
        /// <param name="outgoingClaims"></param>
        /// <returns></returns>
        public IAdapterPresentation TryEndAuthentication(IAuthenticationContext authContext, IProofData proofData, HttpListenerRequest request, out Claim[] outgoingClaims)
        {
            outgoingClaims = new Claim[0];
            if (ValidateProofData(proofData, authContext))
            {
                //authn complete - return authn method
                outgoingClaims = new[]
                {
                     // Return the required authentication method claim, indicating the particulate authentication method used.
                     new Claim( "http://schemas.microsoft.com/ws/2008/06/identity/claims/authenticationmethod", "http://schemas.microsoft.com/ws/2012/12/authmethod/otp")
                 };
                return null;
            }
            else
            {
                //authentication not complete - return new instance of IAdapterPresentationForm derived class and the generic error message
                return new AdapterPresentationForm(true, erromessage);
            }
        }

        bool ValidateProofData(IProofData proofData, IAuthenticationContext authContext)
        {
            if (proofData == null || proofData.Properties == null || !proofData.Properties.ContainsKey("otpvalue"))
                   throw new ExternalAuthenticationException("Error - no answer found", authContext);

            if (!ssl)
                   ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;

            try
            {
                string otpvalue = (string)proofData.Properties["otpvalue"];
                OTPprovider otp_prov = new OTPprovider(privacyIDEAurl);
#if DEBUG
                Debug.WriteLine("OTP Code: " + otpvalue + " User: " + username + " Server:" + privacyIDEAurl);
#endif
                return otp_prov.getAuthOTP(username, otpvalue, privacyIDEArealm);
            }
            catch
            {
                throw new ExternalAuthenticationException("Error - cant validate the otp value", authContext);
            }
        }
    }
}
