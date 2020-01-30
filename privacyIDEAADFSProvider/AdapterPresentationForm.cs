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
            string wellcomemassage = "";
            string htmlTemplate = Resources.AuthPage; // return normal page

            if (inter != null)
            {
                foreach (ADFSinterface adfsui in inter)
                {
#if DEBUG
                Debug.WriteLine("LICD: "+adfsui.LICD+" - "+ lcid.ToString());
#endif
                    if ((int)adfsui.LICD == (int)lcid)
                    {
                        errormessage = adfsui.errormessage;
                        wellcomemassage = adfsui.wellcomemessage;
                    }
                    // fallback to EN-US if nothing is defined
                    else
                    {
                        errormessage = "Login failed! Please try again!";
                        wellcomemassage = "Please provide the one-time-password:";
                    }
                }
            }
            if (error)
            {
                htmlTemplate = htmlTemplate.Replace("#ERROR#", errormessage);
                htmlTemplate = htmlTemplate.Replace("#MESSAGE#", wellcomemassage);
            }
            else
            {
                htmlTemplate = htmlTemplate.Replace("#MESSAGE#", wellcomemassage);
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
            // in case of failure
            return "privacyIDEA OTP Adapter E";
        }

    }
}
