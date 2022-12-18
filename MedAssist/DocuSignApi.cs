using DocuSign.eSign.Client;
using System.Collections.Generic;
using static DocuSign.eSign.Client.Auth.OAuth;
using DocuSign.eSign.Api;
using DocuSign.eSign.Model;
using System;

namespace SccDocuSign
{
    public class DocuSignAPI
    {
        public static OAuthToken  GetAccessToken(DSConfiguration _dsConfig)
        {
            var clientID = _dsConfig.ClientId;
            var userID = _dsConfig.ImpersonatedUserId;
            var server = _dsConfig.AuthServer;
            var privateKey = _dsConfig.PrivateKeyFile;
            var basePath = _dsConfig.BaseURI;
            var scopes = new List<string>
                {
                    "signature",
                    "impersonation",
                };
            var apiClient = new DocuSignClient(basePath);
            OAuthToken _authToken = apiClient.RequestJWTUserToken(
               clientID, userID, server,
               DSHelper.ReadFileContent(DSHelper.PrepareFullPrivateKeyFilePath(privateKey)), 1, scopes);
            return _authToken;
        }

        private static RecipientViewRequest MakeRecipientViewRequest(long appID,string signerEmail, string signerName, string returnUrl, string signerClientId, string envelopeId, string pingUrl = null)
        {
            RecipientViewRequest viewRequest = new RecipientViewRequest();
            // Set the url where you want the recipient to go once they are done signing
            // should typically be a callback route somewhere in your app.
            // The query parameter is included as an example of how
            // to save/recover state information during the redirect to
            // the DocuSign signing ceremony. It's usually better to use
            // the session mechanism of your web framework. Query parameters
            // can be changed/spoofed very easily.
            viewRequest.ReturnUrl = returnUrl + "?state="+appID+"&envelopeId=" + envelopeId;
            // How has your app authenticated the user? In addition to your app's
            // authentication, you can include authenticate steps from DocuSign.
            // Eg, SMS authentication
            viewRequest.AuthenticationMethod = "none";
            // Recipient information must match embedded recipient info
            // we used to create the envelope.
            viewRequest.Email = signerEmail;
            viewRequest.UserName = signerName;
            viewRequest.ClientUserId = signerClientId;
            // DocuSign recommends that you redirect to DocuSign for the
            // Signing Ceremony. There are multiple ways to save state.
            // To maintain your application's session, use the pingUrl
            // parameter. It causes the DocuSign Signing Ceremony web page
            // (not the DocuSign server) to send pings via AJAX to your
            // app,
            // NOTE: The pings will only be sent if the pingUrl is an https address
            if (pingUrl != null)
            {
                viewRequest.PingFrequency = "600"; // seconds
                viewRequest.PingUrl = pingUrl; // optional setting
            }
            return viewRequest;
        }
  
        public static string CreateEmbeddedConsoleView(DSConfiguration _dsConfig,
            string accessToken,
            string returnUrl,
            string envelopeId)
        {
            //var accessToken = GetAccessToken(_dsConfig);
            var apiClient = new DocuSignClient(_dsConfig.BaseURI);
            apiClient.Configuration.DefaultHeader.Add("Authorization", "Bearer " + accessToken);
            EnvelopesApi envelopesApi = new EnvelopesApi(apiClient);
            ConsoleViewRequest viewRequest = MakeConsoleViewRequest(
                returnUrl,
                envelopeId);

            // Step 1. create the NDSE view
            // Call the CreateSenderView API
            // Exceptions will be caught by the calling function
            ViewUrl results = envelopesApi.CreateConsoleView(_dsConfig.AccountID, viewRequest);
            string redirectUrl = results.Url;
            return redirectUrl;
        }
        private static ConsoleViewRequest MakeConsoleViewRequest(
            string dsReturnUrl,
            string envelopeId)
        {
            // Data for this method
            // dsReturnUrl
            // startingView
            // envelopeId
            ConsoleViewRequest viewRequest = new ConsoleViewRequest();

            // Set the URL where you want the recipient to go once they are done
            // with the NDSE. It is usually the case that the
            // user will never "finish" with the NDSE.
            // Assume that control will not be passed back to your app.
            if (!string.IsNullOrEmpty(dsReturnUrl))
            {
                viewRequest.ReturnUrl = dsReturnUrl;
            }

            if (!string.IsNullOrEmpty(envelopeId))
            {
                viewRequest.EnvelopeId = envelopeId;
            }

            return viewRequest;
        }

        /// <summary>
        /// Resends an Existing Envelope
        /// </summary>
        /// <param name="_dsConfig"></param>
        /// <param name="accessToken"></param>
        /// <param name="envelopeId"></param>
        /// <returns></returns>
        public static bool ResendEnvelope(DSConfiguration _dsConfig, string accessToken, string envelopeId)
        {
            var apiClient = new DocuSignClient(_dsConfig.BaseURI);
            apiClient.Configuration.DefaultHeader.Add("Authorization", "Bearer " + accessToken);
            EnvelopesApi envelopesApi = new EnvelopesApi(apiClient);
            var recipients = envelopesApi.ListRecipients(_dsConfig.AccountID, envelopeId);
            RecipientsUpdateSummary summary = envelopesApi.UpdateRecipients(_dsConfig.AccountID, envelopeId, recipients, new EnvelopesApi.UpdateRecipientsOptions { resendEnvelope="true"});
            foreach(var result in summary.RecipientUpdateResults)
            {
                if(result.ErrorDetails != null)
                {
                    //TODO: Figure out what an error looks like
                    //log error?
                    return false;
                }
            }
            return true;
        }

        public static string EnvelopeStatus(DSConfiguration _dsConfig, string accessToken, string envelopeId)
        {
            var apiClient = new DocuSignClient(_dsConfig.BaseURI);
            apiClient.Configuration.DefaultHeader.Add("Authorization", "Bearer " + accessToken);
            EnvelopesApi envelopesApi = new EnvelopesApi(apiClient);

            var dummy = envelopesApi.GetEnvelope(_dsConfig.AccountID, envelopeId, new EnvelopesApi.GetEnvelopeOptions { include = "custom_fields,tabs,recipients,documents,attachments,extensions,workflow" });

            return "";
        }

        public static void UpdateExpiration(DSConfiguration _dsConfig, string accessToken, string envelopeId, DateTime expirationDate)
        {
            try
            {
                var apiClient = new DocuSignClient(_dsConfig.BaseURI);
                apiClient.Configuration.DefaultHeader.Add("Authorization", "Bearer " + accessToken);
                EnvelopesApi envelopesApi = new EnvelopesApi(apiClient);

                var dummy = envelopesApi.GetEnvelope(_dsConfig.AccountID, envelopeId, new EnvelopesApi.GetEnvelopeOptions { include = "custom_fields,tabs,recipients,documents,attachments,extensions,workflow" });
                var dummy3Pre = envelopesApi.GetNotificationSettings(_dsConfig.AccountID, envelopeId);

                var expirationSettings = new Expirations { ExpireEnabled = "true", ExpireAfter = "999", ExpireWarn = "998" };
                envelopesApi.UpdateNotificationSettings(_dsConfig.AccountID, envelopeId, new EnvelopeNotificationRequest { Expirations = expirationSettings });


                var dummy2 = envelopesApi.GetEnvelope(_dsConfig.AccountID, envelopeId, new EnvelopesApi.GetEnvelopeOptions { include = "custom_fields,tabs,recipients,documents,attachments,extensions,workflow" });
                var dummy3 = envelopesApi.GetNotificationSettings(_dsConfig.AccountID, envelopeId);
                var dummyLast = 1;
            }catch(Exception e)
            {
                var dummy = e.Message;
            }



        }
        public static void AddMetaData(DSConfiguration _dsConfig, string accessToken, string envelopeId)
        {
            try
            {
                var apiClient = new DocuSignClient(_dsConfig.BaseURI);
                apiClient.Configuration.DefaultHeader.Add("Authorization", "Bearer " + accessToken);
                EnvelopesApi envelopesApi = new EnvelopesApi(apiClient);

                var dummy = envelopesApi.GetEnvelope(_dsConfig.AccountID, envelopeId, new EnvelopesApi.GetEnvelopeOptions { include = "custom_fields,tabs,recipients,documents,attachments,extensions,workflow" });
                var textCustomField = new TextCustomField();
                textCustomField.FieldId = "1";
                textCustomField.Value = "TESTING TEXT CUSTOM FIELD VALUE";
                textCustomField.Name = "TESTING NAME OF TEXT CUSTOM FIELD";
                textCustomField.Show = "false";

                var listCustomField = new ListCustomField();
                listCustomField.FieldId = "1";
                listCustomField.Value = "TESTING LIST CUSTOM VALUE";
                listCustomField.Name = "TESTING NAME OF TEXT CUSTOM FIELD";
                listCustomField.Show = "false";

                var listFields = new CustomFields();
                listFields.TextCustomFields = new List<TextCustomField> { textCustomField };
                listFields.ListCustomFields = new List<ListCustomField> { listCustomField };
                
                //var fields = new CustomFields { TextCustomFields = listFields};
                envelopesApi.CreateCustomFields(_dsConfig.AccountID, envelopeId, listFields);

                var dummy2 = envelopesApi.GetEnvelope(_dsConfig.AccountID, envelopeId, new EnvelopesApi.GetEnvelopeOptions { include = "custom_fields,tabs,recipients,documents,attachments,extensions,workflow" });

                var dummyLast = 1;

            }
            catch (Exception e)
            {
                var dummy = e.Message;
            }
        }

    }
}