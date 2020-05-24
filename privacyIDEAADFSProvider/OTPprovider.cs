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
using System.Collections;

namespace privacyIDEAADFSProvider
{
    public class OTPprovider
    {
        private string URL;
        private bool isChallengeToken = false;
        /// <summary>
        /// Class creates a OTPprovide for the privacyIDEA system
        /// </summary>
        /// <param name="privacyIDEAurl">Provide the URL (HTTPS) to the privacyIDEA system</param>
        public OTPprovider(string privacyIDEAurl)
        {
            URL = privacyIDEAurl;
        }

        // Properties
        public string ChallengeMessage { get; set; }

        /// <summary>
        /// Dispatcher methode for #14 - made two request to avoid auth fail by TOTP with PIN
        /// </summary>
        /// <param name="OTPuser">User name for the token</param>
        /// <param name="OTPpin">PIN for validation</param>
        /// <param name="realm">Domain/realm name</param>
        /// <param name="transaction_id">ID for the coresponding challenge</param>
        /// <returns>true if the pin is correct</returns>
        public bool getAuthOTP(string OTPuser, string OTPpin, string realm, string transaction_id)
        {
            if (isChallengeToken)
            {
                // first request with transaction_id
                bool request_with_id = validateOTP(OTPuser, OTPpin, realm, transaction_id);
                // first ture retrun direct (SMS or Mail token)
                if (request_with_id) return true;
                // second request without transaction_id (TOTP)
                else return validateOTP(OTPuser, OTPpin, realm, null);
            }
            else
            {
                // if no challenge token for the user exists request without
                return validateOTP(OTPuser, OTPpin, realm, transaction_id);
            }
        }

        /// <summary>
        /// Validates a otp pin to the PID3
        /// </summary>
        /// <param name="OTPuser">User name for the token</param>
        /// <param name="OTPpin">PIN for validation</param>
        /// <param name="realm">Domain/realm name</param>
        /// <param name="transaction_id">ID for the coresponding challenge</param>
        /// <returns>true if the pin is correct</returns>
        private bool validateOTP(string OTPuser, string OTPpin, string realm, string transaction_id)
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
                LogEvent(EventContext.ID3Aprovider, "validateOTP: " + wex.Message + "\n\n" + wex, EventLogEntryType.Error);
                return false;
            }
        }
        /// <summary>
        /// Trigger for a otp challenge to the PID3
        /// </summary>
        /// <param name="OTPuser">User name for the token</param>
        /// <param name="realm">Domain/realm name</param>
        /// <param name="token">Admin token</param>
        /// <returns>string transaction_id for the challenge</returns>
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
                           { "realm", realm},
                    });
                    responseString = Encoding.UTF8.GetString(response);
                    // get transaction id from response
                    string transaction_id = getJsonNode(responseString, "transaction_ids");
                    // get the message from the challenge
                    string messages = getJsonNode(responseString, "messages");
                    if (!string.IsNullOrEmpty(messages))
                    {
                        this.ChallengeMessage = messages;
                    }
                    if (transaction_id.Length > 20) transaction_id = transaction_id.Remove(20);
                    // check if use has challenge token
                    if (getJsonNode(responseString, "value") != "0") this.isChallengeToken = true;
                    return transaction_id;
                }
            }
            catch (WebException wex)
            {
                LogEvent(EventContext.ID3Aprovider, "triggerChallenge: " + wex.Message + "\n\n" + wex, EventLogEntryType.Error);
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
                LogEvent(EventContext.ID3Aprovider, "getAuthToken: " + wex.Message + "\n\n" + wex, EventLogEntryType.Error);
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
                LogEvent(EventContext.ID3Aprovider, "enrollHOTPToken: " + wex.Message + "\n\n" + wex, EventLogEntryType.Error);
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
                LogEvent(EventContext.ID3Aprovider, "enrollSMSToken: " + wex.Message + "\n\n"+ wex, EventLogEntryType.Error);
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
                //Console.WriteLine(xml);
                // return a list if messages node is called
                if (nodename == "messages")
                {
                    string html_message = "";
                    IEnumerable<XElement> childElements =
                        from el in xml.Descendants(nodename).Elements()
                        select el;
                    foreach (XElement el in childElements)
                        html_message += el.Value+"<br/>";
                    return(html_message);
                }

                return xml.Descendants(nodename).Single().Value;
            }
            catch(Exception ex)
            {
                LogEvent(EventContext.ID3Aprovider, "getJsonNode: " + ex.Message + "\n\n" + ex, EventLogEntryType.Error);
                return "";
            }
        }
        /// <summary>
        /// Helper: Creates a log entry in the MS EventLog under Applications
        /// </summary>
        /// <param name="context"></param>
        /// <param name="message"></param>
        /// <param name="type"></param>
        public void LogEvent(EventContext context, string message, EventLogEntryType type)
        {
            int eventID = 0;
            if (context == EventContext.ID3Aprovider) eventID = 9901;
            if (context == EventContext.ID3A_ADFSadapter) eventID = 9902;
            using (EventLog eventLog = new EventLog("AD FS/Admin"))
            {
                    eventLog.Source = "privacyIDEAProvider";
                    eventLog.WriteEntry(message, type, eventID, 0);
             }
        }
        public enum EventContext
        {
            ID3Aprovider,
            ID3A_ADFSadapter
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
