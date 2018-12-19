using Microsoft.IdentityServer.Web.Authentication.External;
using System.Diagnostics;

namespace privacyIDEAADFSProvider
{
    class AdapterPresentationForm : IAdapterPresentationForm
    {
        public ADFSinterface[] inter;
        private bool error = false;
        private string username = "";
        private string realm = "";
        private string id = "";

        public AdapterPresentationForm(bool error, ADFSinterface[] adfsinter)
        {
            this.error = error;
            this.inter = adfsinter;
        }
        public AdapterPresentationForm(ADFSinterface[] adfsinter, string username, string realm, string id)
        {
            this.inter = adfsinter;
            this.username = username;
            this.id = id;
            this.realm = realm;
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
                    if (adfsui.LICD == lcid.ToString())
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
            // fix for #14 and 15
            htmlTemplate = htmlTemplate.Replace("#USER#", this.username);
            htmlTemplate = htmlTemplate.Replace("#REALM#", this.realm);
            htmlTemplate = htmlTemplate.Replace("#ID#", this.id);
            // end fix
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
            return "privacyIDEA OTP Adapter";
        }

    }
}
