<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="auth.aspx.cs" Inherits="GPlus_ServerSideFlow.auth" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>    
    <div>
        <div>Name: <%=me.DisplayName %></div>
        <div>Tagline: <%=me.Tagline%></div>
        <div><img src="<%=me.Image.Url%>" /></div>
    </div>
    <a href="<%=disconnectURL%>">Disconnect</a>
</body>
</html>