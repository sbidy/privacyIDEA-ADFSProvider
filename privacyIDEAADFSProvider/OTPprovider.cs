using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Runtime.Serialization.Json;
using System.Xml;
using System.Xml.Linq;

namespace privacyIDEAADFSProvider
{
    public class OTPprovider
    {
        private string debugPrefix = "ID3Aprovider: ";
        private string URL;
        /// <summary>
        /// Class creates a OTPprovide for the privacyIDEA system
        /// </summary>
        /// <param name="privacyIDEAurl">Provide the URL (HTTPS) to the privacyIDEA system</param>
        public OTPprovider(string privacyIDEAurl)
        {
            URL = privacyIDEAurl;
        }
        /// <summary>
        /// Validates a otp pin to the PID3
        /// </summary>
        /// <param name="OTPuser">User name for the token</param>
        /// <param name="OTPpin">PIN for validation</param>
        /// <param name="realm">Domain/realm name</param>
        /// <returns>true if the pin is correct</returns>
        public bool getAuthOTP(string OTPuser, string OTPpin, string realm, string transaction_id)
        {
            string responseString = "";
            try
            {
                // check if otp contains only numbers
                // Bug #10 - beaks the OTP+PIN combination - removed
                //if (!IsDigitsOnly(OTPpin)) return false;

                NameValueCollection request_header = new NameValueCollection(){
                        {"pass", OTPpin},
                        {"user", OTPuser},
                        {"realm", realm}
                    };
                // add transaction id if challenge request
                if (!string.IsNullOrEmpty(transaction_id)) request_header.Add("transaction_id", transaction_id);
                // send reqeust
                using (WebClient client = new WebClient())
                {
                    byte[] response =
                    client.UploadValues(URL + "/validate/check", request_header);
                    responseString = Encoding.UTF8.GetString(response);
                }
                return (getJsonNode(responseString, "status") == "true" && getJsonNode(responseString, "value") == "true");
            }
            catch (WebException wex)
            {
                Debug.WriteLine(debugPrefix + wex);
                return false;
            }
        }
        /// <summary>
        /// Trigger for a otp challenge to the PID3
        /// </summary>
        /// <param name="OTPuser">User name for the token</param>
        /// <param name="realm">Domain/realm name</param>
        /// <param name="token">Admin token</param>
        public string triggerChallenge(string OTPuser, string realm, string token)
        {
            string responseString = "";
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
                    responseString = Encoding.UTF8.GetString(response);
                    string transaction_id = getJsonNode(responseString, "transaction_ids");
                    // ToDo - not realy a solution if multible tocken enrolled!! For #15
                    if (transaction_id.Length > 20) return transaction_id.Remove(20);
                    else return transaction_id;
                }
            }
            catch (WebException wex)
            {
                Debug.WriteLine(debugPrefix + wex);
                return "";
            }

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
                return getJsonNode(responseString, "token");
            }
            catch (WebException wex)
            {
                Debug.WriteLine(debugPrefix + wex);
                return "";
            }

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
                Debug.WriteLine(debugPrefix + wex);
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
                return (getJsonNode(responseString, "status") == "true" && getJsonNode(responseString, "value") == "true");
            }
            catch (WebException wex)
            {
                Debug.WriteLine(debugPrefix + wex);
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
            Dictionary<string, string> imgs = new Dictionary<string, string>();
            var xml = XDocument.Load(JsonReaderWriterFactory.CreateJsonReader(Encoding.ASCII.GetBytes(jsonResponse), new XmlDictionaryReaderQuotas()));
            int counter = 0;
            foreach (XElement element in xml.Descendants("img"))
            {
                imgs.Add(counter++.ToString(), element.Value);
            }
            return imgs;
        }

        /////////////////////////////////////////////////////////////////
        // ------- HELPER ------ 
        /////////////////////////////////////////////////////////////////
        /// <summary>
        /// Validates the pin for a numeric only string
        /// </summary>
        /// <param name="str">string to validate</param>
        /// <returns>True if string only contains numbers</returns>
        private bool IsDigitsOnly(string str)
        {
            foreach (char c in str)
            {
                if (c < '0' || c > '9')
                    return false;
            }
            if (str.Length > 8) return false;

            return true;
        }
        /// <summary>
        /// Get json information form a defined node
        /// </summary>
        /// <param name="jsonResponse">json string</param>
        /// <param name="nodename">node name of the json field</param>
        /// <returns>returns the value (inner text) from the defined node</returns>
        private string getJsonNode(string jsonResponse, string nodename)
        {
            try
            {
                var xml = XDocument.Load(JsonReaderWriterFactory.CreateJsonReader(Encoding.ASCII.GetBytes(jsonResponse), new XmlDictionaryReaderQuotas()));
                return xml.Descendants(nodename).Single().Value;
            }
            catch(Exception ex)
            {
                Debug.WriteLine(debugPrefix + ex);
                return "";
            }
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