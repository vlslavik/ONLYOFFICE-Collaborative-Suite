﻿<%@ Assembly Name="ASC.Web.Core" %>
<%@ Assembly Name="ASC.Web.Files" %>
<%@ Assembly Name="ASC.Web.Studio" %>

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="DocEditor.aspx.cs" Inherits="ASC.Web.Files.DocEditor" %>

<%@ Register TagPrefix="master" TagName="EditorScripts" Src="Masters/EditorScripts.ascx" %>
<%@ Register TagPrefix="sc" Namespace="ASC.Web.Studio.Controls.Common" Assembly="ASC.Web.Studio" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
    <meta http-equiv="X-UA-Compatible" content="IE=edge" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, minimum-scale=1.0, user-scalable=no" />
    <meta name="apple-mobile-web-app-capable" content="yes" />
    <meta name="apple-touch-fullscreen" content="yes" />

    <link rel="icon" href="~/favicon.ico" type="image/x-icon" />
    <title>ONLYOFFICE™</title>

    <style type="text/css">
        html {
            height: 100%;
            width: 100%;
        }

        body {
            background: #f4f4f4;
            color: #111;
            font-family: Arial, Tahoma,sans-serif;
            font-size: 12px;
            font-weight: normal;
            height: 100%;
            margin: 0;
            padding: 0;
            text-decoration: none;
        }

        div {
            margin: 0;
            padding: 0;
        }
    </style>

</head>
<body class="<%= IsMobile ? "mobile" : "" %>">
    <form id="form1" runat="server">
        <div id="wrap">
            <div id="iframeEditor"></div>
        </div>

        <%= RenderCustomScript() %>

        <master:EditorScripts runat="server" />
        <sc:InlineScript ID="InlineScripts" runat="server" />

        <% if (ItsTry)
           { %>
        <asp:PlaceHolder runat="server" ID="CommonPlaceHolder" />
        <% } %>

        <script language="javascript" type="text/javascript" src="<%= DocServiceApiUrl %>"></script>
        
        <% if (AddCustomScript)
           { %>
        <script type="text/javascript">
            try {
                if (window._gat) {
                    _gaq.push(['_setAccount', 'UA-12442749-17']);
                    _gaq.push(['_trackPageview', 'Try_Docs']);
                }
            } catch (err) {
            }
        </script>
        <% } %>
    </form>
</body>
</html>
