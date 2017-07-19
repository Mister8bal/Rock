﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Web.UI;
using System.Web.UI.WebControls;
using church.ccv.Pastoral.Model;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.church_ccv.Pastoral
{
    /// <summary>
    /// Block for users to create, edit, and view counseling requests.
    /// </summary>
    [DisplayName( "Counseling Request Detail" )]
    [Category( "Pastoral" )]
    [Description( "Block for users to create, edit, and view Counseling requests." )]
    [SecurityRoleField( "Worker Role", "The security role to draw workers from", true, church.ccv.Utility.SystemGuids.Group.GROUP_COUNSELING_WORKERS )]
    [LinkedPage("Counseling Request Statement Page", "The page which summarises a counseling request for printing", false)]
    public partial class CounselingRequestDetail : RockBlock
    {
        #region Properties
        private List<int> DocumentsState { get; set; }
        #endregion

        #region ViewState and Dynamic Controls

        /// <summary>
        /// ViewState of CounselingResultInfos for Counseling Request
        /// </summary>
        /// <value>
        /// The state of the CounselingResultInfos for CounselingRequest.
        /// </value>
        public List<CounselingResultInfo> CounselingResultsState
        {
            get
            {
                List<CounselingResultInfo> result = ViewState["CounselingResultInfoState"] as List<CounselingResultInfo>;
                if ( result == null )
                {
                    result = new List<CounselingResultInfo>();
                }

                return result;
            }

            set
            {
                ViewState["CounselingResultInfoState"] = value;
            }
        }

        #endregion

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );
            gResults.DataKeyNames = new string[] { "TempGuid" };
            gResults.Actions.AddClick += gResults_AddClick;
            gResults.Actions.ShowAdd = true;
            gResults.IsDeleteEnabled = true;

            // Gets any existing results and places them into the ViewState
            CareRequest counselingRequest = null;
            int counselingRequestId = PageParameter( "CounselingRequestId" ).AsInteger();
            if ( !counselingRequestId.Equals( 0 ) )
            {
                counselingRequest = new Service<CareRequest>( new RockContext() ).Get( counselingRequestId );
            }

            if ( counselingRequest == null )
            {
                counselingRequest = new CareRequest { Id = 0 };
            }

            if ( ViewState["CounselingResultInfoState"] == null )
            {
                List<CounselingResultInfo> brInfoList = new List<CounselingResultInfo>();
                foreach ( CareResult counselingResult in counselingRequest.CareResults )
                {
                    CounselingResultInfo counselingResultInfo = new CounselingResultInfo();
                    counselingResultInfo.ResultId = counselingResult.Id;
                    counselingResultInfo.Amount = counselingResult.Amount;
                    counselingResultInfo.TempGuid = counselingResult.Guid;
                    counselingResultInfo.ResultSummary = counselingResult.ResultSummary;
                    counselingResultInfo.ResultTypeValueId = counselingResult.ResultTypeValueId;
                    counselingResultInfo.ResultTypeName = counselingResult.ResultTypeValue.Value;
                    brInfoList.Add( counselingResultInfo );
                }

                CounselingResultsState = brInfoList;
            }

            dlDocuments.ItemDataBound += DlDocuments_ItemDataBound;
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !Page.IsPostBack )
            {
                cpCampus.Campuses = CampusCache.All();
                ShowDetail( PageParameter( "CounselingRequestId" ).AsInteger() );

                if( !string.IsNullOrEmpty( GetAttributeValue( "CounselingRequestStatementPage" ) ) )
                {
                    lbPrint.Visible = true;
                }

            }
            else
            {
                var rockContext = new RockContext();
                CareRequest item = new Service<CareRequest>(rockContext).Get( hfCounselingRequestId.ValueAsInt());
                if (item == null )
                {
                    item = new CareRequest();
                }
                item.LoadAttributes();

                phAttributes.Controls.Clear();
                Rock.Attribute.Helper.AddEditControls( item, phAttributes, false, BlockValidationGroup, 2 );

                confirmExit.Enabled = true;
            }
        }


        /// <summary>
        /// Restores the view-state information from a previous user control request that was saved by the <see cref="M:System.Web.UI.UserControl.SaveViewState" /> method.
        /// </summary>
        /// <param name="savedState">An <see cref="T:System.Object" /> that represents the user control state to be restored.</param>
        protected override void LoadViewState( object savedState )
        {
            base.LoadViewState( savedState );

            DocumentsState = ViewState["DocumentsState"] as List<int>;
            if ( DocumentsState == null )
            {
                DocumentsState = new List<int>();
            }
        }

        /// <summary>
        /// Saves any user control view-state changes that have occurred since the last page postback.
        /// </summary>
        /// <returns>
        /// Returns the user control's current view state. If there is no view state associated with the control, it returns null.
        /// </returns>
        protected override object SaveViewState()
        {
            ViewState["DocumentsState"] = DocumentsState;

            return base.SaveViewState();
        }
        #endregion

        #region Events

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            ShowDetail( PageParameter( "CounselingRequestId" ).AsInteger() );
        }

        /// <summary>
        /// Handles the AddClick event of the gResults control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        protected void gResults_AddClick( object sender, EventArgs e )
        {
            ddlResultType.Items.Clear();
            ddlResultType.AutoPostBack = false;
            ddlResultType.Required = true;
            ddlResultType.BindToDefinedType( DefinedTypeCache.Read( new Guid( church.ccv.Utility.SystemGuids.DefinedType.CARE_RESULT_TYPE ) ), true );
            dtbResultSummary.Text = string.Empty;
            dtbAmount.Text = string.Empty;

            mdAddResult.Show();
        }

        /// <summary>
        /// Handles the RowSelected event of the gResults control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        protected void gResults_RowSelected( object sender, RowEventArgs e )
        {
            Guid? infoGuid = e.RowKeyValue as Guid?;
            List<CounselingResultInfo> resultList = CounselingResultsState;
            var resultInfo = resultList.FirstOrDefault( r => r.TempGuid == infoGuid );
            if ( resultInfo != null )
            {
                ddlResultType.Items.Clear();
                ddlResultType.AutoPostBack = false;
                ddlResultType.Required = true;
                ddlResultType.BindToDefinedType( DefinedTypeCache.Read( new Guid( church.ccv.Utility.SystemGuids.DefinedType.CARE_RESULT_TYPE ) ), true );
                ddlResultType.SetValue( resultInfo.ResultTypeValueId );
                dtbResultSummary.Text = resultInfo.ResultSummary;
                dtbAmount.Text = resultInfo.Amount.ToString();
                hfInfoGuid.Value = e.RowKeyValue.ToString();
                mdAddResult.Show();
            }
        }

        /// <summary>
        /// Handles the DeleteClick event of the gResult control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        protected void gResults_DeleteClick( object sender, RowEventArgs e )
        {
            Guid? infoGuid = e.RowKeyValue as Guid?;
            List<CounselingResultInfo> resultList = CounselingResultsState;
            var resultInfo = resultList.FirstOrDefault( r => r.TempGuid == infoGuid );
            if ( resultInfo != null )
            {
                resultList.Remove( resultInfo );
            }

            CounselingResultsState = resultList;
            BindGridFromViewState();
        }

        /// <summary>
        /// Handles the AddClick event of the mdAddResult control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        protected void btnAddResults_Click( object sender, EventArgs e )
        {
            int? resultType = ddlResultType.SelectedItem.Value.AsIntegerOrNull();
            List<CounselingResultInfo> counselingResultInfoViewStateList = CounselingResultsState;
            Guid? infoGuid = hfInfoGuid.Value.AsGuidOrNull();

            if ( infoGuid != null )
            {
                var resultInfo = counselingResultInfoViewStateList.FirstOrDefault( r => r.TempGuid == infoGuid );
                if ( resultInfo != null )
                {
                    resultInfo.Amount = dtbAmount.Text.AsDecimalOrNull();
                    resultInfo.ResultSummary = dtbResultSummary.Text;
                    if ( resultType != null )
                    {
                        resultInfo.ResultTypeValueId = resultType.Value;
                    }

                    resultInfo.ResultTypeName = ddlResultType.SelectedItem.Text;
                }
            }
            else
            {
                CounselingResultInfo counselingResultInfo = new CounselingResultInfo();

                counselingResultInfo.Amount = dtbAmount.Text.AsDecimalOrNull();

                counselingResultInfo.ResultSummary = dtbResultSummary.Text;
                if ( resultType != null )
                {
                    counselingResultInfo.ResultTypeValueId = resultType.Value;
                }

                counselingResultInfo.ResultTypeName = ddlResultType.SelectedItem.Text;
                counselingResultInfo.TempGuid = Guid.NewGuid();
                counselingResultInfoViewStateList.Add( counselingResultInfo );
            }

            CounselingResultsState = counselingResultInfoViewStateList;

            mdAddResult.Hide();
            pnlView.Visible = true;
            BindGridFromViewState();
        }

        /// <summary>
        /// Handles the Click event of the lbSave control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbSave_Click( object sender, EventArgs e )
        {
            if ( Page.IsValid )
            {
                RockContext rockContext = new RockContext();
                Service<CareRequest> counselingRequestService = new Service<CareRequest>( rockContext );
                Service<CareResult> counselingResultService = new Service<CareResult>( rockContext );

                CareRequest counselingRequest = null;
                int counselingRequestId = PageParameter( "CounselingRequestId" ).AsInteger();

                if ( !counselingRequestId.Equals( 0 ) )
                {
                    counselingRequest = counselingRequestService.Get( counselingRequestId );
                }

                if ( counselingRequest == null )
                {
                    counselingRequest = new CareRequest { Id = 0 };
                    counselingRequest.Type = CareRequest.Types.Counseling;
                }

                counselingRequest.FirstName = dtbFirstName.Text;
                counselingRequest.LastName = dtbLastName.Text;
                counselingRequest.Email = ebEmail.Text;
                counselingRequest.RequestText = dtbRequestText.Text;
                counselingRequest.ResultSummary = dtbSummary.Text;
                counselingRequest.CampusId = cpCampus.SelectedCampusId;
                counselingRequest.ProvidedNextSteps = dtbProvidedNextSteps.Text;
                
                if ( lapAddress.Location != null )
                {
                    counselingRequest.LocationId = lapAddress.Location.Id;
                }

                counselingRequest.RequestedByPersonAliasId = ppPerson.PersonAliasId;
                counselingRequest.WorkerPersonAliasId = ddlWorker.SelectedValue.AsIntegerOrNull();
                counselingRequest.ConnectionStatusValueId = ddlConnectionStatus.SelectedValue.AsIntegerOrNull();

                if ( dpRequestDate.SelectedDate.HasValue )
                {
                    counselingRequest.RequestDateTime = dpRequestDate.SelectedDate.Value;
                }

                counselingRequest.HomePhoneNumber = pnbHomePhone.Number;
                counselingRequest.CellPhoneNumber = pnbCellPhone.Number;
                counselingRequest.WorkPhoneNumber = pnbWorkPhone.Number;

                List<CounselingResultInfo> resultListUI = CounselingResultsState;
                var resultListDB = counselingRequest.CareResults.ToList();

                // remove any Counseling Results that were removed in the UI
                foreach ( CareResult resultDB in resultListDB )
                {
                    if ( !resultListUI.Any( r => r.ResultId == resultDB.Id ) )
                    {
                        counselingRequest.CareResults.Remove( resultDB );
                        counselingResultService.Delete( resultDB );
                    }
                }

                // add any Counseling Results that were added in the UI
                foreach ( CounselingResultInfo resultUI in resultListUI )
                {
                    var resultDB = resultListDB.FirstOrDefault( r => r.Guid == resultUI.TempGuid );
                    if ( resultDB == null )
                    {
                        resultDB = new CareResult();
                        resultDB.CareRequestId = counselingRequest.Id;
                        resultDB.Guid = resultUI.TempGuid;
                        counselingRequest.CareResults.Add( resultDB );
                    }

                    resultDB.Amount = resultUI.Amount;
                    resultDB.ResultSummary = resultUI.ResultSummary;
                    resultDB.ResultTypeValueId = resultUI.ResultTypeValueId;
                }

                if ( counselingRequest.IsValid )
                {
                    if ( counselingRequest.Id.Equals( 0 ) )
                    {
                        counselingRequestService.Add( counselingRequest );
                    }

                    // get attributes
                    counselingRequest.LoadAttributes();
                    Rock.Attribute.Helper.GetEditValues( phAttributes, counselingRequest );

                    rockContext.WrapTransaction( () =>
                    {
                        rockContext.SaveChanges();
                        counselingRequest.SaveAttributeValues( rockContext );
                    } );

                    // update related documents
                    var documentsService = new Service<CareDocument>( rockContext );

                    // delete any images that were removed
                    var orphanedBinaryFileIds = new List<int>();
                    var documentsInDb = documentsService.Queryable().Where( b => b.CareRequestId == counselingRequest.Id ).ToList();

                    foreach ( var document in documentsInDb.Where( i => !DocumentsState.Contains( i.BinaryFileId ) ) )
                    {
                        orphanedBinaryFileIds.Add( document.BinaryFileId );
                        documentsService.Delete( document );
                    }

                    // save documents
                    int documentOrder = 0;
                    foreach ( var binaryFileId in DocumentsState )
                    {
                        // Add or Update the activity type
                        var document = documentsInDb.FirstOrDefault( i => i.BinaryFileId == binaryFileId );
                        if ( document == null )
                        {
                            document = new CareDocument();
                            document.CareRequestId = counselingRequest.Id;
                            counselingRequest.Documents.Add( document );
                        }
                        document.BinaryFileId = binaryFileId;
                        document.Order = documentOrder;
                        documentOrder++;
                    }
                    rockContext.SaveChanges();

                    // redirect back to parent
                    var personId = this.PageParameter( "PersonId" ).AsIntegerOrNull();
                    var qryParams = new Dictionary<string, string>();
                    if ( personId.HasValue )
                    {
                        qryParams.Add( "PersonId", personId.ToString() );
                    }

                    NavigateToParentPage( qryParams );
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the lbCancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbCancel_Click( object sender, EventArgs e )
        {
            var personId = this.PageParameter( "PersonId" ).AsIntegerOrNull();
            var qryParams = new Dictionary<string, string>();
            if ( personId.HasValue )
            {
                qryParams.Add( "PersonId", personId.ToString() );
            }

            NavigateToParentPage( qryParams );
        }

        /// <summary>
        /// Handles the Click event of the lbPrint control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbPrint_Click(object sender, EventArgs e)
        {
            var counselingRequestId = this.PageParameter("CounselingRequestId").AsIntegerOrNull();       
            if (counselingRequestId.HasValue && !counselingRequestId.Equals(0) && !string.IsNullOrEmpty(GetAttributeValue("CounselingRequestStatementPage")))
            {
                NavigateToLinkedPage("CounselingRequestStatementPage", new Dictionary<string, string> { { "CounselingRequestId", counselingRequestId.ToString() } });
            }               
        }

        /// <summary>
        /// Handles the SelectPerson event of the ppPerson control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void ppPerson_SelectPerson( object sender, EventArgs e )
        {
            if ( ppPerson.PersonId != null )
            {
                Person person = new PersonService( new RockContext() ).Get( ppPerson.PersonId.Value );
                if ( person != null )
                {
                    // Make sure that the FirstName box gets either FirstName or NickName of person. 
                    if (!string.IsNullOrWhiteSpace(person.FirstName))
                    {
                        dtbFirstName.Text = person.FirstName;
                    }
                    else if ( !string.IsNullOrWhiteSpace( person.NickName ) )
                    {
                        dtbFirstName.Text = person.NickName;
                    }

                    //If both FirstName and NickName are blank, let them edit it manually
                    dtbFirstName.Enabled = string.IsNullOrWhiteSpace(dtbFirstName.Text);

                    dtbLastName.Text = person.LastName;
                    //If LastName is blank, let them edit it manually
                    dtbLastName.Enabled = string.IsNullOrWhiteSpace( dtbLastName.Text );

                    ddlConnectionStatus.SetValue( person.ConnectionStatusValueId );
                    ddlConnectionStatus.Enabled = false;

                    var homePhoneType = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_HOME.AsGuid() );
                    if ( homePhoneType != null )
                    {
                        var homePhone = person.PhoneNumbers.FirstOrDefault( n => n.NumberTypeValueId == homePhoneType.Id );
                        if ( homePhone != null )
                        {
                            pnbHomePhone.Text = homePhone.NumberFormatted;
                            pnbHomePhone.Enabled = false;
                        }
                    }

                    var mobilePhoneType = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() );
                    if ( mobilePhoneType != null )
                    {
                        var mobileNumber = person.PhoneNumbers.FirstOrDefault( n => n.NumberTypeValueId == mobilePhoneType.Id );
                        if ( mobileNumber != null )
                        {
                            pnbCellPhone.Text = mobileNumber.NumberFormatted;
                            pnbCellPhone.Enabled = false;
                        }
                    }

                    var workPhoneType = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_WORK.AsGuid() );
                    if ( workPhoneType != null )
                    {
                        var workPhone = person.PhoneNumbers.FirstOrDefault( n => n.NumberTypeValueId == workPhoneType.Id );
                        if ( workPhone != null )
                        {
                            pnbWorkPhone.Text = workPhone.NumberFormatted;
                            pnbWorkPhone.Enabled = false;
                        }
                    }

                    ebEmail.Text = person.Email;
                    ebEmail.Enabled = false;

                    lapAddress.SetValue( person.GetHomeLocation() );
                    lapAddress.Enabled = false;

                    // set the campus but not on page load (e will be null) unless from the person profile page (in which case CounselingRequestId in the query string will be 0)
                    int? requestId = Request["CounselingRequestId"].AsIntegerOrNull();
                    
                    if ( !cpCampus.SelectedCampusId.HasValue && ( e != null || (requestId.HasValue && requestId == 0 ) ) )
                    {
                        var personCampus = person.GetCampus();
                        cpCampus.SelectedCampusId = personCampus != null ? personCampus.Id : (int?)null;
                    }
                }
            }
            else
            {
                dtbFirstName.Enabled = true;
                dtbLastName.Enabled = true;
                ddlConnectionStatus.Enabled = true;
                pnbHomePhone.Enabled = true;
                pnbCellPhone.Enabled = true;
                pnbWorkPhone.Enabled = true;
                ebEmail.Enabled = true;
                lapAddress.Enabled = true;
            }
        }

        protected void fileUpDoc_FileUploaded( object sender, EventArgs e )
        {
            var fileUpDoc = (Rock.Web.UI.Controls.FileUploader)sender;

            if ( fileUpDoc.BinaryFileId.HasValue )
            {
                DocumentsState.Add( fileUpDoc.BinaryFileId.Value );
                BindDocuments( true );
            }
        }

        /// <summary>
        /// Handles the FileRemoved event of the fileUpDoc control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="FileUploaderEventArgs"/> instance containing the event data.</param>
        protected void fileUpDoc_FileRemoved( object sender, FileUploaderEventArgs e )
        {
            var fileUpDoc = (Rock.Web.UI.Controls.FileUploader)sender;
            if ( e.BinaryFileId.HasValue )
            {
                DocumentsState.Remove( e.BinaryFileId.Value );
                BindDocuments( true );
            }
        }

        /// <summary>
        /// Handles the ItemDataBound event of the DlDocuments control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DataListItemEventArgs"/> instance containing the event data.</param>
        private void DlDocuments_ItemDataBound( object sender, DataListItemEventArgs e )
        {
            Guid binaryFileTypeGuid = church.ccv.Utility.SystemGuids.BinaryFiletype.CARE_REQUEST_DOCUMENTS.AsGuid();
            var fileupDoc = e.Item.FindControl( "fileupDoc" ) as Rock.Web.UI.Controls.FileUploader;
            if ( fileupDoc != null )
            {
                fileupDoc.BinaryFileTypeGuid = binaryFileTypeGuid;
            }
        }


        #endregion

        #region Methods

        /// <summary>
        /// Binds the documents.
        /// </summary>
        /// <param name="canEdit">if set to <c>true</c> [can edit].</param>
        private void BindDocuments( bool canEdit )
        {
            var ds = DocumentsState.ToList();

            if ( ds.Count() < 6 )
            {
                ds.Add( 0 );
            }

            dlDocuments.DataSource = ds;
            dlDocuments.DataBind();
        }

        /// <summary>
        /// Shows the detail.
        /// </summary>
        /// <param name="counselingRequestId">The counseling request identifier</param>
        public void ShowDetail( int counselingRequestId )
        {
            CareRequest counselingRequest = null;
            var rockContext = new RockContext();
            Service<CareRequest> counselingRequestService = new Service<CareRequest>( rockContext );
            if ( !counselingRequestId.Equals( 0 ) )
            {
                counselingRequest = counselingRequestService.Get( counselingRequestId );
                pdAuditDetails.SetEntity( counselingRequest, ResolveRockUrl( "~" ) );
            }

            if ( counselingRequest == null )
            {
                counselingRequest = new CareRequest { Id = 0 };
                counselingRequest.RequestDateTime = RockDateTime.Now;
                var personId = this.PageParameter( "PersonId" ).AsIntegerOrNull();
                if ( personId.HasValue )
                {
                    var person = new PersonService( rockContext ).Get( personId.Value );
                    if ( person != null )
                    {
                        counselingRequest.RequestedByPersonAliasId = person.PrimaryAliasId;
                        counselingRequest.RequestedByPersonAlias = person.PrimaryAlias;
                    }
                }
                // hide the panel drawer that show created and last modified dates
                pdAuditDetails.Visible = false;
            }

            dtbFirstName.Text = counselingRequest.FirstName;
            dtbLastName.Text = counselingRequest.LastName;
            ebEmail.Text = counselingRequest.Email;
            dtbRequestText.Text = counselingRequest.RequestText;
            dtbSummary.Text = counselingRequest.ResultSummary;
            dtbProvidedNextSteps.Text = counselingRequest.ProvidedNextSteps;
            dpRequestDate.SelectedDate = counselingRequest.RequestDateTime;

            if ( counselingRequest.Campus != null )
            {
                cpCampus.SelectedCampusId = counselingRequest.CampusId;
            }
            else
            {
                cpCampus.SelectedIndex = 0;
            }

            if ( counselingRequest.RequestedByPersonAlias != null )
            {
                ppPerson.SetValue( counselingRequest.RequestedByPersonAlias.Person );
            }
            else
            {
                ppPerson.SetValue( null );
            }

            if ( counselingRequest.HomePhoneNumber != null )
            {
                pnbHomePhone.Text = counselingRequest.HomePhoneNumber;
            }

            if ( counselingRequest.CellPhoneNumber != null )
            {
                pnbCellPhone.Text = counselingRequest.CellPhoneNumber;
            }

            if ( counselingRequest.WorkPhoneNumber != null )
            {
                pnbWorkPhone.Text = counselingRequest.WorkPhoneNumber;
            }

            lapAddress.SetValue( counselingRequest.Location );

            LoadDropDowns( counselingRequest );

            if ( counselingRequest.ConnectionStatusValueId != null )
            {
                ddlConnectionStatus.SetValue( counselingRequest.ConnectionStatusValueId );
            }

            ddlWorker.SetValue( counselingRequest.WorkerPersonAliasId );

            BindGridFromViewState();

            DocumentsState = counselingRequest.Documents.OrderBy( s => s.Order ).Select( s => s.BinaryFileId ).ToList();
            BindDocuments( true );

            counselingRequest.LoadAttributes();
            Rock.Attribute.Helper.AddEditControls( counselingRequest, phAttributes, true, BlockValidationGroup, 2 );

            // call the OnSelectPerson of the person picker which will update the UI based on the selected person
            ppPerson_SelectPerson( null, null );

            hfCounselingRequestId.Value = counselingRequest.Id.ToString();
        }

        /// <summary>
        /// Binds the grid.
        /// </summary>
        private void BindGridFromViewState()
        {
            List<CounselingResultInfo> counselingResultInfoViewStateList = CounselingResultsState;
            gResults.DataSource = counselingResultInfoViewStateList;
            gResults.DataBind();
        }

        /// <summary>
        /// Loads the drop downs.
        /// </summary>
        private void LoadDropDowns( CareRequest counselingRequest )
        {
            ddlConnectionStatus.BindToDefinedType( DefinedTypeCache.Read( new Guid( Rock.SystemGuid.DefinedType.PERSON_CONNECTION_STATUS ) ), true );

            Guid groupGuid = GetAttributeValue( "WorkerRole" ).AsGuid();
            var personList = new GroupMemberService( new RockContext() )
                .Queryable( "Person, Group" )
                .Where( gm => gm.Group.Guid == groupGuid )
                .Select( gm => gm.Person )
                .ToList();

            string WorkerPersonAliasValue = counselingRequest.WorkerPersonAliasId.ToString();
            if ( counselingRequest.WorkerPersonAlias != null && 
                counselingRequest.WorkerPersonAlias.Person != null &&
                !personList.Select( p => p.Id ).ToList().Contains( counselingRequest.WorkerPersonAlias.Person.Id ) )
            {
                personList.Add( counselingRequest.WorkerPersonAlias.Person );
            }

            ddlWorker.DataSource = personList.OrderBy( p => p.NickName ).ThenBy( p => p.LastName ).ToList();
            ddlWorker.DataTextField = "FullName";
            ddlWorker.DataValueField = "PrimaryAliasId";
            ddlWorker.DataBind();
            ddlWorker.Items.Insert( 0, new ListItem() );
        }

        #endregion

        #region CounselingResultInfo

        /// <summary>
        /// The class used to store CounselingResult info.
        /// </summary>
        [Serializable]
        public class CounselingResultInfo
        {
            [DataMember]
            public int? ResultId { get; set; }

            [DataMember]
            public int ResultTypeValueId { get; set; }

            [DataMember]
            public string ResultTypeName { get; set; }

            [DataMember]
            public decimal? Amount { get; set; }

            [DataMember]
            public Guid TempGuid { get; set; }

            [DataMember]
            public string ResultSummary { get; set; }
        }

        #endregion

        
    }
}