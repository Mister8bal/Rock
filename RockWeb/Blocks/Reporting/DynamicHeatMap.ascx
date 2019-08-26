<%@ Control Language="C#" AutoEventWireup="true" CodeFile="DynamicHeatMap.ascx.cs" Inherits="RockWeb.Blocks.Reporting.DynamicHeatMap" %>

<asp:UpdatePanel ID="upnlContent" runat="server" UpdateMode="Conditional">
    <ContentTemplate>

        <Rock:NotificationBox ID="nbConfigurationWarning" runat="server" NotificationBoxType="Warning" Visible="false" />

        <div class="panel panel-block">
            <div class="panel-heading">
                <h1 class="panel-title"><i class="fa fa-map-marker"></i>&nbsp;Dynamic Map</h1>
                <a class="btn btn-xs btn-default pull-right margin-l-sm" onclick="javascript: toggleOptions()"><i title="Options" class="fa fa-gear"></i></a>
            </div>
            <asp:Panel ID="pnlOptions" runat="server" Title="Options" CssClass="panel-body js-options" Style="display: none">
                <div class="row">
                    <div class="col-md-6">
                        <Rock:RockDropDownList ID="ddlUserDataView" runat="server" Label="Dataview" Help="Select the dataview to use to filter the results." Required="true" />
                        <Rock:CampusesPicker ID="cpCampuses" runat="server" Label="Campus Filter" Help="Select the campuses to narrow the results down to families with that home campus." Required="false" />
                        <Rock:GroupPicker ID="gpGroupToMap" runat="server" Label="Geo-fencing Group" Help="Select a Group to show the geofences for that group and it's child groups" />
                    </div>
                    <div class="col-md-6">
                        <Rock:RockCheckBox ID="cbShowCampusLocations" runat="server" Label="Show Campus Locations On Map" Checked="true" />
                        <Rock:RangeSlider ID="rsDataPointRadius" runat="server" MinValue="0" MaxValue="128" Text="32" Label="Radius" Help="The radius of influence for each data point, in pixels" />
                    </div>
                </div>

                <div class="actions">
                    <asp:LinkButton ID="btnApplyOptions" runat="server" Text="Apply" CssClass="btn btn-primary" OnClick="btn_ApplyOptionsClick" />
                </div>
            </asp:Panel>

            <div class="margin-all-md">
                <div class="pull-right js-heatmap-actions">
                    <asp:Panel ID="pnlPieSlicer" runat="server" CssClass="btn btn-default btn-xs js-createpieshape">
                        <i class='fa fa-pie-chart' title="Create pie slices from selected circle"></i>
                    </asp:Panel>
                    <asp:Panel ID="pnlSaveShape" runat="server" CssClass="btn btn-default btn-xs js-saveshape">
                        <i class='fa fa-floppy-o' title="Save selected shape to a named location"></i>
                    </asp:Panel>

                    <div class="btn btn-danger btn-xs js-deleteshape" style="display:none"><i class='fa fa-times' title="Delete selected shape"></i></div>
                </div>
            </div>
            <div class="panel-body">
                <asp:Literal ID="lMapStyling" runat="server" />

                <asp:Panel ID="pnlMap" runat="server">

                    <div id="map_wrapper">
                        <div id="map_canvas" class="mapping"></div>
                    </div>
                </asp:Panel>

                <asp:Literal ID="lMessages" runat="server" />
                <asp:Literal ID="lDebug" runat="server" />
            </div>
        </div>

        <asp:HiddenField ID="hfPolygonColors" runat="server" />
        <asp:HiddenField ID="hfCenterLatitude" runat="server" />
        <asp:HiddenField ID="hfCenterLongitude" runat="server" />
        <asp:HiddenField ID="hfZoom" runat="server" />

        <%-- Configuration Panel --%>
        <asp:Panel ID="pnlConfigure" runat="server" Visible="false">
            <Rock:ModalDialog ID="mdConfigure" runat="server" ValidationGroup="vgConfigure" OnSaveClick="mdConfigure_SaveClick">
                <Content>
                    <Rock:RockDropDownList ID="ddlBlockConfigDataView" runat="server" Label="Dataview" Help="Select the dataview to use to filter the results." Required="false" ValidationGroup="vgConfigure" />
                </Content>
            </Rock:ModalDialog>
        </asp:Panel>

        <script>
            /**
             * Utility for modifying hex color values
             */
            const pSBC=(p,c0,c1,l)=>{
                let r,g,b,P,f,t,h,i=parseInt,m=Math.round,a=typeof(c1)=="string";
                if(typeof(p)!="number"||p<-1||p>1||typeof(c0)!="string"||(c0[0]!='r'&&c0[0]!='#')||(c1&&!a))return null;
                if(!this.pSBCr)this.pSBCr=(d)=>{
                    let n=d.length,x={};
                    if(n>9){
                        [r,g,b,a]=d=d.split(","),n=d.length;
                        if(n<3||n>4)return null;
                        x.r=i(r[3]=="a"?r.slice(5):r.slice(4)),x.g=i(g),x.b=i(b),x.a=a?parseFloat(a):-1
                    }else{
                        if(n==8||n==6||n<4)return null;
                        if(n<6)d="#"+d[1]+d[1]+d[2]+d[2]+d[3]+d[3]+(n>4?d[4]+d[4]:"");
                        d=i(d.slice(1),16);
                        if(n==9||n==5)x.r=d>>24&255,x.g=d>>16&255,x.b=d>>8&255,x.a=m((d&255)/0.255)/1000;
                        else x.r=d>>16,x.g=d>>8&255,x.b=d&255,x.a=-1
                    }return x};
                h=c0.length>9,h=a?c1.length>9?true:c1=="c"?!h:false:h,f=pSBCr(c0),P=p<0,t=c1&&c1!="c"?pSBCr(c1):P?{r:0,g:0,b:0,a:-1}:{r:255,g:255,b:255,a:-1},p=P?p*-1:p,P=1-p;
                if(!f||!t)return null;
                if(l)r=m(P*f.r+p*t.r),g=m(P*f.g+p*t.g),b=m(P*f.b+p*t.b);
                else r=m((P*f.r**2+p*t.r**2)**0.5),g=m((P*f.g**2+p*t.g**2)**0.5),b=m((P*f.b**2+p*t.b**2)**0.5);
                a=f.a,t=t.a,f=a>=0||t>=0,a=f?a<0?t:t<0?a:a*P+t*p:0;
                if(h)return"rgb"+(f?"a(":"(")+r+","+g+","+b+(f?","+m(a*1000)/1000:"")+")";
                else return"#"+(4294967296+r*16777216+g*65536+b*256+(f?m(a*255):0)).toString(16).slice(1,f?undefined:-2)
            }

            function toggleOptions() {
                $('.js-options').slideToggle();
            }

            /**
            * Wrapper for the shape controls.  This is a type of
            * Google Maps custom overlay.  As such, it's prototype
            * must be google.maps.OverlayView
            * 
            */
            const HeatMapShapeControls = function(shape, content, map){

                this.shape_ = shape;
                this.content_ = content;
                this.map_ = map;

                // Define a property to hold the container div. We'll
                // actually create this div upon receipt of the onAdd()
                // method so we'll leave it null for now.
                this.div_ = null;

                // Explicitly call setMap on this overlay
                this.setMap(map);

                this.deleteControl = null;
                this.countLabel = null;
            
            }

            HeatMapShapeControls.prototype = new google.maps.OverlayView(); 

            HeatMapShapeControls.prototype.onAdd = function() {

                var div = document.createElement('div');
                div.style.border = 'none';
                div.style.borderWidth = '0px';
                div.style.position = 'absolute';
        
                this.div_ = div;
        
                // Add the element to the "overlayImage" pane.
                var panes = this.getPanes();
                panes.overlayImage.appendChild(this.div_);

            };

            HeatMapShapeControls.prototype.draw = function() {

                var overlayProjection = this.getProjection();
        
                let cntr = overlayProjection.fromLatLngToDivPixel(this.shape_.getCenter());
        
                // center the main element.
                var div = this.div_;
                div.innerHTML = '<span>'+this.content_.toString()+'</span>';
                let delEl = this.createDeleteElement();
                div.appendChild(this.createSaveElement());
                if(this.shape_.overlayType == 'circle'){
                    div.appendChild(this.createPieSliceElement());
                }
                div.appendChild(delEl);
                div.style.left = cntr.x - (div.offsetWidth/2) + 'px';
                div.style.top = cntr.y - (div.offsetHeight/2) + 'px';
                div.style.backgroundColor = "rgba(255,255,255,.5)";
                div.style.padding = '4px 7px';
                div.style.border = '1px solid #333';
                div.style.borderRadius = '20px';
                div.style.fillOpacity = '.5';


            };

            HeatMapShapeControls.prototype.remove = function() {
                if(!this.div_ || !this.div_.parentNode){
                    return;
                }
                this.div_.parentNode.removeChild(this.div_);
            };

            HeatMapShapeControls.prototype.update = function(content) {
                this.content_ = content;
                this.draw();
            };

            HeatMapShapeControls.prototype.createDeleteElement = function() {
                let out = document.createElement('a');
                out.innerHTML = '<i class="fa fa-times-circle-o" aria-hidden="true"></i>'
                out.style.color = 'red';
                out.style.marginLeft = '1rem';
                out.style.fontSize = '1.4rem';
                out.style.verticalAlign = "middle"
                google.maps.event.addDomListener(out,"click", event => {
                    google.maps.event.trigger(this,'delete-region');
                });
                return out;
            }

            HeatMapShapeControls.prototype.createSaveElement = function() {
                let out = document.createElement('a');
                out.innerHTML = '<i class="fa fa-floppy-o" title="Save shape to a named location"></i>'
                out.style.marginLeft = '1rem';
                out.style.fontSize = '1.4rem';
                out.style.verticalAlign = "middle"
                out.style.borderRadius = '10px';
                google.maps.event.addDomListener(out,"click", event => {
                    google.maps.event.trigger(this,'save-region');
                });
                return out;
            }

            HeatMapShapeControls.prototype.createPieSliceElement = function() {
                let out = document.createElement('a');
                out.innerHTML = '<i class="fa fa-pie-chart" title="Create a new pie slice"></i>'
                out.style.marginLeft = '1rem';
                out.style.fontSize = '1.4rem';
                out.style.verticalAlign = "middle"
                out.style.borderRadius = '10px';
                google.maps.event.addDomListener(out,"click", event => {
                    google.maps.event.trigger(this,'create-slice');
                });
                return out;
            }

            const HeatMapShape = function(parentObj, gmapShape = null, gmapOptions = null){

                /**
                 * Private variables
                 */
                let mapShapeObj = null;
                let mapShapeLabel = null;
                let mapCountLabel = null;
                let shapeControls = null;
                let name = "";
                let overlayType = null;
                let heatMap = null;
                let bounds;
                let pointCount = 0;
                let shapeCenter = null;
                let isSelected = false;
                let parentHeatMap = parentObj;
                let map = null;
                let slicerHandler = {
                    centerPt: null,
                    radius: null,
                    sliceCuts: [],
                    currentSlices: []
                }

                const pieSlicerState = {
                    SelectedCenterPt: null,
                    SelectedRadius: null,
                    SelectedPieCuts: [],
                    CurrentPieSlices: []
                };

                if(gmapShape){
                    mapShapeObj = gmapShape
                }else if(gmapOptions){
                    mapShapeObj = new google.maps.Polygon(gmapOptions);
                }else{
                    throw new Error("You must provide a valid Google Map Polygon options object or a valid Google Maps Shape object.");
                }

                map = mapShapeObj.getMap();

                // NOTE: bounds is the rectangle bounds of the shape (not the actual shape)
                bounds = mapShapeObj.getBounds();

                if (mapShapeObj.getCenter)
                {
                    shapeCenter = mapShapeObj.getCenter();
                }
                else
                {
                    shapeCenter = bounds.getCenter();
                }

                const getArchPath = (startAngle, endAngle)=>{
                    var point, previous,
                    atEnd = false,
                    points = Array(),
                    a = startAngle;
                    while (true) {
                        point = google.maps.geometry.spherical.computeOffset(that.getCenter(), mapShapeObj.radius, a);
                        points.push(point);
                        if (a == endAngle){
                            break;
                        }
                        a++;
                        if (a > 360) {
                            a = 1;
                        }
                    }
                    if (direction == 'counterclockwise') {
                        points = points.reverse();
                    }
                    return points;
                }

                const handleSliceMove = function(event){

                    let heading = google.maps.geometry.spherical.computeHeading(
                        that.getCenter(),
                        event.latLng
                    );

                    if(!slicerHandler.centerPt){
                        slicerHandler.centerPt = that.getCenter();
                    }

                    if(!slicerHandler.radius && that.overlayType == 'circle'){
                        slicerHandler.radius = mapShapeObj.getRadius();
                    }

                    if (heading < 0)
                    {
                        heading += 360;
                    }

                    let sliceCuts = [];
                    sliceCuts.push(heading);
                    slicerHandler.sliceCuts.forEach(function(sc){
                        sliceCuts.push(sc);
                    });

                    sliceCuts.sort(function(a,b){
                        return a-b;
                    });

                    slicerHandler.currentSlices.forEach(function(cs){
                        cs.delete();
                    });

                    slicerHandler.currentSlices = [];


                    sliceCuts.forEach(function(sc,i){

                        var centerPt = slicerHandler.centerPt;
                        var radiusMeters = slicerHandler.radius;
                                        
                        var pieSlicePath = Array();

                        var nextRadialPoint = sc;
                        lastRadialPoint = sc;

                        if (i < sliceCuts.length-1){
                            // find the next arc starting point
                            lastRadialPoint = sliceCuts[i+1];
                        }else{
                            // use the first arc of our currentPieCuts
                            lastRadialPoint = sliceCuts[0];

                            // make sure the pieshape colors don't flash to random colors as it is resized
                            polygonColorIndex = 0;
                        }

                        // if the start of the arc is counterclockwise from the current, move it back 360 degrees (because it is probably the last missing piece of the circle)
                        if (nextRadialPoint >= lastRadialPoint){
                            nextRadialPoint -= 360;
                        }
                                    
                        // create a Google Map Path as an array of all the lines from the center to the outer radius for every full degree to make it look like a pie slice
                        while (nextRadialPoint < lastRadialPoint) {
                            pieSlicePath.push(google.maps.geometry.spherical.computeOffset(centerPt, radiusMeters, nextRadialPoint));
                            nextRadialPoint += 1;
                        }
                            
                        // ensure that the last path of the pieslice is there for the last line of the path
                        var endArc = lastRadialPoint;

                        pieSlicePath.push(google.maps.geometry.spherical.computeOffset(centerPt, radiusMeters, endArc));
                            
                        // put the center point to the start and end of the pieSlicePath
                        pieSlicePath.unshift(centerPt);
                        pieSlicePath.push(centerPt);

                        var pieSlicePoly = new HeatMapShape(parentHeatMap,
                            new google.maps.Polygon({
                                path: pieSlicePath,
                                map: map,
                                fillColor: that.parentHeatMap.GetNextColor(),
                                fillOpacity: 0.6,
                                draggable: false,
                                editable: false,
                            })
                        );

                        pieSlicePoly.isPieDrawing = true;
                        pieSlicePoly.startArc = sc;
                        pieSlicePoly.overlayType = 'polygon';
                        pieSlicePoly.isPieSlice = true;
                        while (pieSlicePoly.startArc < 0){
                            pieSlicePoly.startArc += 360;
                        }

                        //that.parentHeatMap.AddUpdateShape(pieSlicePoly, false );
                    });

                }

                const pieMouseMoveListener = google.maps.event.addListener(map, 'mousemove', handleSliceMove);

                let that = {
                    name:"",
                    overlayType:null,
                    mapShapeLabel:mapShapeLabel,
                    mapCountLabel:mapCountLabel,
                    parentHeatMap:parentHeatMap,
                    radius:null,
                    handleShapeClick:function(){
                        AllShapes.forEach(function (s) {
                            if (s.mapCountLabel && s.mapCountLabel.text.startsWith("*"))
                            {
                                s.mapCountLabel.text = s.mapCountLabel.text.slice(1);
                                s.mapCountLabel.changed('text');
                            }
                        });
                        
                        mapCountLabel.text = '*' + mapCountLabel.text;
                        mapCountLabel.changed('text');
                    },
                    handleBoundsChange:function(){
                        this.update();
                    },
                    getGMShapeObject:()=>mapShapeObj,
                    processHeatMap:function(hm = null){

                        heatMap = hm || heatMap;

                        pointCount = 0;

                        let data = heatMap.data.getArray();

                        for (let i = 0; i<data.length; i++) {

                            let latLng = data[i];
                            let pointWeight = 1;
                            let point = latLng;
    
                            if (latLng.location)
                            {
                                // weighted location
                                point = latLng.location;
                                pointWeight = latLng.weight;
                            }

                            // first check if within bounds to narrow down (for performance)
                            if(!bounds.contains(point)){
                                continue;
                            }

                            if ('polygon' == this.overlayType) {
                                if (google.maps.geometry.poly.containsLocation(point, mapShapeObj)) {
                                    pointCount += pointWeight;
                                }
                            } else if ('circle' == this.overlayType) {
                                if (google.maps.geometry.spherical.computeDistanceBetween(shapeCenter, point) < mapShapeObj.radius) {
                                    pointCount += pointWeight;
                                }
                            } else if ('rectangle' == this.overlayType) {
                                pointCount += pointWeight;
                            }
                            
                        }

                    }, 
                    handlePieSliceClick:function () {

                        
                        
                    },
                    getCenter:function(){
                        return shapeCenter;
                    },
                    isSelectedShape:function(){
                        return isSelected;
                    },
                    update:function(){
                        that.processHeatMap(heatMap);
                        that.render();
                    },
                    setSelected:function(){
                        isSelected = true;
                    },
                    setUnselected:function(){
                        isSelected = false;
                    },
                    delete:function(){
                        mapCountLabel.remove();
                        mapShapeObj.setMap(null);
                    },
                    render:function(){

                        bounds = mapShapeObj.getBounds();
        
                        if (mapShapeObj.getCenter)
                        {
                            shapeCenter = mapShapeObj.getCenter();
                        }
                        else
                        {
                            shapeCenter = bounds.getCenter();
                        }

                        totalCount = pointCount;
                        mapLabel = totalCount.toString();

                        if (name){
                            mapShapeLabel = name + ': <span class="badge">' + mapLabel + '</span>';
                        }else{
                            mapShapeLabel = '<span class="badge">'+mapLabel+'</span>';
                        }
                        
                        if (!mapCountLabel) {
                            mapCountLabel = new HeatMapShapeControls(that, mapShapeLabel, map);
                        }else{
                            mapCountLabel.update(mapShapeLabel);
                            mapCountLabel.draw();
                        }

                        mapCountLabel.addListener("delete-region", ()=>{
                            parentHeatMap.DeleteShape(this);
                        });

                        mapCountLabel.addListener('create-slice', ()=>{
                            this.handlePieSliceClick();
                        });

                        mapShapeObj.setMap(mapShapeObj.getMap());
                    }
                }
                
                mapShapeObj.addListener('bounds_changed', that.handleBoundsChange.bind(that));

                return that;
            }

            const HeatMap = function( canvasId ) {

                // if this is an async postback, the map is already created, so just break out
                if ($('#map_canvas').data("googleMap"))
                {
                    return;
                }

                // hook into rangeslider
                let rangeSlider = $('#<%=rsDataPointRadius.ClientID%>');

                /**
                 * Private vars 
                 */
                
                let map;
                let mapCanvas;

                let heatMap;

                let mapStyle = <%=this.StyleCode%>;
                let polygonColorIndex = 0;
                let polygonColors = $('#<%=hfPolygonColors.ClientID%>').val().split(',');

                let lat = Number($('#<%=hfCenterLatitude.ClientID%>').val());
                let long = Number($('#<%=hfCenterLongitude.ClientID%>').val());
                let zoom = Number($('#<%=hfZoom.ClientID%>').val());
                let centerLatLng = null;
                
                // Set default map options
                let mapOpts = {
                    mapTypeId: 'roadmap',
                    styles: mapStyle,
                    center: centerLatLng,
                    zoom: zoom,
                    streetViewControl: false
                }

                var groupId = <%=this.GroupId ?? 0 %>;

                const heatMapData = [<%=this.HeatMapData%>]

                const heatMapBounds = new google.maps.LatLngBounds();

                const AllShapes = [];
                let SelectedShape; 

                let campusMarkersData = [<%=this.CampusMarkersData%>]

                // Instantiate markers array to track campus markers.
                const campusMarkers = [];

                const pinImage = new google.maps.MarkerImage(
                    '//chart.googleapis.com/chart?chst=d_map_pin_letter&chld=%E2%80%A2|' + 'FE7569',
                    new google.maps.Size(21, 34),
                    new google.maps.Point(0,0),
                    new google.maps.Point(10, 34)
                );

                const pinShadow = new google.maps.MarkerImage(
                    '//chart.googleapis.com/chart?chst=d_map_pin_shadow',
                    new google.maps.Size(40, 37),
                    new google.maps.Point(0, 0),
                    new google.maps.Point(12, 35)
                );

                let initialColor;

                /**
                 * Private Methods 
                 */ 
                 const handleRangeSliderChange = (obj) => {
                    var newRadius = parseInt($(this).val());
                    if (heatmap) {
                        heatmap.set('radius', newRadius);
                    }
                }

                const handleShapeChange = function(modifiedShape){
                    AllShapes.forEach(function(s){
                        let gmo = s.getGMShapeObject();
                        if(gmo == modifiedShape){
                            s.update();
                        }
                    });
                }

                rangeSlider.on("change", handleRangeSliderChange);

                let that = {
                    drawingManager:new google.maps.drawing.DrawingManager({
                        drawingMode: null,
                        drawingControl: true,
                        drawingControlOptions: {
                            position: google.maps.ControlPosition.TOP_CENTER,
                            drawingModes: [
                                google.maps.drawing.OverlayType.CIRCLE,
                                google.maps.drawing.OverlayType.POLYGON,
                                google.maps.drawing.OverlayType.RECTANGLE
                            ]
                        },
                        circleOptions: {
                            draggable: true,
                            editable: true,
                            fillColor: initialColor,
                            strokeColor: initialColor
                        },
                        polygonOptions: {
                            draggable: true,
                            editable: true,
                            fillColor: initialColor,
                            strokeColor: initialColor,
                            strokeWeight: 2
                        },
                        polylineOptions: {
                            draggable: true,
                            editable: true,
                            fillColor: initialColor,
                            strokeColor: initialColor
                        },
                        rectangleOptions: {
                            draggable: true,
                            editable: true,
                            fillColor: initialColor,
                            strokeColor: initialColor
                        }
                    })
                }

                that.loadGroups = ()=>{
                    
                    if(!groupId){
                        return;
                    }

                    $.get( Rock.settings.get('baseUrl') + 'api/Groups/GetMapInfo/' + groupId, function( mapItems ) {
                        $.each(mapItems, function (i, mapItem) {
                            that.addGroupGeoFence(mapItem);
                        });
                    });

                    // Get Child Groups
                    $.get( Rock.settings.get('baseUrl') + 'api/Groups/GetMapInfo/' + groupId + '/Children', function( mapItems ) {
                        $.each(mapItems, function (i, mapItem) {
                            that.addGroupGeoFence(mapItem);
                        });
                    });
                    
                }

                // if a GroupId was specified, show geofences
                that.addGroupGeoFence = function (mapItem){

                    if (typeof mapItem.PolygonPoints !== 'undefined' && mapItem.PolygonPoints.length > 0) {

                        let geoFencePath = Array();

                        $.each(mapItem.PolygonPoints, function(j, point) {
                            geoFencePath.push(new google.maps.LatLng(point.Latitude, point.Longitude));
                        });

                        let geoFencePoly = new HeatMapShape(
                            that,
                            null,
                            {
                                path: geoFencePath,
                                map: map,
                                fillColor: that.GetNextColor(),
                                fillOpacity: 0.6,
                                draggable: false,
                                editable: false,
                            }
                        );

                        geoFencePoly.name = mapItem.name;
                        geoFencePoly.overlayType = 'polygon';

                        that.AddUpdateShape(geoFencePoly);
                    }
                }

                that.DeleteShape = function(shape) {

                    let allShapesIndex = AllShapes.indexOf(shape);
                                
                    if (allShapesIndex > -1)
                    {
                        AllShapes.splice(allShapesIndex, 1);

                        if (AllShapes.length == 0)
                        {
                            $('.js-deleteshape').hide();
                        }

                    }
                                
                    shape.delete();

                    shape = null;

                };

                that.GetNextColor = () => {

                    if (polygonColors && polygonColors.length) {

                        if (polygonColorIndex >= polygonColors.length) {
                            polygonColorIndex = 0;
                        }

                        return polygonColors[polygonColorIndex++];
                    }

                    return null;
                }

                
                that.AddShape = function(shape){
                    if(!(shape instanceof HeatMapShape)){
                        throw new TypeError('Invalid Object Type:  Shape must be if type HeatMapShape.');
                    }
                }

                that.AddUpdateShape = function (shape, justUpdate) {

                    SelectedShape = shape;

                    let gmShape = shape.getGMShapeObject();
                    if (!justUpdate) {

                        //google.maps.event.addListener(gmShape, 'click', selectedSHape.handleShapeClick);

                        // set the color of the next shape
                        if (polygonColors && polygonColors.length) {
                            var color = that.GetNextColor();

                            this.drawingManager.polygonOptions.fillColor = color;
                            this.drawingManager.polygonOptions.strokeColor = color;
                            this.drawingManager.circleOptions.fillColor = color;
                            this.drawingManager.circleOptions.strokeColor = color;
                            this.drawingManager.rectangleOptions.fillColor = color;
                            this.drawingManager.rectangleOptions.strokeColor = color;
                        }

                        AllShapes.push(shape);
                        $('.js-deleteshape').show();
                    }

                    SelectedShape.processHeatMap(heatmap);
                    SelectedShape.render();

                  
                }

                that.initMap = ()=>{

                    const _this = this;
                    centerLatLng = new google.maps.LatLng(lat,long );
                    initialColor = that.GetNextColor();

                    mapCanvas = document.getElementById(canvasId);

                    if(!mapCanvas){
                        throw new Error('Invalid Map canvas object');
                    }
                
                    // Display a map on the page
                    map = new google.maps.Map(mapCanvas, mapOpts);

                    map.setTilt(45);
                    map.setCenter(centerLatLng);

                    campusMarkersData.forEach( function (c) {
                        campusMarkers.push(
                            new google.maps.Marker({
                                position: c.location,
                                map: map,
                                title: c.campusName,
                                icon: pinImage,
                                shadow: pinShadow
                            })
                        );
                    });

                    heatMapData.forEach(function (a) {
                        heatMapBounds.extend(a.location || a);
                    });

                    heatmap = new google.maps.visualization.HeatmapLayer({
                        dissipating: true,
                        data: heatMapData,
                        maxIntensity: 50,
                        radius: <%=this.DataPointRadius%>,
                    });

                    heatmap.setMap(map);

                    that.loadGroups();
                    
                    that.drawingManager.setMap(map);

                    google.maps.event.addListener(that.drawingManager, 'overlaycomplete', function (event) {
                        var shape = new HeatMapShape(that,event.overlay);
                        shape.overlayType = event.type;
                        shape.setSelected();
                        that.AddUpdateShape(shape, false);
                    });

                    google.maps.event.addListener(that.drawingManager, 'polygoncomplete', function (polygon) {
                        google.maps.event.addListener(polygon, 'dragend', function (a,b,c) {
                            handleShapeChange(polygon);
                        });
                        google.maps.event.addListener(polygon.getPath(), 'insert_at', function (a,b,c) {
                            handleShapeChange(polygon);
                        });
                        google.maps.event.addListener(polygon.getPath(), 'set_at', function (a,b,c) {
                            handleShapeChange(polygon);
                        });
                    });

                    $('.js-saveshape').click(function () {

                        if (SelectedShape) {
                            
                            var geoFencePath;
                            if (typeof(SelectedShape.getPaths) != 'undefined') {
                                
                                var coordinates = new Array();
                                var vertices = SelectedShape.getPaths().getAt(0);
                                // Iterate over the vertices of the shape's path
                                for (var i = 0; i < vertices.length; i++) {
                                    var xy = vertices.getAt(i);
                                    coordinates[i] = xy.toUrlValue();
                                }
    
                                // if the last coor is not already the first, then
                                // add the first vertex to the end of the path.
                                if (coordinates[coordinates.length - 1] != coordinates[0]) {
                                    coordinates.push(coordinates[0]);
                                }
    
                                geoFencePath = coordinates.join('|');
                            } else if ('rectangle' == SelectedShape.overlayType)
                            {
                                var ne = SelectedShape.getBounds().getNorthEast();
                                var sw = SelectedShape.getBounds().getSouthWest();
    
                                geoFencePath = ne.toUrlValue() + '|' + sw.lat() + ',' + ne.lng() + '|' + sw.toUrlValue() + '|' + ne.lat() + ',' + sw.lng() + ' | ' + ne.toUrlValue();
                            } else if ('circle' == SelectedShape.overlayType)
                            {
                                var center = SelectedShape.getCenter();
                                var radius = SelectedShape.radius;
                                geoFencePath = 'CIRCLE|' + center.lng() + ' ' + center.lat() + '|' + radius;
                            }
    
                            $('#<%=hfLocationSavePath.ClientID%>').val(geoFencePath);
                            
                            Rock.controls.modal.showModalDialog($('#<%=mdSaveLocation.ClientID%>').find('.rock-modal'), '#<%=upSaveLocation.ClientID%>');
                        }
                    });

                    $('.js-createpieshape').click();
                    
                }

                return that;

            }

            const hm = new HeatMap('map_canvas');

            Sys.Application.add_load(function () {
                
                hm.initMap();

            });

            // extend polygon to getBounds
            if (!google.maps.Polygon.prototype.getBounds) {
                google.maps.Polygon.prototype.getBounds = function(latLng) {
                    var bounds = new google.maps.LatLngBounds();
                    var paths = this.getPaths();
                    var path;
                    for (var p = 0; p < paths.getLength(); p++) {
                        path = paths.getAt(p);
                        for (var i = 0; i < path.getLength(); i++) {
                            bounds.extend(path.getAt(i));
                        }
                    }
 
                    return bounds;
                }
            } 

            // extend polygon to getBounds
            if (!google.maps.Polyline.prototype.getBounds) {
                google.maps.Polyline.prototype.getBounds = function() {
                    var bounds = new google.maps.LatLngBounds();
                    this.getPath().forEach(function(e) {
                        bounds.extend(e);
                    });
                    return bounds;
                }
            };

        </script>

    </ContentTemplate>
</asp:UpdatePanel>


<asp:UpdatePanel ID="upSaveLocation" runat="server">
    <ContentTemplate>
        <Rock:HiddenFieldWithClass ID="hfLocationSavePath" runat="server" CssClass="js-savelocation-value" />
        <Rock:HiddenFieldWithClass ID="hfLocationId" runat="server" CssClass="js-savelocationid" />

        <script>
            function saveLocationGeofence() {
                var locationId = $('#<%=lpLocation.ClientID%> .js-item-id-value').val();
                var locationName = $('#<%=lpLocation.ClientID%> .js-item-name-value').val();
                $('#<%=hfLocationId.ClientID%>').val(locationId);
                window.location = "javascript:__doPostBack('<%=upSaveLocation.ClientID%>')";

                var map = $('#map_canvas').data().googleMap;

                SelectedShape.Name = locationName;
                map.AddUpdateShape(SelectedShape, true);
            }
        </script>

        <%-- Save Shape to Location --%>
        <Rock:ModalDialog ID="mdSaveLocation" runat="server" CssClass="js-savelocation-modal" ValidationGroup="vgSaveLocation" OnOkScript="saveLocationGeofence();" Visible="true">
            <Content>
                <Rock:LocationItemPicker ID="lpLocation" runat="server" AllowMultiSelect="false" />
            </Content>
        </Rock:ModalDialog>
        
    </ContentTemplate>
</asp:UpdatePanel>
