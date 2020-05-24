using Microsoft.IdentityServer.Web.Authentication.External;
using System.Diagnostics;

namespace privacyIDEAADFSProvider
{
    class AdapterPresentationForm : IAdapterPresentationForm
    {
        public ADFSinterface[] inter;
        private bool error = false;
        private string _ChallengeMessage;


        public AdapterPresentationForm(bool error, string ChallengeMessage, ADFSinterface[] adfsinter)
        {
            this.error = error;
            this.inter = adfsinter;
            this._ChallengeMessage = ChallengeMessage;
#if DEBUG
            Debug.WriteLine("ID3A_ADFSadapter: Challenge Message: "+ChallengeMessage);
#endif
        }

        /// Returns the HTML Form fragment that contains the adapter user interface. This data will be included in the web page that is presented
        /// to the cient.
        public string GetFormHtml(int lcid)
        {
            
            // check the localization with the lcid
            string errormessage = "Login failed! Please try again!";
            string welcomemessage = "Please provide the one-time-password:";
            string otptext = "OTP Token";
            string submittext = "Submit";
            string htmlTemplate = Resources.AuthPage; // return normal page

            if (inter != null)
            {
                foreach (ADFSinterface adfsui in inter)
                {
#if DEBUG
                    Debug.WriteLine("ID3A_ADFSadapter: Detected language LCID:"+ lcid);
#endif

                    if ((int)adfsui.LCID == (int)lcid)
                    {
                        if (!string.IsNullOrEmpty(adfsui.errormessage)) errormessage = adfsui.errormessage;
                        if (!string.IsNullOrEmpty(adfsui.welcomemessage)) welcomemessage = adfsui.welcomemessage;
                        if (!string.IsNullOrEmpty(adfsui.otptext)) otptext = adfsui.otptext;
                        if (!string.IsNullOrEmpty(adfsui.submittext)) submittext = adfsui.submittext;
                        break;
                    }
                }
            }
            // show the error message in case of a failure
            if (error)
            {
                htmlTemplate = htmlTemplate.Replace("#ERROR#", errormessage);
                htmlTemplate = htmlTemplate.Replace("#MESSAGE#", welcomemessage);
                htmlTemplate = htmlTemplate.Replace("#OTPTEXT#", otptext);
                htmlTemplate = htmlTemplate.Replace("#SUBMIT#", submittext);
                htmlTemplate = htmlTemplate.Replace("#C_MESSAGE#", _ChallengeMessage);
            }
            // show the normal logon message
            else
            {
                htmlTemplate = htmlTemplate.Replace("#MESSAGE#", welcomemessage);
                htmlTemplate = htmlTemplate.Replace("#ERROR#", "");
                htmlTemplate = htmlTemplate.Replace("#OTPTEXT#", otptext);
                htmlTemplate = htmlTemplate.Replace("#SUBMIT#", submittext);
                htmlTemplate = htmlTemplate.Replace("#C_MESSAGE#", _ChallengeMessage);
            }
            return htmlTemplate;
        }

        /// Return any external resources, ie references to libraries etc., that should be included in 
        /// the HEAD section of the presentation form html. 
        public string GetFormPreRenderHtml(int lcid)
        {
            return null;
        }

        //returns the title string for the web page which presents the HTML form content to the end user
        public string GetPageTitle(int lcid)
        {
            if (inter != null)
            {
                foreach (ADFSinterface adfsui in inter)
                {
                    if ((int)adfsui.LCID == (int)lcid)
                    {
                        return adfsui.title;
                    }
                    // fallback to EN-US if nothing is defined
                    else
                    {
                        return "privacyIDEA OTP Adapter";
                    }
                }
            }
            // in case of failure
            return "privacyIDEA OTP Adapter E";
        }

    }
}
