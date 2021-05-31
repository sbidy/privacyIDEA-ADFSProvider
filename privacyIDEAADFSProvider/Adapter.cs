using System.Net;
using Microsoft.IdentityServer.Web.Authentication.External;
using Claim = System.Security.Claims.Claim;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Xml.Serialization;
using System.DirectoryServices.AccountManagement;
using System;

// old b6483f285cb7b6eb
// new bf6bdb60967d5ecc 1.3.2
// new 

namespace privacyIDEAADFSProvider
{
    public class Adapter : IAuthenticationAdapter
    {
        private string debugPrefix = "ID3A_ADFSadapter: ";
        private string version = typeof(Adapter).Assembly.GetName().Version.ToString();
        // TODO: Create a property class
        private string privacyIDEAurl;
        private string privacyIDEArealm;
        private string privacyIDEArealmsource;
        string transaction_id = "";
        
        private string UserRealm;
        
        private bool ssl = true;
        private string token;
        private string admin_user;
        private string admin_pw;
        private bool show_challenge = false;
        private bool use_upn = false;
        private string message = "";
        public ADFSinterface[] uidefinition;
        private OTPprovider otp_prov;

        public IAuthenticationAdapterMetadata Metadata
        {
            //get { return new <instance of IAuthenticationAdapterMetadata derived class>; }
            get {
                AdapterMetadata meta = new AdapterMetadata();
                meta.AdapterMetadataInit(uidefinition);
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

            string username, domain="", upn="";
            string[] a_identityClaim = identityClaim.Value.Split('\\'); // identityClaim is NETBIOSDOMAIN\sAMAccountName

            if (a_identityClaim.Length > 1) 
            {
                username = a_identityClaim[1];
                domain = a_identityClaim[0];

                if (privacyIDEArealmsource == "NETBIOS")
                {
                    UserRealm = a_identityClaim[0];
                }
                else {
                    UserRealm = privacyIDEArealm;
                }

                // get UPN from sAMAccountName
                upn = GetUserPrincipalName(username, domain);
                if (upn.Length > 1)
                {
                    // use upn or sam as loginname attribute
                    if (use_upn)
                    {
                        username = upn;
                    }

                    if (privacyIDEArealmsource == "FQDN")
                    {
                    // get FQDN for realm from UPN
                    string[] a_tmpupn = upn.Split('@');

                        if (a_tmpupn.Length > 1)
                        {
                            UserRealm = a_tmpupn[1];
                        }
                        else UserRealm = privacyIDEArealm;
                    }
                }

                else
                {
                    UserRealm = privacyIDEArealm;
                }
            }
            else
            {
                username = a_identityClaim[0];
                UserRealm = privacyIDEArealm;
            }

#if DEBUG
            Debug.WriteLine(debugPrefix + " UPN value: " + upn + " Domain value: "+domain);
#endif
            // check if ssl is disabled in the config
            // TODO: Delete for security reasons 
            if (!ssl) ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;

            // use upn or sam as loginname attribute
            //if (use_upn) username = upn;

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
                //transaction_id = otp_prov.triggerChallenge(username, privacyIDEArealm, token);
                transaction_id = otp_prov.triggerChallenge(username, UserRealm, token);

            }
            // set vars to context - fix for 14 and 15
            authContext.Data.Add("userid", username);
            //authContext.Data.Add("realm", privacyIDEArealm);
            authContext.Data.Add("realm", UserRealm);
            authContext.Data.Add("transaction_id", transaction_id);
            // defeine if massage will be showen
            if (show_challenge) message = otp_prov.ChallengeMessage;

            return new AdapterPresentationForm(false, message, uidefinition);
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
                    try
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
                            use_upn = server_config.upn.ToLower() == "false" ? false : true;
                            show_challenge = server_config.ChallengeMessage.ToLower() == "false" ? false : true;
                            privacyIDEArealm = server_config.realm;
                            privacyIDEArealmsource = server_config.realmsource.ToUpper();
                            privacyIDEAurl = server_config.url;
                            uidefinition = server_config.@interface;
                        }
                    }
                    catch
                    {
                        throw new Exception("Config cant be validated!");
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
            return new AdapterPresentationForm(true, message, uidefinition);
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
                return new AdapterPresentationForm(true, message, uidefinition);
            }
        }
        /// <summary>
        /// Check the OTP an does the real authentication
        /// </summary>
        /// <param name="proofData">the date from the HTML fild</param>
        /// <param name="authContext">The autch context which contains secrued parametes.</param>
        /// <returns>True if auth is done and user can be validated</returns>
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

        private string GetUserPrincipalName(string userName, string domain)
        {
            // set up domain context
            PrincipalContext ctx = new PrincipalContext(ContextType.Domain, domain);

            // find a user
            UserPrincipal user = UserPrincipal.FindByIdentity(ctx, userName);

            if (user != null)
            {
               return user.UserPrincipalName;
            }
            else
            {
                return null;
            }
        }

    }
}
