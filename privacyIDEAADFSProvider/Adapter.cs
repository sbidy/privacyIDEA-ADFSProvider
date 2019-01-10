using System.Net;
using Microsoft.IdentityServer.Web.Authentication.External;
using Claim = System.Security.Claims.Claim;
using System.IO;
using System.Text;
using System.Xml;
using System.Diagnostics;
using System.Xml.Serialization;
using System.Collections.Generic;

// old b6483f285cb7b6eb
// new bf6bdb60967d5ecc 1.3.2

namespace privacyIDEAADFSProvider
{
    public class Adapter : IAuthenticationAdapter
    {
        private string debugPrefix = "ID3A_ADFSadapter: ";
        private string version = typeof(Adapter).Assembly.GetName().Version.ToString();
        // TODO: Create a property class
        private string privacyIDEAurl;
        public string privacyIDEArealm;
        string transaction_id = "";
        private bool ssl = true;
        private string token;
        private string admin_user;
        private string admin_pw;
        public ADFSinterface[] uidefinition;
        private OTPprovider otp_prov;

        public IAuthenticationAdapterMetadata Metadata
        {
            //get { return new <instance of IAuthenticationAdapterMetadata derived class>; }
            get {
                AdapterMetadata meta = new AdapterMetadata();
                meta.adapterversion = version;
                return meta;
            }
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
#if DEBUG
                Debug.WriteLine(debugPrefix + " Claim value: " + identityClaim.Value);
#endif
            // seperates the username from the domain
            // TODO: Map the domain to the ID3A realm
            string[] tmp = identityClaim.Value.Split('\\');
            string username = "";
            if(tmp.Length > 1) username = tmp[1];
            else username = tmp[0];
            // check if ssl is disabled in the config
            // TODO: Delete for security reasons 
            if (!ssl) ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;

             // trigger challenge
            otp_prov = new OTPprovider(privacyIDEAurl);
            // get a new admin token for all requests if the an admin pw is defined
            // #2
            if (!string.IsNullOrEmpty(admin_pw) && !string.IsNullOrEmpty(admin_user))
            {
                token = otp_prov.getAuthToken(admin_user, admin_pw);
                // trigger a challenge (SMS, Mail ...) for the the user
#if DEBUG
                Debug.WriteLine(debugPrefix + " User: " + username + " Realm: " + privacyIDEArealm);
#endif
                transaction_id = otp_prov.triggerChallenge(username, privacyIDEArealm, token);
            }
            // set vars to context - fix for 14 and 15
            authContext.Data.Add("userid", username);
            authContext.Data.Add("realm", privacyIDEArealm);
            authContext.Data.Add("transaction_id", transaction_id);

            return new AdapterPresentationForm(false, uidefinition);
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
                        XmlRootAttribute xRoot = new XmlRootAttribute
                        {
                            ElementName = "server",
                            IsNullable = true
                        };
                        XmlSerializer serializer = new XmlSerializer(typeof(ADFSserver), xRoot);
                        ADFSserver server_config = (ADFSserver)serializer.Deserialize(reader);
                        admin_pw = server_config.adminpw;
                        admin_user = server_config.adminuser;
                        ssl = server_config.ssl.ToLower() == "false" ? false : true;
                        privacyIDEArealm = server_config.realm;
                        privacyIDEAurl = server_config.url;
                        uidefinition = server_config.@interface;
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
            return new AdapterPresentationForm(true, uidefinition);
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
                return new AdapterPresentationForm(true, uidefinition);
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
                // fix for #14 and #15
                string session_user = (string)authContext.Data["userid"];
                string session_realm = (string)authContext.Data["realm"];
                string transaction_id = (string)authContext.Data["transaction_id"];
                // end fix
#if DEBUG
                Debug.WriteLine(debugPrefix+"OTP Code: " + otpvalue + " User: " + session_user + " Server: " + session_realm + " Transaction_id: " + transaction_id);
#endif
                return otp_prov.getAuthOTP(session_user, otpvalue, session_realm, transaction_id);
            }
            catch
            {
                throw new ExternalAuthenticationException("Error - can't validate the otp value", authContext);
            }
        }
    }
}
