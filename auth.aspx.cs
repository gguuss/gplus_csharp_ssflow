using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using Google.Apis.Plus.v1.Data;

namespace GPlus_ServerSideFlow
{
    public partial class auth : System.Web.UI.Page
    {
        public Google.Apis.Plus.v1.Data.Person me;
        public string disconnectURL;

        protected void Page_Load(object sender, EventArgs e)
        {

            GPlusWrapper.PlusWrapper pw = new GPlusWrapper.PlusWrapper();
            if (Request.Params["code"] != null)
            {
              OAuthResponseObject response = ManualCodeExchanger.ExchangeCode(Request.Params["code"]);
              if (response != null) {
                pw.setTokens(response.access_token, response.refresh_token);  
              }
            }                                    
            me = pw.Authenticate();
            pw.ListActivities("me");
            Moment wrote = pw.WriteDemoMoment();
            disconnectURL = pw.GetDisconnectURL();
        }
    }
}