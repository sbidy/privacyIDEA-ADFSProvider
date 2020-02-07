using Microsoft.IdentityServer.Web.Authentication.External;
using System.Diagnostics;

namespace privacyIDEAADFSProvider
{
    class AdapterPresentationForm : IAdapterPresentationForm
    {
        public ADFSinterface[] inter;
        private bool error = false;


        public AdapterPresentationForm(bool error, ADFSinterface[] adfsinter)
        {
            this.error = error;
            this.inter = adfsinter;
        }

        /// Returns the HTML Form fragment that contains the adapter user interface. This data will be included in the web page that is presented
        /// to the cient.
        public string GetFormHtml(int lcid)
        {
            
            // check the localization with the lcid
            string errormessage = "";
            string welcomemassage = "";
            string htmlTemplate = Resources.AuthPage; // return normal page

            if (inter != null)
            {
                foreach (ADFSinterface adfsui in inter)
                {
#if DEBUG
                    Debug.WriteLine("ID3A_ADFSadapter: Detected language LCID:"+ lcid);
#endif

                    if ((int)adfsui.LICD == (int)lcid)
                    {
                        errormessage = adfsui.errormessage;
                        welcomemassage = adfsui.welcomemessage;
                        break;
                    }
                    // fallback to EN-US if nothing is defined
                    else
                    {
                        errormessage = "Login failed! Please try again!";
                        welcomemassage = "Please provide the one-time-password:";
                    }
                }
            }
            // show the error message in case of a failure
            if (error)
            {
                htmlTemplate = htmlTemplate.Replace("#ERROR#", errormessage);
                htmlTemplate = htmlTemplate.Replace("#MESSAGE#", welcomemassage);
            }
            // show the normal logon message
            else
            {
                htmlTemplate = htmlTemplate.Replace("#MESSAGE#", welcomemassage);
                htmlTemplate = htmlTemplate.Replace("#ERROR#", "");

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
                    if ((int)adfsui.LICD == (int)lcid)
                    {
                        return adfsui.titel;
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
