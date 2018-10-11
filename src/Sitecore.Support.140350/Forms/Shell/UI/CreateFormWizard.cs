using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.UI;

using Sitecore.Globalization;

using HtmlAgilityPack;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;
using Sitecore.Form.Core.Configuration;
using Sitecore.Form.Core.Renderings;
using Sitecore.Form.Core.Utility;
using Sitecore.Form.UI.Controls;
using Sitecore.Forms.Core.Data;
using Sitecore.Forms.Shell.UI.Controls;
using Sitecore.StringExtensions;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Pages;
using Sitecore.Web.UI.Sheer;
using Sitecore.WFFM.Abstractions.Analytics;
using Sitecore.WFFM.Abstractions.Dependencies;
using Factory = Sitecore.Configuration.Factory;
using ItemUtil = Sitecore.Data.Items.ItemUtil;
using WebUtil = Sitecore.Web.WebUtil;

namespace Sitecore.Support.Forms.Shell.UI
{
    public class CreateFormWizard : WizardForm
    {
        #region Proptected Fields
        protected readonly IAnalyticsSettings AnalyticsSettings;

        protected readonly string multiTreeID = "Forms_MultiTreeView";
        protected readonly string newFormUri = "newFormUriKey";

        protected Edit EbFormName;
        protected Scrollbox ExistingForms;
        protected Frame GlobalForms;
        protected Sitecore.Support.Form.UI.Controls.MultiTreeView multiTree;
        protected Edit GoalName;
        protected DataContext GoalsDataContext;
        protected Checkbox OpenNewForm;
        protected TreePickerEx Goals;

        protected WizardDialogBaseXmlControl CreateForm;
        protected Literal FormNameLiteral;
        protected Radiobutton ChooseForm;
        protected Radiobutton CreateBlankForm;

        protected WizardDialogBaseXmlControl SelectForm;

        protected WizardDialogBaseXmlControl AnalyticsPage;
        protected Checkbox EnableFormDropoutTracking;
        protected Groupbox AnalyticsOptions;
        protected Radiobutton CreateGoal;
        protected Radiobutton SelectGoal;
        protected Literal SelectGoalLiteral;
        protected Groupbox DropoutOptions;
        protected Literal EnableFormDropoutTrackingLiteral;
        protected Literal EnableDropoutSavedToLiteral;
        protected Literal GoalNameLiteral;
        protected Literal PointsLiteral;
        protected Edit Points;

        protected WizardDialogBaseXmlControl ConfirmationPage;
        protected Literal ChoicesLiteral;

        #endregion

        #region Private Fields

        private Item formsRoot;
        private Item goalsRoot;

        #endregion

        public CreateFormWizard()
        {
            this.AnalyticsSettings = DependenciesManager.Resolve<IAnalyticsSettings>();
        }

        #region Protected Methods

        /// <summary>
        /// Overriden. Onload event implementation
        /// </summary>
        /// <param name="e">
        /// The event aguments
        /// </param>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (!Context.ClientPage.IsEvent)
            {
                Context.ClientPage.ClientScript.RegisterClientScriptInclude("jquery", "/sitecore modules/web/web forms for marketers/scripts/jquery.js");
                Context.ClientPage.ClientScript.RegisterClientScriptInclude("jquery-ui.min", "/sitecore modules/web/web forms for marketers/scripts/jquery-ui.min.js");
                Context.ClientPage.ClientScript.RegisterClientScriptInclude("jquery-ui-i18n", "/sitecore modules/web/web forms for marketers/scripts/jquery-ui-i18n.js");
                Context.ClientPage.ClientScript.RegisterClientScriptInclude("json2.min", "/sitecore modules/web/web forms for marketers/scripts/json2.min.js");
                Context.ClientPage.ClientScript.RegisterClientScriptInclude("head.load.min", "/sitecore modules/web/web forms for marketers/scripts/head.load.min.js");
                Context.ClientPage.ClientScript.RegisterClientScriptInclude("sc.webform", "/sitecore modules/web/web forms for marketers/scripts/sc.webform.js?v=17072012");

                Localize();

                ChooseForm.Checked = true;
                CreateGoal.Checked = true;
                EnableFormDropoutTracking.Checked = true;
                AnalyticsOptions.Visible = true;
                DropoutOptions.Visible = true;

                Goals.Value = string.Empty;

                ThemesManager.RegisterCssScript(null, FormsRoot, FormsRoot);

                EbFormName.Value = GetUniqueName("Example Form");

                Sitecore.Support.Form.UI.Controls.MultiTreeView view = new Sitecore.Support.Form.UI.Controls.MultiTreeView
                {
                    Roots = Sitecore.Form.Core.Utility.Utils.GetFormRoots(),
                    Filter = "Contains('{C0A68A37-3C0A-4EEB-8F84-76A7DF7C840E},{A87A00B1-E6DB-45AB-8B54-636FEC3B5523},{FFB1DA32-2764-47DB-83B0-95B843546A7E}', @@templateid)",
                    ID = multiTreeID,
                    DataViewName = "Master",
                    TemplateID = Sitecore.Form.Core.Configuration.IDs.FormTemplateID.ToString(),
                    IsFullPath = true
                };
                multiTree = view;
                ExistingForms.Controls.Add(multiTree);
            }
            else
            {
                multiTree = ExistingForms.FindControl(multiTreeID) as Sitecore.Support.Form.UI.Controls.MultiTreeView;
            }
        }

        protected virtual void Localize()
        {
            CreateForm["Header"] = DependenciesManager.ResourceManager.Localize("CREATE_NEW_FORM");
            CreateForm["Text"] = DependenciesManager.ResourceManager.Localize("CREATE_BLANK_OR_COPY_EXISTING_FORM");
            FormNameLiteral.Text = DependenciesManager.ResourceManager.Localize("FORM_TEXT") + ":";
            CreateBlankForm.Header = DependenciesManager.ResourceManager.Localize("CREATE_BLANK_FORM");
            ChooseForm.Header = DependenciesManager.ResourceManager.Localize("SELECT_FORM_TO_COPY");

            SelectForm["Header"] = DependenciesManager.ResourceManager.Localize("SELECT_FORM");
            SelectForm["Text"] = DependenciesManager.ResourceManager.Localize("COPY_EXISTING_FORM");

            AnalyticsPage["Header"] = DependenciesManager.ResourceManager.Localize("ANALYTICS");
            AnalyticsPage["Text"] = DependenciesManager.ResourceManager.Localize("CHOOSE_WHICH_ANALYTICS_OPTIONS_WILL_BE_USED");
            AnalyticsOptions.Header = DependenciesManager.ResourceManager.Localize("GOAL");
            CreateGoal.Header = DependenciesManager.ResourceManager.Localize("CREATE_NEW_GOAL");
            GoalName.Value = DependenciesManager.ResourceManager.Localize("FORM_NAME_FORM_COMPLETED");
            GoalNameLiteral.Text = DependenciesManager.ResourceManager.Localize("NAME") + ":";
            PointsLiteral.Text = DependenciesManager.ResourceManager.Localize("ENGAGEMENT_VALUE") + ":";
            SelectGoal.Header = DependenciesManager.ResourceManager.Localize("SELECT_EXISTING_GOAL");
            SelectGoalLiteral.Text = DependenciesManager.ResourceManager.Localize("SELECT_NEW_OR_EXISTEN_GOAL");
            DropoutOptions.Header = DependenciesManager.ResourceManager.Localize("DROPOUT_TRACKING");
            EnableFormDropoutTracking.Header = DependenciesManager.ResourceManager.Localize("ENABLE_FORM_DROPOUT_TRACKING");
            EnableFormDropoutTrackingLiteral.Text = DependenciesManager.ResourceManager.Localize("SELECT_IT_TO_TRACK_INFORMATION_ENTERED_IN_FORM");
            EnableDropoutSavedToLiteral.Text = DependenciesManager.ResourceManager.Localize("IF_ENABLED_ANY_DATA_ENTERED_IS_SAVED_IN_ANALYTICS");

            ConfirmationPage["Header"] = DependenciesManager.ResourceManager.Localize("CONFIRMATION");
            ConfirmationPage["Text"] = DependenciesManager.ResourceManager.Localize("CONFIRM_CONFIGURATION_OF_NEW_FORM");
            ChoicesLiteral.Text = DependenciesManager.ResourceManager.Localize("YOU_HAVE_SELECTED_THE_FOLLOWING_SETTINGS");
        }

        protected override void ActivePageChanged(string page, string oldPage)
        {
            base.ActivePageChanged(page, oldPage);

            if (page == "ConfirmationPage")
            {
                NextButton.Visible = true;
                BackButton.Visible = true;
                NextButton.Disabled = false;
                this.NextButton.Disabled = false;
                this.CancelButton.Header = "Cancel";
                this.NextButton.Header = "Create";
            }
            if (oldPage == "ConfirmationPage")
            {
                NextButton.Disabled = false;
                BackButton.Disabled = false;
                CancelButton.Header = "Cancel";
                NextButton.Header = "Next >";
            }

            if (oldPage == "CreateForm" && this.AnalyticsSettings.IsAnalyticsAvailable)
            {
                string name = "{0} Form Completed".FormatWith(EbFormName.Value);

                if (GoalsDataContext.CurrentItem != null)
                {
                    var childs = new List<Item>(this.GoalsDataContext.CurrentItem.Children.ToArray());

                    var count = childs.Where(s => s.Name == name).Count();
                    if (count > 0)
                    {
                        int i = 1;

                        while (childs.FirstOrDefault(s => s.Name == "{0} {1} Form Completed".FormatWith(this.EbFormName.Value, i)) != null)
                        {
                            ++i;
                        }

                        name = "{0} {1} Form Completed".FormatWith(EbFormName.Value, i);
                    }
                }
                GoalName.Value = name;
                SheerResponse.SetOuterHtml(GoalName.ID, GoalName);
            }
        }

        protected override bool ActivePageChanging(string page, ref string newpage)
        {
            bool flag = base.ActivePageChanging(page, ref newpage);

            if (!this.CheckGoalSettings(page, ref newpage))
            {
                return flag;
            }

            if (!this.AnalyticsSettings.IsAnalyticsAvailable && newpage == "AnalyticsPage")
            {
                newpage = "ConfirmationPage";
            }

            if ((page == "CreateForm") && (newpage == "SelectForm"))
            {
                if (EbFormName.Value == string.Empty)
                {
                    Context.ClientPage.ClientResponse.Alert(DependenciesManager.ResourceManager.Localize("EMPTY_FORM_NAME"));
                    newpage = "CreateForm";
                    return flag;
                }
                if (!Regex.IsMatch(EbFormName.Value, Configuration.Settings.ItemNameValidation, RegexOptions.ECMAScript))
                {
                    var message = new StringBuilder();
                    message.AppendFormat("\'{0}\' ", EbFormName.Value);
                    message.Append(DependenciesManager.ResourceManager.Localize("IS_NOT_VALID_NAME"));
                    Context.ClientPage.ClientResponse.Alert(message.ToString());
                    newpage = "CreateForm";
                    return flag;
                }

                if (FormsRoot.Database.GetItem(FormsRoot.Paths.ContentPath + "/" + EbFormName.Value) != null)
                {
                    var message = new StringBuilder();
                    message.AppendFormat("\'{0}\' ", EbFormName.Value);
                    message.Append(DependenciesManager.ResourceManager.Localize("IS_NOT_UNIQUE_NAME"));
                    Context.ClientPage.ClientResponse.Alert(message.ToString());
                    newpage = "CreateForm";
                    return flag;
                }

                if (CreateBlankForm.Checked)
                {
                    newpage = this.AnalyticsSettings.IsAnalyticsAvailable ? "AnalyticsPage" : "ConfirmationPage";
                }
            }

            if ((page == "SelectForm") && ((newpage == "ConfirmationPage") || (newpage == "AnalyticsPage")))
            {
                string id = this.multiTree.Selected;
                Item item = FormsRoot.Database.GetItem(id);
                if (id == null || item == null)
                {
                    Context.ClientPage.ClientResponse.Alert(DependenciesManager.ResourceManager.Localize("PLEASE_SELECT_FORM"));

                    newpage = "SelectForm";
                    return flag;
                }

                if (item.TemplateID != Sitecore.Form.Core.Configuration.IDs.FormTemplateID)
                {
                    var message = new StringBuilder();
                    message.AppendFormat("\'{0}\' ", item.Name);
                    message.Append(DependenciesManager.ResourceManager.Localize("IS_NOT_FORM"));
                    Context.ClientPage.ClientResponse.Alert(message.ToString());

                    newpage = "SelectForm";
                    return flag;
                }
            }

            if ((page == "ConfirmationPage") && newpage == "ConfirmationPage" && !this.AnalyticsSettings.IsAnalyticsAvailable)
            {
                newpage = CreateBlankForm.Checked ? "CreateForm" : "SelectForm";
            }


            if ((page == "ConfirmationPage") && (newpage == "SelectForm" || newpage == "AnalyticsPage"))
            {
                this.CancelButton.Header = "Cancel";
                this.NextButton.Header = "Next >";
            }

            if (((page == "ConfirmationPage" || page == "AnalyticsPage") &&
                 (newpage == "SelectForm")) && CreateBlankForm.Checked)
            {
                newpage = "CreateForm";
            }

            if (newpage == "ConfirmationPage")
            {
                ChoicesLiteral.Text = RenderSetting();
            }

            return flag;
        }

        protected bool CheckGoalSettings(string page, ref string newpage)
        {
            if (page == "AnalyticsPage" && newpage == "ConfirmationPage")
            {

                if (!this.CreateGoal.Checked)
                {
                    var value = StaticSettings.ContextDatabase.GetItem(this.Goals.Value);

                    if (value == null || (value.TemplateName != "Page Event" && value.TemplateName != "Goal"))
                    {
                        Context.ClientPage.ClientResponse.Alert(DependenciesManager.ResourceManager.Localize("CHOOSE_GOAL"));
                        newpage = "AnalyticsPage";
                        return false;
                    }
                }

                else
                {
                    if (string.IsNullOrEmpty(this.GoalName.Value))
                    {
                        Context.ClientPage.ClientResponse.Alert(DependenciesManager.ResourceManager.Localize("ENTER_NAME_FOR_GOAL"));
                        newpage = "AnalyticsPage";
                        return false;
                    }

                    var childs = new List<Item>(this.GoalsDataContext.CurrentItem.Children.ToArray());
                    if (childs.FirstOrDefault(c => c.Name == this.GoalName.Value) != null)
                    {
                        Context.ClientPage.ClientResponse.Alert(DependenciesManager.ResourceManager.Localize("GOAL_ALREADY_EXISTS", this.GoalName.Value));
                        newpage = "AnalyticsPage";
                        return false;
                    }

                    if (!ItemUtil.IsItemNameValid(this.GoalName.Value))
                    {
                        Context.ClientPage.ClientResponse.Alert(DependenciesManager.ResourceManager.Localize("GOAL_NAME_IS_NOT_VALID", this.GoalName.Value));
                        newpage = "AnalyticsPage";
                        return false;
                    }
                }

            }
            return true;
        }

        protected string RenderEndSection()
        {
            return "</fieldset>";
        }

        protected string RenderBeginSection(string name)
        {
            StringBuilder html = new StringBuilder();
            html.Append("<fieldset class='scfGroupSection' >");
            html.Append("<legend>");
            html.Append(DependenciesManager.ResourceManager.Localize(name));
            html.Append("</legend>");


            return html.ToString();
        }

        protected virtual string RenderSetting()
        {
            StringBuilder html = new StringBuilder();
            if (this.RenderConfirmationFormSection)
            {
                html.Append(this.RenderBeginSection("FORM"));
                html.Append(this.GenerateItemSetting());
                html.Append(this.RenderEndSection());
            }

            string preview = (string.Compare(WebUtil.GetQueryString("mode"), StaticSettings.DesignMode, true) != 0) ? GeneratePreview() : string.Empty;

            if (this.AnalyticsSettings.IsAnalyticsAvailable)
            {
                html.Append(RenderBeginSection("ANALYTICS"));
                html.Append(GenerateAnalytics());
                html.Append(RenderEndSection());

                var info = GenerateFutherInfo();
                if (info.Length > 0)
                {
                    html.Append(RenderBeginSection("FURTHER_INFORMATION"));
                    html.Append(info);
                    html.Append(RenderEndSection());
                }
            }

            if (!string.IsNullOrEmpty(preview))
            {
                html.Append(RenderBeginSection("PREVIEW"));

                html.Append(preview);

                html.Append(RenderEndSection());
            }


            return html.ToString();
        }

        protected virtual string GenerateItemSetting()
        {

            string name = this.EbFormName.Value;
            return string.Join("", new[] {"<p>",
                                         string.Format(DependenciesManager.ResourceManager.Localize("FORM_ADDED_IN_MESSAGE"), FormsRoot.Paths.FullPath, name),
                                         "</p>"});
        }

        protected virtual string GeneratePreview()
        {
            var html = new StringBuilder();
            if (ChooseForm.Checked)
            {

                html.Append("<p>");

                var output = new HtmlTextWriter(new StringWriter());

                Item form = StaticSettings.GlobalFormsRoot.Database.GetItem(CreateBlankForm.Checked ? string.Empty : multiTree.Selected);
                RenderFormPreview(form, output);

                html.Append(output.InnerWriter.ToString());

                html.Append("</p>");
                return html.ToString();
            }
            return string.Empty;
        }

        protected virtual string GenerateFutherInfo()
        {
            StringBuilder html = new StringBuilder();

            html.Append("<p>");
            html.Append(DependenciesManager.ResourceManager.Localize("MARKETING_INFO"));
            html.Append("</p>");

            if (EnableFormDropoutTracking.Checked)
            {
                html.Append("<p>");
                html.Append(DependenciesManager.ResourceManager.Localize("DROPOUT_INFO"));
                html.Append("</p>");
            }


            return html.ToString();
        }

        protected virtual string GenerateAnalytics()
        {

            var html = new StringBuilder();

            html.Append("<p>");

            html.Append("<table>");

            html.Append("<tr><td class='scwfmOptionName'>");
            html.Append(DependenciesManager.ResourceManager.Localize("ASOCIATED_GOAL"));
            html.Append("</td><td class='scwfmOptionValue'>");

            string goalName = GoalName.Value;
            if (!CreateGoal.Checked)
            {
                goalName = FormsRoot.Database.GetItem(Goals.Value).Name;
            }

            html.AppendFormat(": {0}", goalName);
            html.Append("</td></tr>");


            html.Append("<tr><td class='scwfmOptionName'>");
            html.Append(DependenciesManager.ResourceManager.Localize("FORM_DROPOUT_TRACKING"));
            html.Append("</td><td class='scwfmOptionValue'>");
            html.AppendFormat(": {0}", EnableFormDropoutTracking.Checked ? "Enabled" : "Disabled");
            html.Append("</td></tr>");

            html.Append("</table>");

            html.Append("</p>");

            return html.ToString();
        }

        protected override void OnNext(object sender, EventArgs formEventArgs)
        {
            if (NextButton.Header == "Create")
            {
                SaveForm();

                SheerResponse.SetModified(false);
            }

            Next();
        }

        /// <summary>
        /// Saves the analytics.
        /// </summary>
        /// <param name="form">The form.</param>
        /// <param name="goalID">The goal ID.</param>
        protected virtual void SaveAnalytics(FormItem form, string goalID)
        {
            if (this.AnalyticsSettings.IsAnalyticsAvailable)
            {

                var tracking = form.Tracking;
                tracking.Update(true, this.EnableFormDropoutTracking.Checked);


                Item goal = this.CreateGoal.Checked
                    ? this.GoalsRoot.Add(this.GoalName.Value, new TemplateID(Sitecore.Form.Core.Configuration.IDs.GoalTemplateID))
                    : this.GoalsRoot.Database.GetItem(goalID);

                goal.Editing.BeginEdit();
                if (this.CreateGoal.Checked)
                {
                    goal["Points"] = string.IsNullOrEmpty(this.Points.Value) ? "0" : this.Points.Value;
                }

                goal["__Workflow state"] = "{EDCBB550-BED3-490F-82B8-7B2F14CCD26E}";
                goal.Editing.EndEdit();

                tracking.AddEvent(goal.ID.Guid);


                form.BeginEdit();
                form.InnerItem.Fields["__Tracking"].Value = tracking.ToString();
                form.EndEdit();
            }
            else
            {
                if (form.InnerItem.Fields["__Tracking"] != null)
                {
                    form.BeginEdit();
                    form.InnerItem.Fields["__Tracking"].Value = "<tracking ignore=\"1\"/>";
                    form.EndEdit();
                }
            }
        }

        protected virtual void SaveForm()
        {
            string goalID = this.Goals.Value;
            Item parent = this.FormsRoot;
            Assert.IsNotNull(parent, "forms root");
            var queryString = WebUtil.GetQueryString("la");
            Language formLanguage = Context.ContentLanguage;
            if (!string.IsNullOrEmpty(queryString))
            {
                Language.TryParse(WebUtil.GetQueryString("la"), out formLanguage);
            }

            Item form = this.FormsRoot.Database.GetItem(this.CreateBlankForm.Checked ? string.Empty : this.multiTree.Selected, formLanguage);

            string name = this.EbFormName.Value;
            string itemName = ItemUtil.ProposeValidItemName(name);

            if (form != null)
            {
                Item oldForm = form;
                form = Context.Workflow.CopyItem(form, parent, itemName, new ID(), true);
                Sitecore.Support.Forms.Core.Data.FormItemSynchronizer.UpdateIDReferences(oldForm, form);
            }
            else
            {
                if (parent.Language != formLanguage)
                {
                    parent = this.FormsRoot.Database.GetItem(parent.ID, formLanguage);
                }

                form = Context.Workflow.AddItem(itemName, new TemplateID(Sitecore.Form.Core.Configuration.IDs.FormTemplateID), parent);
                form.Editing.BeginEdit();
                form.Fields[Sitecore.Form.Core.Configuration.FieldIDs.ShowFormTitleID].Value = "1";
                form.Editing.EndEdit();
            }

            form.Editing.BeginEdit();
            form[Sitecore.Form.Core.Configuration.FieldIDs.FormTitleID] = name;
            form[Sitecore.Form.Core.Configuration.FieldIDs.DisplayNameFieldID] = name;
            form.Editing.EndEdit();

            this.SaveAnalytics(form, goalID);

            this.ServerProperties[this.newFormUri] = form.Uri.ToString();

            Registry.SetString("/Current_User/Dialogs//sitecore/shell/default.aspx?xmlcontrol=Forms.FormDesigner", "1250,500");
            SheerResponse.SetDialogValue(form.Uri.ToString());
        }

        protected void RenderFormPreview(Item form, HtmlTextWriter output)
        {
            var writer = new HtmlTextWriter(new StringWriter());
            var render = new FormRender();
            render.FormID = form != null ? form.ID.ToString() : string.Empty;

            render.IsFastPreview = true;
            render.InitControls();

            //ReflectionUtil.CallMethod(render.formControl, "OnLoad", true, false, new object[] { new EventArgs() });
            //ReflectionUtil.CallMethod(render.formControl, "OnPreRender", true, false, new object[] { new EventArgs() });

            render.RenderControl(writer);
            if (!(writer.InnerWriter.ToString() == string.Empty))
            {
                string pageHtml = writer.InnerWriter.ToString();

                HtmlDocument doc = new HtmlDocument();
                using (new ThreadCultureSwitcher(Language.Parse("en").CultureInfo))
                {
                    doc.LoadHtml(pageHtml);
                }
                this.RemoveScipts(doc.DocumentNode);

                pageHtml = Regex.Replace(doc.DocumentNode.InnerHtml, "on\\w*=\".*?\"", string.Empty);
                output.Write(pageHtml);

                output.Write("<img height='1px' alt='' src='/sitecore/images/blank.gif' width='1' border='0'onload='javascript:Sitecore.Wfm.Utils.zoom(this.previousSibling)'/>");
            }
        }

        protected string GetUniqueName(string name)
        {
            string uniqueName = name;
            int i = 0;
            while (FormsRoot.Database.GetItem(FormsRoot.Paths.ContentPath + "/" + uniqueName) != null)
            {
                uniqueName = name + (++i);
            }
            return uniqueName;
        }

        #endregion

        #region Handle Events

        [HandleMessage("form:creategoal", true)]
        public void OnCreateGoalChanged(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");

            this.Goals.Disabled = true;
            this.Goals.Disabled = this.CreateGoal.Checked;

            this.GoalName.ReadOnly = false;
            this.Points.ReadOnly = false;

            this.GoalName.ReadOnly = !this.CreateGoal.Checked;
            this.Points.ReadOnly = !this.CreateGoal.Checked;

            if (this.Goals.Disabled)
            {
                this.Goals.Value = string.Empty;
            }

            this.GoalName.Style["color"] = this.CreateGoal.Checked ? "black" : "#999999";
            this.Points.Style["color"] = this.CreateGoal.Checked ? "black" : "#999999";

            SheerResponse.SetOuterHtml(this.GoalName.ID, this.GoalName);
            SheerResponse.SetOuterHtml(this.Points.ID, this.Points);

            SheerResponse.Eval("$j('.Range').numeric();$$('.scComboboxDropDown')[0].disabled = " + this.Goals.Disabled.ToString().ToLower() + ";");
        }

        #endregion


        #region Private Methods

        private string GetWindowKey(string url)
        {
            if (url == null || url.Length == 0)
            {
                return String.Empty;
            }

            string result = url;

            int n = result.IndexOf("?xmlcontrol=");

            if (n >= 0)
            {
                n = result.IndexOf("&", n);

                if (n >= 0)
                {
                    result = StringUtil.Left(result, n);
                }
            }
            else if (result.IndexOf("?") >= 0)
            {
                result = StringUtil.Left(result, result.IndexOf("?"));
            }

            if (result.StartsWith(WebUtil.GetServerUrl(), StringComparison.OrdinalIgnoreCase))
            {
                result = StringUtil.Mid(result, WebUtil.GetServerUrl().Length);
            }

            return result;
        }

        private void RemoveScipts(HtmlNode node)
        {
            for (int i = 0; i < node.ChildNodes.Count; i++)
            {
                HtmlNode curNode = node.ChildNodes[i];
                this.RemoveScipts(curNode);
                if (curNode.Name.ToLower() == "script")
                {
                    curNode.InnerHtml = " ";
                }
            }
        }




        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether [render confirmation form section].
        /// </summary>
        /// <value>
        /// <c>true</c> if [render confirmation form section]; otherwise, <c>false</c>.
        /// </value>
        protected virtual bool RenderConfirmationFormSection
        {
            get { return true; }
        }

        protected virtual Item FormsRoot
        {
            get
            {
                if (formsRoot == null)
                {
                    formsRoot = Factory.GetDatabase(DatabaseName).GetItem(Root);
                }
                return formsRoot;
            }
        }

        protected virtual Item GoalsRoot
        {
            get
            {
                if (goalsRoot == null)
                {
                    goalsRoot = Factory.GetDatabase(DatabaseName).GetItem(StaticSettings.GoalsRootID);
                }
                return goalsRoot;
            }
        }

        private string Root
        {
            get
            {
                return WebUtil.GetQueryString("root", StaticSettings.GlobalFormsRootID);
            }
        }

        private string DatabaseName
        {
            get
            {
                return WebUtil.GetQueryString("db", "master");
            }
        }

        #endregion
    }
}
