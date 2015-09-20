<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="LogImporter.aspx.cs" Inherits="LogImporter.LogImporter" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
    <link rel="stylesheet" href="Content/bootstrap.css" />
    <link rel="stylesheet" href="//code.jquery.com/ui/1.11.4/themes/smoothness/jquery-ui.css">
    <script src="Scripts/bootstrap.js"></script>
    <script src="Scripts/jquery-1.9.1.js"></script>
    <script src="Scripts/jquery-ui-1.11.4.js"></script>
    <script>
  $(function() {
    var availableTags = [
      "ActionScript",
      "AppleScript",
      "Asp",
      "BASIC",
      "C",
      "C++",
      "Clojure",
      "COBOL",
      "ColdFusion",
      "Erlang",
      "Fortran",
      "Groovy",
      "Haskell",
      "Java",
      "JavaScript",
      "Lisp",
      "Perl",
      "PHP",
      "Python",
      "Ruby",
      "Scala",
      "Scheme"
    ];
    function split( val ) {
      return val.split( /,\s*/ );
    }
    function extractLast( term ) {
      return split( term ).pop();
    }
 
    $( "#tags" )
      // don't navigate away from the field on tab when selecting an item
      .bind( "keydown", function( event ) {
        if ( event.keyCode === $.ui.keyCode.TAB &&
            $( this ).autocomplete( "instance" ).menu.active ) {
          event.preventDefault();
        }
      })
      .autocomplete({
        minLength: 0,
        source: function( request, response ) {
          // delegate back to autocomplete, but extract the last term
          response( $.ui.autocomplete.filter(
            availableTags, extractLast( request.term ) ) );
        },
        focus: function() {
          // prevent value inserted on focus
          return false;
        },
        select: function( event, ui ) {
          var terms = split( this.value );
          // remove the current input
          terms.pop();
          // add the selected item
          terms.push( ui.item.value );
          // add placeholder to get the comma-and-space at the end
          terms.push( "" );
          this.value = terms.join( ", " );
          return false;
        }
      });
  });
  </script>
</head>
<body>
    <form id="form1" runat="server" class="container">
        <nav class="navbar navbar-default">
            <!-- Brand and toggle get grouped for better mobile display -->
            <div class="navbar-header">
                <a class="navbar-brand" href="#">Log Importer</a>
            </div>
        </nav>
        <br />
        <div class="form-group col-md-6">
            <label for="tags">Tag programming languages: </label>
            <input id="tags" class="form-control" />
        </div>
        <div class="form-group col-md-6">
            <label for="tags">Tag programming languages: </label>
            <input id="tags" class="form-control" />
        </div>

        <asp:GridView ID="grdMediaDetails" runat="server" CssClass="table table-hover table-striped" AllowSorting="True" AutoGenerateColumns="False" OnSorting="grdMediaDetails_Sorting">
            <Columns>
                <asp:BoundField DataField="ModId" HeaderText="ID" Visible="false" />
                <asp:BoundField DataField="Station" HeaderText="Station" SortExpression="Station" />
                <asp:BoundField DataField="Client" HeaderText="Client" SortExpression="Client" />
                <asp:BoundField DataField="EType" HeaderText="E Type" SortExpression="EType" />
                <asp:BoundField DataField="ISCI" HeaderText="ISCI" SortExpression="ISCI" />
                <asp:BoundField DataField="AirDate" HeaderText="Air Date" DataFormatString="{0:M/dd/yyyy}" HtmlEncode="false" SortExpression="AirDate" />
                <asp:BoundField DataField="AirTime" HeaderText="Air Time" DataFormatString="{0:h:mm tt}" HtmlEncode="false" SortExpression="AirTime" />
                <asp:BoundField DataField="RATE" HeaderText="Rate" SortExpression="RATE" />
            </Columns>
        </asp:GridView>
        <br />
        <asp:FileUpload ID="fuCsvPath" runat="server" />
        <br />
        <asp:RadioButton ID="rbPreLog" runat="server" Checked="True" GroupName="LogType" Text="Pre Log" CssClass="radio-inline" />
        <asp:RadioButton ID="rbPostLog" runat="server" GroupName="LogType" Text="Post Log" CssClass="radio-inline" />
        <br />
        <asp:Button ID="btnImport" runat="server" Text="Import File" OnClick="btnImport_Click" CssClass="btn btn-default" />
        <asp:Button ID="btnReset" runat="server" Text="Reset" OnClick="btnReset_Click" CssClass="btn btn-default" />
        <br />
        <br />
        <asp:Button ID="btnSave" runat="server" Text="Save" OnClick="btnSave_Click" CssClass="btn btn-primary" />
        <br />
        <br />
        <asp:GridView ID="grdUnmatched" runat="server" CssClass="table table-hover table-striped" AllowSorting="True" AutoGenerateColumns="False">
            <Columns>
                <asp:BoundField DataField="Station" HeaderText="Station" />
                <asp:BoundField DataField="Client" HeaderText="Client" />
                <asp:BoundField DataField="ISCI" HeaderText="ISCI" />
                <asp:BoundField DataField="AirDate" HeaderText="Air Date" DataFormatString="{0:M/dd/yyyy}" HtmlEncode="false" />
                <asp:BoundField DataField="AirTime" HeaderText="Air Time" DataFormatString="{0:h:mm tt}" HtmlEncode="false" />
                <asp:BoundField DataField="RATE" HeaderText="Rate" />
            </Columns>
        </asp:GridView>
    </form>
</body>
</html>
