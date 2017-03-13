using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace privacyIDEAADFSProvider
{
    public class OTPprovider
    {

        private string URL;
        /// <summary>
        /// Class creates a OTPprovide for the privacyIDEA system
        /// </summary>
        /// <param name="privacyIDEAurl">Provide the URL (HTTPS) to the privacyIDEA system</param>
        public OTPprovider(string privacyIDEAurl)
        {
            URL = privacyIDEAurl;
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
        }
        /// <summary>
        /// Validates a otp pin to the PID3
        /// </summary>
        /// <param name="OTPuser">User name for the token</param>
        /// <param name="OTPpin">PIN for validation</param>
        /// <param name="realm">Domain/realm name</param>
        /// <returns>true if the pin is correct</returns>
        public bool getAuthOTP(string OTPuser, string OTPpin, string realm)
        {
            string responseString = "";
            try
            {
                // check if otp contains only numbers
                if (!IsDigitsOnly(OTPpin)) return false;

                using (WebClient client = new WebClient())
                {

                    byte[] response =
                    client.UploadValues(URL + "/validate/check", new NameValueCollection()
                    {
                        {"pass", OTPpin},
                        {"user", OTPuser},
                        {"realm", realm}
                    });

                    responseString = Encoding.UTF8.GetString(response);
                }
                Debug.WriteLine("Response: " + responseString);
                return CheckOTPValue(responseString);
            }
            catch (WebException wex)
            {
                Debug.WriteLine(wex);
                return false;
            }
        }
        /// <summary>
        /// Trigger for a otp challange to the PID3
        /// </summary>
        /// <param name="OTPuser">User name for the token</param>
        /// <param name="realm">Domain/realm name</param>
        /// <param name="token">Admin token</param>
        public void triggerChellenge(string OTPuser, string realm, string token)
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    client.Headers.Set("PI-Authorization", token);
                    byte[] response =
                    client.UploadValues(URL + "/validate/triggerchallenge", new NameValueCollection()
                    {
                           { "user", OTPuser},
                           { "realm ", realm},
                    });
                }
            }
            catch (WebException wex)
            {
                Debug.WriteLine(wex);
            }

        }
        /// <summary>
        /// Checks the the json for the otp status
        /// </summary>
        /// <param name="jsonResponse">json sting</param>
        /// <returns>true if the pin is correct</returns>
        private bool CheckOTPValue(string jsonResponse)
        {
            // nomalize string
            Regex regex = new Regex(@"{""status""(.*?)\}");

            Match match = regex.Match(jsonResponse);
            if (match.Success)
            {
                // get data form json response
                var settings = match.Value.Trim('{', '}')
                     .Replace("\"", "")
                     .Replace(" ", "")
                     .Split(',')
                     .Select(s => s.Trim().Split(':'))
                     .ToDictionary(a => a[0], a => a[1]);
                Debug.WriteLine("Match: " + match.Value);
                if ((settings["status"] == "true") && (settings["value"] == "true")) return true;
            }
            return false;
        }
        /// <summary>
        /// Validates the pin for a numeric only string
        /// </summary>
        /// <param name="str">string to validate</param>
        /// <returns>Ture if string only contains numbers</returns>
        private bool IsDigitsOnly(string str)
        {
            foreach (char c in str)
            {
                if (c < '0' || c > '9')
                    return false;
            }
            return true;
        }
        /// <summary>
        /// Requests a admin token for administrative tasks
        /// </summary>
        /// <param name="admin_user">Admin user name</param>
        /// <param name="admin_pw">Admin password</param>
        /// <returns>The admin token</returns>
        public string getAuthToken(string admin_user, string admin_pw)
        {
            string responseString = "";
            try
            {
                using (WebClient client = new WebClient())
                {
                    byte[] response =
                    client.UploadValues(URL + "/auth", new NameValueCollection()
                    {
                           { "username", admin_user },
                           { "password", admin_pw }
                    });
                    responseString = Encoding.UTF8.GetString(response);
                }
                return ExtractAuthToken(responseString);
            }
            catch (WebException wex)
            {
                Debug.WriteLine(wex);
                return "";
            }

        }
        /// <summary>
        /// Extracts the token string from the json
        /// </summary>
        /// <param name="jsonResponse">json string</param>
        /// <returns>The admin token</returns>
        private string ExtractAuthToken(string jsonResponse)
        {
            // nomalize string
            Regex regex = new Regex(@"""token"": "".*?\"",");

            Match match = regex.Match(jsonResponse);
            if (match.Success)
            {
                // get data form json response
                var settings = match.Value.Trim(',')
                     .Replace("\"", "")
                     .Replace(" ", "")
                     .Split(',')
                     .Select(s => s.Trim().Split(':'))
                     .ToDictionary(a => a[0], a => a[1]);
                Debug.WriteLine("MatchToken: " + match.Value);

                if (!string.IsNullOrEmpty(settings["token"])) return settings["token"];
                else return null;
            }
            return null;
        }
        /// <summary>
        /// Enrolls a new token to the specified user
        /// </summary>
        /// <param name="OTPuser">User name to enroll the token</param>
        /// <param name="token">Admin token</param>
        /// <returns>Base64 coded token QR image</returns>
        public Dictionary<string, string> enrollHOTPToken(string OTPuser, string realm, string token)
        {
            string responseString = "";
            try
            {
                using (WebClient client = new WebClient())
                {
                    client.Headers.Set("PI-Authorization", token);
                    byte[] response =
                    client.UploadValues(URL + "/token/init?genkey=1", new NameValueCollection()
                    {
                        { "type ", "hotp" },
                        { "user", OTPuser},
                        {"realm", realm }
                    });
                    responseString = Encoding.UTF8.GetString(response);
                }
                return getQRimage(responseString);
            }
            catch (WebException wex)
            {
                Debug.WriteLine(wex);
                //return getQRimage(responseString);
                return new Dictionary<string, string>();
            }
        }
        /// <summary>
        /// Enrolls a new SMS token to the specified user
        /// </summary>
        /// <param name="OTPuser">User name to enroll the token</param>
        /// <param name="token">Admin token</param>
        /// <returns>Base64 coded token QR image</returns>
        public bool enrollSMSToken(string OTPuser, string realm, string phonenumber, string token)
        {
            string responseString = "";
            try
            {
                using (WebClient client = new WebClient())
                {
                    client.Headers.Set("PI-Authorization", token);
                    byte[] response =
                    client.UploadValues(URL + "/token/init?genkey=1", new NameValueCollection()
                    {
                        {"type ", "sms"},
                        {"user", OTPuser},
                        {"realm", realm},
                        {"phone", phonenumber}
                    });
                    responseString = Encoding.UTF8.GetString(response);
                }
                return CheckOTPValue(responseString);
            }
            catch (WebException wex)
            {
                Debug.WriteLine(wex);
                return false;
            }
        }
        /// <summary>
        /// Extracts the img values from the json string
        /// </summary>
        /// <param name="jsonResponse">json string</param>
        /// <returns></returns>
        private Dictionary<string, string> getQRimage(string jsonResponse)
        {
            // nomalize string
            Regex regex = new Regex(@"\""img"": "".*?\""");
            Dictionary<string, string> imgs = new Dictionary<string, string>();
            int counter = 0;
            Match match = regex.Match(jsonResponse);
            if (match.Success)
            {
                foreach (Match m in regex.Matches(jsonResponse))
                {
                    // get data form json response
                    var settings = match.Value.Replace(" ", "").Replace("\":\"", "\"-");
                    Debug.WriteLine("MatchToken: " + match.Value);
                    imgs.Add(settings.Split('-')[0].Replace("\"", "") + counter++, settings.Split('-')[1].Replace("\"", ""));
                }
            }
            return imgs;
        }
    }
    public class WrongOTPExeption : Exception
    {
        public WrongOTPExeption()
        {

        }

        public WrongOTPExeption(string message) : base(message)
        {
        }

        public WrongOTPExeption(string message, Exception inner) : base(message, inner)
        {

        }
    }
}