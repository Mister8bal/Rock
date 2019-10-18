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
                <!--<div class="pull-right js-heatmap-actions">
                    <asp:Panel ID="pnlPieSlicer" runat="server" CssClass="btn btn-default btn-xs js-createpieshape">
                        <i class='fa fa-pie-chart' title="Create pie slices from selected circle"></i>
                    </asp:Panel>
                    <asp:Panel ID="pnlSaveShape" runat="server" CssClass="btn btn-default btn-xs js-saveshape">
                        <i class='fa fa-floppy-o' title="Save selected shape to a named location"></i>
                    </asp:Panel>

                    <div class="btn btn-danger btn-xs js-deleteshape" style="display:none"><i class='fa fa-times' title="Delete selected shape"></i></div>
                
                </div>-->
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
           function toggleOptions() {
                $('.js-options').slideToggle();
            }

            /**
            * Wrapper for the shape controls.  This is a type of
            * Google Maps custom overlay.  As such, it's prototype
            * must be google.maps.OverlayView
            * 
            */
            const HeatMapShapeControls = function(shape, count, shapeName, map){

                this.shape_ = shape;
                this.count_ = count;
                this.shapeName_ = shapeName;
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

                if(!this.div_){
                    var div = document.createElement('div');

                    div.style.border = 'none';
                    div.style.borderWidth = '0px';
                    div.style.position = 'absolute';
            
                    this.div_ = div;
                }
        
                // Add the element to the "overlayImage" pane.
                var panes = this.getPanes();
                panes.overlayImage.appendChild(this.div_);
                this.draw();

            };

            HeatMapShapeControls.prototype.handleProjectionReady = function() {

                var overlayProjection = this.getProjection();
        
                let cntr = overlayProjection.fromLatLngToDivPixel(this.shape_.getCenter());
        
                // center the main element.
                var div = this.div_;
                let controls = this.createControlsElement();

                controls.appendChild(this.createCountElement());
                controls.appendChild(this.createSaveElement());
                controls.appendChild(this.createDeleteElement());
               
                div.innerHTML = "";

               
                if(this.shapeName_.length){
                    div.appendChild(this.createLabelElement());
                }

                console.log("APPENDING CONTROLS", div);
                div.appendChild(controls);

                div.style.left = cntr.x - (div.offsetWidth/2) + 'px';
                div.style.top = cntr.y - (div.offsetHeight/2) + 'px';
                
            }

            HeatMapShapeControls.prototype.draw = function() {

                this.handleProjectionReady();

            };

            HeatMapShapeControls.prototype.onRemove = function(){
                if(!this.div_.parentNode){
                    this.div_ = null;
                    return;
                }
                console.log("REMOVING");
                this.div_.parentNode.removeChild(this.div_);
            }

            HeatMapShapeControls.prototype.remove = function() {
                this.map_ = null;
                this.setMap(this.map_);
            };

            HeatMapShapeControls.prototype.update = function(shape, count, shapeName, map) {
                this.shapeName_ = shapeName;
                this.count_ = count;
                this.shape_ = shape;
                this.map_ = shape.getMap();
                this.setMap(this.map_);
                //this.draw();
            };

            HeatMapShapeControls.prototype.createLabelElement = function(){
                let out = document.createElement('div');
                let l = document.createElement('span');
                l.className = 'badge';
                l.innerText = this.shapeName_;
                l.style.marginBottom = '1rem';
                out.style.textAlign = 'center';
                out.appendChild(l);
                return out;
            }

            HeatMapShapeControls.prototype.createCountElement = function(){
                let out = document.createElement('span');
                out.className = 'badge';
                out.innerText = this.count_;
                return out;
            }

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
                    console.log("SAVE");
                    google.maps.event.trigger(this,'save-region');
                });
                return out;
            }

            HeatMapShapeControls.prototype.createControlsElement = function() {
                let out = document.createElement('div');
                out.className = 'controls';
                out.style.backgroundColor = "rgba(255,255,255,.5)";
                out.style.padding = '4px 7px';
                out.style.border = '1px solid #333';
                out.style.borderRadius = '20px';
                out.style.fillOpacity = '.5';
                return out;
            }

            /**
             * Heat Map Shape. This is a wrapper object used 
             * to manage each shape created in the heatmap.
             * 
             * @param parentObj The object Heat Map that we're attaching the shape to.
             * @param gmapShape A google maps shape objec
             * @param gmapOptions Google map options.
             */
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

                let that = {
                    name:"",
                    overlayType:null,
                    mapShapeLabel:mapShapeLabel,
                    mapCountLabel:mapCountLabel,
                    parentHeatMap:parentHeatMap,
                    radius:null,
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
                    getCenter:function(){
                        return shapeCenter;
                    },
                    save:function(){

                        var geoFencePath;

                        // Assume this is a polygon
                        if (typeof (mapShapeObj.getPaths) != 'undefined') {

                            var coordinates = new Array();
                            var vertices = mapShapeObj.getPaths().getAt(0);

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

                        } else if (mapShapeObj.overlayType == 'rectangle'){
                                var ne = mapShapeObj.getBounds().getNorthEast();
                                var sw = mapShapeObj.getBounds().getSouthWest();
                                geoFencePath = ne.toUrlValue() + 
                                '|' + sw.lat() + ',' + ne.lng() + 
                                '|' + sw.toUrlValue() + 
                                '|' + ne.lat() + ',' + sw.lng() + 
                                ' | ' + ne.toUrlValue();
                        } 
    
                        $('#<%=hfLocationSavePath.ClientID%>').val(geoFencePath);
              
                        Rock
                        .controls
                        .modal
                        .showModalDialog(
                            parentHeatMap.saveModal, 
                                '#<%=upSaveLocation.ClientID%>'
                        );

                        
                    },
                    
                    /**
                     * Registers a function on the labels
                     * save-region event.  Value of this is assigned
                     * to the shape object.
                     *
                     * @param Function f: the function to be called when event is triggered
                     */
                    onSaveClick:function(f){
                        
                        let func = f.bind(this);
                        mapCountLabel.addListener('save-region',func);
                    },

                    /**
                     * Registers a function on the labels
                     * delete-region event.  Value of this is assigned
                     * to the shape object.
                     *
                     * @param Function f: the function to be called when event is triggered
                     */
                    onDeleteClick:function(f){
                        let func = f.bind(this);
                        mapCountLabel.addListener('delete-region',(ev)=>{
                            func(ev);
                        });
                    },

                    /**
                     * updates the shape data and re-triggers the render process. 
                     **/
                    update:function(){
                        that.processHeatMap(heatMap);
                        that.render();
                    },

                    /**
                     * Deletes the shape from the map.
                     **/
                    delete:function(){
                        mapCountLabel.remove();
                        mapShapeObj.setMap(null);
                    },

                    /**
                     * Sets the map for this shape
                     * 
                     * @param newMap Object Google Map.  A google maps object.
                     */
                    setMap:function(newMap){
                        map = newMap;
                    },

                    /**
                     * Returns the google map object this shape is attached to.
                     * 
                     * @return Object GoogleMap object.
                     **/
                    getMap:function(){
                        return map ? map : mapShapeObj.getMap();
                    },

                    /**
                     * RE-Renders the label for this shape object.
                     **/
                    redrawOverlay:function(){
                        
                        if(mapCountLabel){
                            mapCountLabel.remove();
                            mapCountLabel = null;
                        }
                        
                        mapCountLabel = new HeatMapShapeControls(that, mapShapeLabel, map);
                    },

                    /**
                     * Renders the shape object on the 
                     * google map.
                     * */
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
                        this.count = mapLabel;

                        new HeatMapShapeControls()
                        if (!mapCountLabel) {
                            mapCountLabel = new HeatMapShapeControls(that, mapLabel, this.name, map);
                        }else{
                            mapCountLabel.update(that, mapLabel, this.name, map);
                        }

                        mapCountLabel

                        mapShapeObj.setMap(map);

                    }
                }
                
                mapShapeObj.addListener('bounds_changed', that.handleBoundsChange.bind(that));

                return that;
            }

            /**
             * Wrapper object for the heatmap.
             * 
             * @param canvasId The id for the canvas where the map is displayed.
             */
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

                let polygonCompleteListener = null;
                let overlayCompleteListener = null;
                let initialColor;

                /**
                 * Private Methods 
                 */ 
                 const handleRangeSliderChange = (obj) => {
                    var newRadius = parseInt($(obj.target).val());
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
                                google.maps.drawing.OverlayType.POLYGON,
                                google.maps.drawing.OverlayType.RECTANGLE
                            ]
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

                that.allShapes = AllShapes;
                that.activeShape = null;
                that.saveModal = null;

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
                    }
                                
                    shape.delete();

                    shape = null;

                };

                that.GetNextColor = () => {

                    if(!polygonColors && !polygonColors.length){
                        return null;
                    }

                    if (polygonColorIndex >= polygonColors.length) {
                        polygonColorIndex = 0;
                    }

                    return polygonColors[polygonColorIndex++];
                   
                }

                that.hasShape = function (s) {

                    let out = false;

                    AllShapes.forEach(function(sh){
                        if(s == sh){
                            out = true;
                        }
                    });

                    return out;

                }

                that.AddUpdateShape = function (shape, justUpdate) {

                    if(!shape){
                        return;
                    }

                    if(that.hasShape(shape)){
                        return;
                    }

                    SelectedShape = shape;

                    let gmShape = shape.getGMShapeObject();

                    if (!justUpdate) {

                        // set the color of the next shape
                        if (polygonColors && polygonColors.length) {
                            var color = that.GetNextColor();
                            this.drawingManager.polygonOptions.fillColor = color;
                            this.drawingManager.polygonOptions.strokeColor = color;
                            this.drawingManager.rectangleOptions.fillColor = color;
                            this.drawingManager.rectangleOptions.strokeColor = color;
                        }
                        
                        AllShapes.push(shape);

                    }

                    SelectedShape.processHeatMap(heatmap);
                    SelectedShape.render();

                    //Now that we've rendered a new shape, 
                    //lets reset the drawing type controls.
                    this.drawingManager.setDrawingMode(null);
                  
                }

                that.handleOverlayComplete = function(event){

                    let shape = new HeatMapShape(that,event.overlay);

                    shape.overlayType = event.type;

                    that.AddUpdateShape(shape, false);

                    shape.onDeleteClick(()=>{
                        that.DeleteShape(shape);
                    });

                    //This will be the value of the HeatMapShape object.
                    shape.onSaveClick(function(ev){
                        console.log("ON SAVE CLICK",this);
                        this.save();
                        that.activeShape = this;
                    });

                }

                that.initMap = ()=>{

                    const _this = this;
                    centerLatLng = new google.maps.LatLng(lat,long );
                    initialColor = that.GetNextColor();
                    mapCanvas = document.getElementById(canvasId);

                    that.saveModal = document.getElementById('mdSaveLocation');
                    that.saveModal = $(that.saveModal).find('.rock-modal');
                    
                    if(!mapCanvas){
                        throw new Error('Invalid Map canvas object');
                    }
                
                    // Display a map on the page
                    map = new google.maps.Map(mapCanvas, mapOpts);

                    map.setTilt(45);
                    map.setCenter(centerLatLng);

                    campusMarkersData.forEach( c => {
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

                    google.maps.event.removeListener(overlayCompleteListener);
                    google.maps.event.removeListener(polygonCompleteListener);

                    overlayCompleteListener = google.maps.event.addListener(that.drawingManager, 'overlaycomplete', that.handleOverlayComplete);

                    polygonCompleteListener = google.maps.event.addListener(that.drawingManager, 'polygoncomplete', function (polygon) {
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

                    AllShapes.forEach(function(s){
                        s.setMap(map);
                        //We have to redraw the overlay
                        s.redrawOverlay();
                        s.render();
                    });

                    $('.js-createpieshape').click();
                    
                }

                that.saveLocationGeofence = function() {

                    var locationId = $('#<%=lpLocation.ClientID%> .js-item-id-value').val();
                    var locationName = $('#<%=lpLocation.ClientID%> .js-item-name-value').val();
                    $('#<%=hfLocationId.ClientID%>').val(locationId);
                    window.location = "javascript:__doPostBack('<%=upSaveLocation.ClientID%>')";
    
                    that.activeShape.name = locationName;
                    that.activeShape.update();
                    that.activeShape = null;
                }

                return that;

            }

            /**
             * Instantiate a new Heatmap object.
             * */
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

        <%-- Save Shape to Location --%>
        <Rock:ModalDialog ID="mdSaveLocation" runat="server" ClientIDMode="static" CssClass="js-savelocation-modal" ValidationGroup="vgSaveLocation" OnOkScript="hm.saveLocationGeofence();" Visible="true">
            <Content>
                <Rock:LocationItemPicker ID="lpLocation" runat="server" AllowMultiSelect="false" />
            </Content>
        </Rock:ModalDialog>
        
    </ContentTemplate>
</asp:UpdatePanel>
