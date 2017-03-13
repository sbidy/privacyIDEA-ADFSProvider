using Microsoft.IdentityServer.Web.Authentication.External;

namespace privacyIDEAADFSProvider
{
    class AdapterPresentationForm : IAdapterPresentationForm
    {
        public string errormessage = "";
        public string wellcomemassage = "";
        private bool error = false;

        public AdapterPresentationForm(bool error, string errormessage)
        {
            this.error = error;
            this.errormessage = errormessage;
        }
        public AdapterPresentationForm(string wellcomemassage)
        {
            this.wellcomemassage = wellcomemassage;
        }

        /// Returns the HTML Form fragment that contains the adapter user interface. This data will be included in the web page that is presented
        /// to the cient.
        public string GetFormHtml(int lcid)
        {
            string htmlTemplate = Resources.AuthPage; // return normal page
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
