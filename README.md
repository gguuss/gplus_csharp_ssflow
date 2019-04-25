# WARNING

[Google+ API is shut down](https://developers.google.com/+/api-shutdown).

This code is rather stale.  For the latest examples of how to use C# with Google+, please start from the official repo:

https://github.com/googleplus/gplus-quickstart-csharp

# Google+ Server-side flow demo

This project demonstrates how to use the Google+ API using the server-side OAUTH v2 flow. 

## Configuration

Open the project in Visual Studio.

Edit the PlusWrapper.cs file to include your client id and client secret from the Google APIs console:
  https://code.google.com/apis/console
  
You need to have the Google+ service enabled.

Run the project and open auth.aspx.

Sign in to Google+ and then the server will authorize the client.

Demo API calls are performed, you can add break points to the code to see the data returned from Google+.
