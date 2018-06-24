<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Creator.Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
    <style type="text/css">
    label 
    {
        margin: 5px;
    }
    table
    {
        margin: 5px; 
    }
    td
    {
        padding: 5px;
    }
    </style>
</head>
<body>
    <form id="form1" runat="server">
    <div>

    <fieldset>
    <legend>Criar sistema</legend>
        <label>Caminho Creator</label>
        [<asp:Label ID="deCaminho" runat="server" style="font-weight: 700"></asp:Label>]
        <br />
        <label>Nome do sistema</label>
        [<asp:Label ID="nmSistema" runat="server" style="font-weight: 700"></asp:Label>]
        <br />
        <label>ConnectionString</label>
        [<asp:Label ID="deConnectionString" runat="server" style="font-weight: 700"></asp:Label>]
        <br />
        <label>Prefixo das tabelas</label>
        [<asp:Label ID="dePrefixoTabela" runat="server" style="font-weight: 700"></asp:Label>]
    <br />    
        <asp:GridView ID="gvBanco" runat="server" onrowdatabound="gvBanco_RowDataBound">
        </asp:GridView>
    <br />
        <asp:Button ID="btCreate" runat="server" Text="Criar" 
            onclick="btCreate_Click" />
            </fieldset>
    </div>
    </form>
</body>
</html>
