<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="WebForm1.aspx.cs" Inherits="WebApplication1.WebForm1" %>

<!DOCTYPE html>

<!--https://mapbox.com/studio/styles/add-style/mapbox/cj5h27pou3sb42smkgo5jfnqd/
    MAPBOX STYLE URL: mapbox://styles/hawkie94/cjab4s4hg32122rpkgbhydi0k
    SHARE URL: https://api.mapbox.com/styles/v1/hawkie94/cjab4s4hg32122rpkgbhydi0k.html?fresh=true&title=true&access_token=pk.eyJ1IjoiaGF3a2llOTQiLCJhIjoiY2phYjRvc3c1MHVudTMybmllNTZocW1mYSJ9.I20LALbEgwpjXzMQxDDrIw#16.8/48.147382/17.102258/0
    MAPBOX access token: pk.eyJ1IjoiaGF3a2llOTQiLCJhIjoiY2phYjRvc3c1MHVudTMybmllNTZocW1mYSJ9.I20LALbEgwpjXzMQxDDrIw

<bounds minlat="48.0817000" minlon="16.9567000" maxlat="48.2368000" maxlon="17.3639000"/>
4326
-->
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Hawkie's shop navigator</title>
    <meta charset=utf-8/>
    <meta name='viewport' content='initial-scale=1,maximum-scale=1,user-scalable=no' />
    
    <link rel="stylesheet" href="https://api.mapbox.com/mapbox.js/v3.1.1/mapbox.css"/>
    <script src="https://api.mapbox.com/mapbox.js/v3.1.1/mapbox.js"></script>
    
    <link rel="stylesheet" type="text/css" href="style.css">
    <link rel="stylesheet" href="//code.jquery.com/ui/1.11.4/themes/smoothness/jquery-ui.css">
    <script src="http://code.jquery.com/jquery-latest.min.js"></script>
    <link rel="stylesheet" href="https://maxcdn.bootstrapcdn.com/bootstrap/3.2.0/css/bootstrap.min.css" />
    <link rel="stylesheet" href="https://maxcdn.bootstrapcdn.com/bootstrap/3.2.0/css/bootstrap-theme.min.css" />
    <link rel="stylesheet" href="https://ajax.googleapis.com/ajax/libs/jqueryui/1.11.4/themes/smoothness/jquery-ui.css">
    <script src="https://ajax.googleapis.com/ajax/libs/jquery/3.2.1/jquery.min.js" type="text/javascript"></script>
</head>

<body>
    <form id="form1" runat="server">
        <div id="menu">
            <h2>HAWKIE'S SHOP NAVIGATOR</h2>

            <div class="input-group"><asp:RadioButton GroupName="ChoiceRadioButtons" ID="WithoutRadius" runat="server" Text="Search by object distance" onclick="hideMarker()"/>
                <div class="sub">
                    <asp:Label ID="DistanceLabel" runat="server" Text="Search for distance (in metres):"></asp:Label>
                    <asp:TextBox ID="DistanceTextBox" runat="server" MaxLength="4">0</asp:TextBox>
                </div>
            </div>

            <div class="input-group"><asp:RadioButton GroupName="ChoiceRadioButtons" ID="WithRadius" runat="server" Text="Search within the given radius" onclick="showMarker()"/>
                <div class="sub">
                    <asp:Label ID="RadiusLabel" runat="server" Text="Search radius (in metres):"></asp:Label>
                    <asp:TextBox ID="RadiusTextBox" runat="server" MaxLength="5">0</asp:TextBox><br>
                    <asp:CheckBox ID="ShopsOnlyCheckBox" runat="server" Text="Don't search for bus stops"/>
                </div>
            </div>

            <div class="input-group">
                <asp:Label ID="filtersLabel" runat="server" Text="Choose categories:"></asp:Label>
                <nav id="filters" class="filter_ux"></nav>
            </div>
            
            <p><button id="executeScriptButton" onclick="updateMap(event)">Show Results</button></p>
        </div>
        <div id="map">
            <script>
                var aTypes = '';
                var checkBoxes = [];
                //var cbTypes = ['shoes', 'doityourself', 'clothes', 'beauty'];
                var cbTypes = ['computer', 'doityourself', 'electronics', 'hardware']
                
                var markerCoords = [48.146, 17.118];
                var map, markerPos;

                initMap();

                function initMap() {
                    L.mapbox.accessToken = 'pk.eyJ1IjoiaGF3a2llOTQiLCJhIjoiY2phYjRvc3c1MHVudTMybmllNTZocW1mYSJ9.I20LALbEgwpjXzMQxDDrIw';
                    map = L.mapbox.map('map', 'mapbox.streets');
                    map.setView([48.146, 17.118], 13.0);

                    markerPos = L.marker(markerCoords, {
                        icon: L.mapbox.marker.icon({
                            'marker-color': '#FF7F50',
                            'marker-size': 'large',
                            'marker-symbol': 'marker'
                        }),
                        draggable: true
                    }).bindPopup('<b> Current position </b>');

                    initCheckBoxes();

                    markerPos.on('dragend', refreshPos);
                    refreshPos();

                    document.getElementById("WithRadius").checked = false;
                    document.getElementById("WithoutRadius").checked = false;
                    //hideMarker();
                }
                function initCheckBoxes() {
                    for (var i = 0; i < cbTypes.length; i++) {
                        var item = filters.appendChild(document.createElement('div'));
                        var tempCheckBox = item.appendChild(document.createElement('input'));
                        var label = item.appendChild(document.createElement('label'));

                        tempCheckBox.type = 'checkbox';
                        tempCheckBox.id = cbTypes[i];
                        tempCheckBox.checked = false;

                        label.innerHTML = cbTypes[i];
                        label.setAttribute('for', cbTypes[i]);
                        checkBoxes.push(tempCheckBox);
                    }
                }

                function refreshPos() {
                    var pos1 = markerPos.getLatLng().lat;
                    var pos2 = markerPos.getLatLng().lng;
                    markerCoords = [pos1, pos2];

                    markerPos.bindPopup('<b> Current position </b><br>' + markerPos.getLatLng().lat + '<br>' + markerPos.getLatLng().lng);
                    //updateMap();
                }
                function updateMap(event) {
                    if (typeof(event) !== "undefined")
                        event.preventDefault();
                    aTypes = '';
                    for (var i = 0; i < checkBoxes.length; i++) {
                        if (checkBoxes[i].checked) {
                            aTypes += cbTypes[i] + ',';
                        }
                    }
                    aTypes = aTypes.substr(0, aTypes.length - 1);
                    getAndShowData();

                    return false;
                }
                
                function getAndShowData() {
                    var param_01 = checkBoxes[0].checked;
                    var param_02 = getInputJson();
                    if (checkSearchMode()) {
                        alert("Error: Search mode was not chosen!");
                        return;
                    }
                    if (checkShopCategories()) {
                        alert("Error: No shop category was chosen!")
                        return;
                    }

                    $.ajax({
                        type: "POST",
                        async: true,
                        processData: true,
                        cache: false,
                        url: 'WebForm1.aspx/GetGeoData',
                        data: getInputJson(),
                        contentType: 'application/json; charset=utf-8',
                        dataType: 'json',
                        success: function (data) {
                            try {
                                //alert(data);
                                var geojson = jQuery.parseJSON(data.d);
                                map.featureLayer.setGeoJSON(geojson);
                            }
                            catch (err) {
                                alert("Something went wrong!");
                                console.log(err.message);
                                console.log(data.d);
                            }
                        },
                        error: function (err) {
                            alert(err.message);
                            console.log(err.message);
                        }
                    });
                }

                function getInputJson() {
                    var searchCategory;
                    var numParam;
                    var busStopSearch = document.getElementById("ShopsOnlyCheckBox").checked;

                    if (document.getElementById("WithoutRadius").checked == true) {
                        searchCategory = 0;
                        numParam = document.getElementById("DistanceTextBox").value;
                    }
                    else {
                        searchCategory = 1;
                        numParam = document.getElementById("RadiusTextBox").value;
                    }

                    //--------------------------------------------------------------------------//
                    var finalString = '{';

                    finalString = finalString + '"searchCat":"' + searchCategory + '",';
                    finalString = finalString + '"numParameter":"' + numParam + '",';
                    finalString = finalString + '"busStopSearch":"' + busStopSearch + '",';
                    finalString = finalString + '"fieldsStr":"' + aTypes + '",';
                    finalString = finalString + '"lat":"' + markerPos.getLatLng().lat + '",';
                    finalString = finalString + '"lng":"' + markerPos.getLatLng().lng;
                    finalString = finalString + '"}';

                    return finalString;
                }
                function checkShopCategories() {
                    for (var i = 0; i < checkBoxes.length; i++)
                        if (checkBoxes[i].checked)
                            return false;
                    return true;
                }
                function checkSearchMode() {
                    if (document.getElementById("WithoutRadius").checked == true)
                        return false;
                    if (document.getElementById("WithRadius").checked == true)
                        return false;

                    return true;
                }

                function hideMarker() {
                    $("#RadiusTextBox").prop("disabled", true);
                    $("#DistanceTextBox").prop("disabled", false);
                    $("#ShopsOnlyCheckBox").prop("disabled", true);
                    markerPos.removeFrom(map);
                }
                function showMarker() {
                    $("#RadiusTextBox").prop("disabled", false);
                    $("#DistanceTextBox").prop("disabled", true);
                    $("#ShopsOnlyCheckBox").prop("disabled", false);
                    markerPos.addTo(map);
                }
                document.getElementById("RadiusTextBox").addEventListener("input", function () {
                    this.value = this.value.replace(/\D/g, '');
                }, true);
                document.getElementById("DistanceTextBox").addEventListener("input", function () {
                    this.value = this.value.replace(/\D/g, '');
                }, true);
            </script>
        </div>
    </form>
</body>
</html>
