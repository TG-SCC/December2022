using DocuSign.eSign.Client;
using System.Collections.Generic;
using static DocuSign.eSign.Client.Auth.OAuth;
using DocuSign.eSign.Api;
using DocuSign.eSign.Model;
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
            var apiClient = new ApiClient(basePath);
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

    }
}