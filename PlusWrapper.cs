using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Added for auth flow
// Microsoft
using System.Diagnostics;
using System.Threading;

// DotNetOpenAuth
using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OAuth2;
//Google
using Google.Apis.Authentication.OAuth2;
using Google.Apis.Authentication.OAuth2.DotNetOpenAuth;
using Google.Apis.Util;
using System.Web;
// The generated plus class
using Google.Apis.Plus.v1;
using Google.Apis.Plus.v1.Data;
// For reading window titles
using System.Runtime.InteropServices;


/**
 * Here's how it works:
 * 
 * Authenticating the Google+ Service object and getting data.
 *   GPlusWrapper.PlusWrapper pw = new GPlusWrapper.PlusWrapper();
 *   me = pw.Authenticate();
 * 
 * Using the wrapper to perform operations against public Google+ data.
 *   GPlusWrapper.PlusWrapper pw = new GPlusWrapper.PlusWrapper();
 *   ActivityFeed af = pw.Search("cats");
 */

namespace GPlusWrapper
{
    public class PlusWrapper
    {
        // The PlusService, generated from the Discovery API / doc
        protected PlusService plusService;

        // For testing
        Person me;

        // Used internally by the OAUTH client
        private IAuthorizationState _authstate;

        // Used for polling the active window for access code in installed client flows
        [DllImport("user32.dll")]
        static extern int GetForegroundWindow();
        [DllImport("user32.dll")]
        static extern int GetWindowText(int hWnd, StringBuilder text, int count);
        [DllImport("user32.dll")]
        static extern int SendMessage(int hWnd, uint Msg, int wParam, int lParam);

        // for closing the window
        public const int SC_CLOSE = 0xF060;
        public const uint WM_SYSCOMMAND = 0x0112;
        
        private string _accessToken; // access token, used for API calls and expires
        private string _refreshToken; // refresh token, used to generate fresh access tokens

        private static class ClientCredentials
        {
            // These come from the APIS console, https://code.google.com/apis/console
            static public string ClientID = "YOUR_CLIENT_ID";
            static public string ClientSecret = "YOUR_CLIENT_SECRET";
        }

        // CreateAuthenticator
        /// <summary>
        /// Creates the authenticator for the server-side flow.
        /// </summary>
        /// <returns>An authenticator to be used for queries</returns>
        private OAuth2Authenticator<WebServerClient> CreateAuthenticator()
        {
            // Register the authenticator.
            AuthorizationServerDescription description = GoogleAuthenticationServer.Description;

            if (description.AuthorizationEndpoint.AbsoluteUri.IndexOf("request_visible_actions") < 1)
            {
                string paramChar = (description.AuthorizationEndpoint.AbsoluteUri.IndexOf('?') > 0) ? "&" : "?";
                description.AuthorizationEndpoint = new Uri(description.AuthorizationEndpoint.AbsoluteUri + paramChar + "request_visible_actions=http://schemas.google.com/AddActivity");
            }

            if (description.AuthorizationEndpoint.AbsoluteUri.IndexOf("offline") < 1)
            {
                string paramChar = (description.AuthorizationEndpoint.AbsoluteUri.IndexOf('?') > 0) ? "&" : "?";
                description.AuthorizationEndpoint = new Uri(description.AuthorizationEndpoint.AbsoluteUri + paramChar + "access_type=offline");
            }

            var provider = new WebServerClient(description);
            provider.ClientIdentifier = ClientCredentials.ClientID;
            provider.ClientSecret = ClientCredentials.ClientSecret;

            var authenticator =
                new OAuth2Authenticator<WebServerClient>(provider, GetAuthorization) { NoCaching = true };
            return authenticator;
        }

        // GetAuthorization
        /// <summary>
        /// Gets the authorization object for the client-side flow
        /// </summary>
        /// <param name="client">The web server client used for authorization</param>
        /// <returns>An authorization state that can be used for API queries </returns>
        private IAuthorizationState GetAuthorization(WebServerClient client)
        {
            // If this user is already authenticated, then just return the auth state.
            IAuthorizationState state = _authstate;
            if (state != null)
            {
                return state;
            }

            // Check if an authorization request already is in progress.
            HttpRequestInfo reqinfo = new HttpRequestInfo(HttpContext.Current.Request);
            state = client.ProcessUserAuthorization(reqinfo);

            // Check to see if we have an access token and use that to generate the state.
            if (_accessToken != null)
            {
                state = CreateState(_accessToken, true);
                // Check to see if we have a refresh token and use that to get the auth state.
            }
            else if (_refreshToken != null)
            {
                state = CreateState(_refreshToken);
                bool worked = client.RefreshToken(state);
                if (state != null)
                {
                    return state;
                }
            }

            if (state != null && (!string.IsNullOrEmpty(state.AccessToken) || !string.IsNullOrEmpty(state.RefreshToken)))
            {
                // Store and return the credentials.
                HttpContext.Current.Session["AUTH_STATE"] = _authstate = state;
                _accessToken = state.AccessToken;
                _refreshToken = state.RefreshToken;
                return state;
            }

            // Otherwise do a new authorization request.
            string scope = "https://www.googleapis.com/auth/plus.login";
            OutgoingWebResponse response = client.PrepareRequestUserAuthorization(new[] { scope });
            response.Send(); // Will throw a ThreadAbortException to prevent sending another response.
            return null;
        }

        // CreateState
        /// <summary>
        /// Creates a state object from a refresh token or access token.
        /// </summary>
        /// <param name="refreshToken">The refresh token from an authorization response.</param>
        /// <returns>A generated authorization state.</returns>
        private IAuthorizationState CreateState(string token, bool isAccessToken = false)
        {
            string[] scopes = { PlusService.Scopes.PlusMe.GetStringValue() };
            IAuthorizationState state = new AuthorizationState(scopes);
            if (isAccessToken)
            {
                state.AccessToken = token;
            }
            else
            {
                state.RefreshToken = token;
            }
            return state;
        }

        // RefreshService
        /// <summary>
        /// Ensures that the current Plus service is authenticated and valid
        /// </summary>
        private void RefreshService()
        {
            // Create the service.
            if (plusService == null)
            {
                // Register the authenticator.
                var auth = CreateAuthenticator();
                plusService = new PlusService(auth);
                if (plusService != null)
                {
                    PeopleResource.GetRequest prgr = plusService.People.Get("me");
                    me = prgr.Fetch();
                }
            }
        }

        public Person Authenticate()
        {
            // Use the client to perform all of the Auth steps.
            RefreshService();

            // Now we should have the Plus service object, use it to perform a simple operation.
            return me;            
        }

        public ActivityFeed Search(String searchStr)
        {
            //refreshService();

            if (plusService != null)
            {
                ActivitiesResource.SearchRequest sr = plusService.Activities.Search(searchStr);
                ActivityFeed af = null;
                af = sr.Fetch();
                return af;
            }
            return null;
        }

        public ActivityFeed ListActivities(String userID)
        {
            //refreshService();

            if (plusService != null)
            {
                ActivitiesResource.Collection arc = new ActivitiesResource.Collection();
                ActivitiesResource.ListRequest lr = plusService.Activities.List(userID, arc);
                ActivityFeed af = null;
                af = lr.Fetch();

                return af;
            }
            return null;
        }

        public Moment WriteDemoMoment()
        {
            Moment body = new Moment();
            ItemScope target = new ItemScope();

            target.Id = "replacewithuniqueforaddtarget";
            target.Image = "http://www.google.com/s2/static/images/GoogleyEyes.png";
            target.Type = "";
            target.Description = "The description for the activity";
            target.Name = "An example of add activity";

            body.Target = target;
            body.Type = "http://schemas.google.com/AddActivity";

            MomentsResource.InsertRequest insert =
                new MomentsResource.InsertRequest(
                    plusService,
                    body,
                    "me",
                    MomentsResource.Collection.Vault);
            Moment m = insert.Fetch();
            return m;
        }

        public string GetDisconnectURL()
        {
            string revokeURL = "https://accounts.google.com/o/oauth2/revoke?token=";
            if (_accessToken != null)
            {
                revokeURL += _accessToken;
            }
            else if (_refreshToken != null)
            {
                revokeURL += _refreshToken;
            }
            return revokeURL;
        }
    }
}
