using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Form.Core.Configuration;
using Sitecore.Form.Core.Utility;
using Sitecore.Forms.Shell.UI.Controls;
using Sitecore.Globalization;
using Sitecore.Layouts;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Pages;
using Sitecore.Web.UI.Sheer;
using Sitecore.WFFM.Abstractions.Dependencies;

namespace Sitecore.Support.Forms.Shell.UI
{
    public class InsertFormWizard : CreateFormWizard
    {
        #region Fields

        /// <summary>
        /// The placeholder list
        /// </summary>
        protected PlaceholderList Placeholders;

        /// <summary>
        /// The select placeholder page
        /// </summary>
        protected WizardDialogBaseXmlControl SelectPlaceholder;

        /// <summary>
        /// The enter new form name page
        /// </summary>
        protected WizardDialogBaseXmlControl FormName;

        /// <summary>
        /// The copy form
        /// </summary>
        protected Radiobutton InsertForm;

        /// <summary>
        /// The current item uri
        /// </summary>
        private string currentItemUri;

        #endregion

        #region Public methods

        /// <summary>
        /// Gets the context item
        /// </summary>
        /// <returns>
        /// Returns the context item
        /// </returns>
        public Item GetCurrentItem()
        {
            string queryString = Web.WebUtil.GetQueryString("id");
            string language = Web.WebUtil.GetQueryString("la");
            string version = Web.WebUtil.GetQueryString("vs");
            string databaseName = Web.WebUtil.GetQueryString("db");

            var uri = new ItemUri(queryString, Language.Parse(language), Sitecore.Data.Version.Parse(version), databaseName);
            return Database.GetItem(uri);
        }

        #endregion

        #region Protected methods

        /// <summary>
        /// Actives the page changing.
        /// </summary>
        /// <param name="page">The page.</param>
        /// <param name="newpage">The newpage.</param>
        /// <returns></returns>
        protected override bool ActivePageChanging(string page, ref string newpage)
        {
            bool flag = true;

            if (!this.AnalyticsSettings.IsAnalyticsAvailable && newpage == "AnalyticsPage")
            {
                newpage = "ConfirmationPage";
            }

            if (!this.CheckGoalSettings(page, ref newpage))
            {
                return flag;
            }

            if (this.InsertForm.Checked && page == "CreateForm" && newpage == "FormName")
            {
                newpage = "SelectForm";
            }

            if (this.InsertForm.Checked && page == "ConfirmationPage" && newpage == "AnalyticsPage")
            {
                newpage = "SelectPlaceholder";
            }

            if (this.InsertForm.Checked && page == "SelectForm" && newpage == "FormName")
            {
                newpage = "CreateForm";
            }

            if (((page == "CreateForm") || (page == "FormName")) && (newpage == "SelectForm"))
            {
                if (this.EbFormName.Value == string.Empty)
                {
                    Context.ClientPage.ClientResponse.Alert(DependenciesManager.ResourceManager.Localize("EMPTY_FORM_NAME"));
                    newpage = (page == "CreateForm") ? "CreateForm" : "FormName";
                    return flag;
                }

                if (this.FormsRoot.Database.GetItem(this.FormsRoot.Paths.ContentPath + "/" + this.EbFormName.Value) != null)
                {
                    var message = new StringBuilder();
                    message.AppendFormat("\'{0}\' ", this.EbFormName.Value);
                    message.Append(DependenciesManager.ResourceManager.Localize("IS_NOT_UNIQUE_NAME"));
                    Context.ClientPage.ClientResponse.Alert(message.ToString());
                    newpage = (page == "CreateForm") ? "CreateForm" : "FormName";
                    return flag;
                }

                if (!Regex.IsMatch(this.EbFormName.Value, Configuration.Settings.ItemNameValidation, RegexOptions.ECMAScript))
                {
                    var message = new StringBuilder();
                    message.AppendFormat("\'{0}\' ", this.EbFormName.Value);
                    message.Append(DependenciesManager.ResourceManager.Localize("IS_NOT_VALID_NAME"));
                    Context.ClientPage.ClientResponse.Alert(message.ToString());
                    newpage = (page == "CreateForm") ? "CreateForm" : "FormName";
                    return flag;
                }

                if (this.CreateBlankForm.Checked)
                {
                    newpage = !string.IsNullOrEmpty(this.Placeholder) ? "ConfirmationPage" : "SelectPlaceholder";

                    if (this.AnalyticsSettings.IsAnalyticsAvailable && newpage == "ConfirmationPage")
                    {
                        newpage = "AnalyticsPage";
                    }
                }
            }

            if ((page == "SelectForm") && ((newpage == "SelectPlaceholder") || (newpage == "ConfirmationPage") || (newpage == "AnalyticsPage")))
            {
                string id = this.multiTree.Selected;
                Item item = StaticSettings.GlobalFormsRoot.Database.GetItem(id);
                if (id == null || item == null)
                {
                    Context.ClientPage.ClientResponse.Alert(DependenciesManager.ResourceManager.Localize("PLEASE_SELECT_FORM"));

                    newpage = "SelectForm";
                    return flag;
                }

                if (item.TemplateID != IDs.FormTemplateID)
                {
                    var message = new StringBuilder();
                    message.AppendFormat("\'{0}\' ", item.Name);
                    message.Append(DependenciesManager.ResourceManager.Localize("IS_NOT_FORM"));
                    Context.ClientPage.ClientResponse.Alert(message.ToString());

                    newpage = "SelectForm";
                    return flag;
                }
            }


            if (newpage == "SelectPlaceholder" && page == "AnalyticsPage")
            {
                newpage = string.IsNullOrEmpty(this.Placeholder) ? "SelectPlaceholder" : "SelectForm";
            }

            if (newpage == "SelectPlaceholder" && page == "SelectForm" && !this.InsertForm.Checked)
            {
                newpage = string.IsNullOrEmpty(this.Placeholder) ? "SelectPlaceholder"
                  : (!this.AnalyticsSettings.IsAnalyticsAvailable ? "ConfirmationPage" : "AnalyticsPage");
            }

            if (newpage == "SelectPlaceholder" && page == "SelectForm" && this.InsertForm.Checked)
            {
                newpage = string.IsNullOrEmpty(this.Placeholder) ? "SelectPlaceholder" : "ConfirmationPage";
            }

            if ((page == "ConfirmationPage") && newpage == "ConfirmationPage" && !this.AnalyticsSettings.IsAnalyticsAvailable)
            {
                newpage = string.IsNullOrEmpty(this.Placeholder) ? "SelectPlaceholder" : "SelectForm";
            }

            if ((page == "ConfirmationPage") && (newpage == "SelectPlaceholder" || newpage == "AnalyticsPage"))
            {
                if (newpage != "AnalyticsPage")
                {
                    newpage = string.IsNullOrEmpty(this.Placeholder) ? "SelectPlaceholder" : "SelectForm";
                }

                this.NextButton.Disabled = false;
                this.BackButton.Disabled = false;
                this.CancelButton.Header = "Cancel";
                this.NextButton.Header = "Next >";
            }

            if ((page == "SelectPlaceholder") && (newpage == "ConfirmationPage" || newpage == "AnalyticsPage"))
            {
                if (string.IsNullOrEmpty(this.ListValue))
                {
                    newpage = "SelectPlaceholder";
                    Context.ClientPage.ClientResponse.Alert(DependenciesManager.ResourceManager.Localize("SELECT_MUST_SELECT_PLACEHOLDER"));
                }
                if (this.InsertForm.Checked)
                {
                    newpage = "ConfirmationPage";
                }
            }

            if ((((page == "ConfirmationPage" || page == "AnalyticsPage") && (newpage == "SelectForm")) ||
              ((page == "SelectPlaceholder") && (newpage == "SelectForm"))) && this.CreateBlankForm.Checked)
            {
                newpage = "CreateForm";
            }

            if (newpage == "ConfirmationPage")
            {
                this.ChoicesLiteral.Text = this.RenderSetting();
            }

            return flag;
        }

        /// <summary>
        /// Generates the item setting.
        /// </summary>
        /// <returns></returns>
        [NotNull]
        protected override string GenerateItemSetting()
        {
            string placeholder = this.ListValue ?? this.Placeholder;
            string name = this.EbFormName.Value;
            Item item = Database.GetItem(ItemUri.Parse(this.currentItemUri));

            var html = new StringBuilder();
            html.Append("<p>");
            Item root = SiteUtils.GetFormsRootItemForItem(item);

            html.AppendFormat(DependenciesManager.ResourceManager.Localize("FORM_ADDED_MESSAGE"), item.Name, placeholder, root.Paths.FullPath, name);

            html.Append("</p>");
            return html.ToString();
        }

        /// <summary>
        /// Localizes this instance.
        /// </summary>
        protected override void Localize()
        {
            base.Localize();

            this.SelectPlaceholder["Header"] = DependenciesManager.ResourceManager.Localize("SELECT_PLACEHOLDER");
            this.SelectPlaceholder["Text"] = DependenciesManager.ResourceManager.Localize("FORM_WILL_BE_INSERTED_INTO_PLACEHOLDER");
            this.InsertForm.Header = DependenciesManager.ResourceManager.Localize("INSERT_FORM");
            this.CreateForm["Header"] = DependenciesManager.ResourceManager.Localize("INSERT_FORM_HEADER");
            this.CreateForm["Text"] = DependenciesManager.ResourceManager.Localize("INSERT_FORM_TEXT");
            this.FormName["Header"] = DependenciesManager.ResourceManager.Localize("ENTER_FORM_NAME_HEADER");
            this.FormName["Text"] = DependenciesManager.ResourceManager.Localize("ENTER_FORM_NAME_TEXT");
        }

        /// <summary>
        /// Overriden. Onload event implementation
        /// </summary>
        /// <param name="e">The event aguments</param>
        protected override void OnLoad(EventArgs e)
        {
            if (!Context.ClientPage.IsEvent)
            {
                Item item = this.GetCurrentItem();
                this.currentItemUri = item.Uri.ToString();

                this.Localize();
            }

            base.OnLoad(e);

            if (!Context.ClientPage.IsEvent)
            {
                Item item = this.GetCurrentItem();
                this.EbFormName.Value = this.GetUniqueName(item.Name);

                this.Layout = item[Sitecore.FieldIDs.LayoutField];
                this.Placeholders.DeviceID = this.DeviceID;
                this.Placeholders.ShowDeviceTree = string.IsNullOrEmpty(this.Mode);
                this.Placeholders.ItemUri = this.currentItemUri;
                this.Placeholders.AllowedRendering = StaticSettings.GetRendering(item).ToString();
            }
            else
            {
                this.currentItemUri = this.ServerProperties["forms_current_item"] as string;
            }
        }

        /// <summary>
        /// Overridden. On prerender event implementation
        /// </summary>
        /// <param name="e">
        /// The event arguments
        /// </param>
        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);
            this.ServerProperties["forms_current_item"] = this.currentItemUri;
        }

        /// <summary>
        /// Actives the page changed.
        /// </summary>
        /// <param name="page">The page.</param>
        /// <param name="oldPage">The old page.</param>
        protected override void ActivePageChanged(string page, string oldPage)
        {
            base.ActivePageChanged(page, oldPage);

            if (page == "ConfirmationPage" && this.InsertForm.Checked)
            {
                this.CancelButton.Header = "Cancel";
                this.NextButton.Header = "Insert";
            }
        }

        /// <summary>
        /// Called when [next].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="formEventArgs">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected override void OnNext(object sender, EventArgs formEventArgs)
        {
            if (this.NextButton.Header == "Create" || this.NextButton.Header == "Insert")
            {
                this.SaveForm();

                SheerResponse.SetModified(false);
            }

            this.Next();
        }

        /// <summary>
        /// Saves the form.
        /// </summary>
        protected override void SaveForm()
        {
            string idDevice = this.Placeholders.DeviceID;
            Item form;
            if (!this.InsertForm.Checked)
            {
                base.SaveForm();
                form = Database.GetItem(ItemUri.Parse((string)this.ServerProperties[this.newFormUri]));
            }
            else
            {
                var queryString = Web.WebUtil.GetQueryString("la");
                Language formLanguage = Context.ContentLanguage;
                if (!string.IsNullOrEmpty(queryString))
                {
                    Language.TryParse(Web.WebUtil.GetQueryString("la"), out formLanguage);
                }

                form = this.FormsRoot.Database.GetItem(this.CreateBlankForm.Checked ? string.Empty : this.multiTree.Selected, formLanguage);
            }
            if (this.Mode != StaticSettings.DesignMode && this.Mode != "edit")
            {
                Item item = Database.GetItem(ItemUri.Parse(this.currentItemUri));
                var definition = LayoutDefinition.Parse(LayoutField.GetFieldValue(item.Fields[Sitecore.FieldIDs.LayoutField]));
                var rendering = new RenderingDefinition();
                string placeholder = this.ListValue;

                var renderingId = StaticSettings.GetRendering(definition);
                rendering.ItemID = renderingId.ToString();

                if (rendering.ItemID == IDs.FormInterpreterID.ToString())
                {
                    rendering.Parameters = "FormID=" + form.ID;
                }
                else
                {
                    rendering.Datasource = form.ID.ToString();
                }

                rendering.Placeholder = placeholder;

                var device = definition.GetDevice(idDevice);
                var deviceRendering = device.GetRenderings(rendering.ItemID);

                if (renderingId != IDs.FormMvcInterpreterID && deviceRendering.Any(x =>
                  (x.Parameters != null && x.Parameters.Contains(rendering.Parameters)) ||
                  x.Datasource != null && x.Datasource.Contains(form.ID.ToString())))
                {
                    Context.ClientPage.ClientResponse.Alert(DependenciesManager.ResourceManager.Localize("FORM_CANT_BE_INSERTED"));
                    return;
                }

                item.Editing.BeginEdit();
                device.AddRendering(rendering);
                if (item.Name != "__Standard Values")
                {
                    LayoutField.SetFieldValue(item.Fields[Sitecore.FieldIDs.LayoutField], definition.ToXml());
                }
                else
                {
                    item[Sitecore.FieldIDs.LayoutField] = definition.ToXml();
                }

                item.Editing.EndEdit();
            }
            else
            {
                Item item = Database.GetItem(ItemUri.Parse(this.currentItemUri));
                var definition = LayoutDefinition.Parse(LayoutField.GetFieldValue(item.Fields[Sitecore.FieldIDs.LayoutField]));
                var rendering = new RenderingDefinition();
                string placeholder = this.ListValue;

                var renderingId = StaticSettings.GetRendering(definition);

                rendering.ItemID = renderingId.ToString();

                rendering.Parameters = "FormID=" + form.ID;
                rendering.Datasource = form.ID.ToString();

                rendering.Placeholder = placeholder;

                var device = definition.GetDevice(idDevice);
                var deviceRendering = device.GetRenderings(rendering.ItemID);

                if (renderingId != IDs.FormMvcInterpreterID && deviceRendering.Any(x =>
                  (x.Parameters != null && x.Parameters.Contains(rendering.Parameters)) ||
                  x.Datasource != null && x.Datasource.Contains(form.ID.ToString())))
                {
                    Context.ClientPage.ClientResponse.Alert(DependenciesManager.ResourceManager.Localize("FORM_CANT_BE_INSERTED"));
                    return;
                }
                SheerResponse.SetDialogValue(form.ID.ToString());
            }
        }
        #endregion

        #region "Public Properties"

        #region Public properties

        /// <summary>
        /// Gets a value indicating whether this instance is called page editor.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is called page editor; otherwise, <c>false</c>.
        /// </value>
        public bool IsCalledFromPageEditor
        {
            get
            {
                return Web.WebUtil.GetQueryString("pe", "0") == "1";
            }
        }

        /// <summary>
        /// Gets the device ID.
        /// </summary>
        /// <value>The device ID.</value>
        public string DeviceID
        {
            get
            {
                return Web.WebUtil.GetQueryString("deviceid");
            }
        }

        /// <summary>
        /// Gets or sets the layout.
        /// </summary>
        /// <value>The layout.</value>
        public string Layout
        {
            get
            {
                return StringUtil.GetString(this.ServerProperties["LayoutCurrent"]);
            }

            set
            {
                Assert.ArgumentNotNull(value, "value");
                this.ServerProperties["LayoutCurrent"] = value;
            }
        }

        /// <summary>
        /// </summary>
        public string ListValue
        {
            get
            {
                return this.Placeholders.SelectedPlaceholder;
            }
        }

        /// <summary>
        /// </summary>
        public string Mode
        {
            get
            {
                return Web.WebUtil.GetQueryString("mode");
            }
        }

        /// <summary>
        /// </summary>
        public string Placeholder
        {
            get
            {
                return Web.WebUtil.GetQueryString("placeholder");
            }
        }

        #endregion

        #region Protected properties

        /// <summary>
        /// </summary>
        protected override Item FormsRoot
        {
            get
            {
                Item item = Database.GetItem(ItemUri.Parse(this.currentItemUri));
                return SiteUtils.GetFormsRootItemForItem(item);
            }
        }

        /// <summary>
        /// Gets a value indicating whether [render confirmation form section].
        /// </summary>
        /// <value>
        /// <c>true</c> if [render confirmation form section]; otherwise, <c>false</c>.
        /// </value>
        protected override bool RenderConfirmationFormSection
        {
            get
            {
                return !this.InsertForm.Checked;
            }
        }
        #endregion

        #endregion
    }
}
