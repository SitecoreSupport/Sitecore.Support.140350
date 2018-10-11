using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Web.UI;
using Sitecore.Web.UI.HtmlControls;
using System;
using Control = System.Web.UI.Control;

namespace Sitecore.Support.Form.UI.Controls
{
    class UnselectDataTreeview : DataTreeview
    {
        [NotNull]
        protected override Control Populate(Control control, DataContext dataContext)
        {
            Assert.ArgumentNotNull(control, "control");
            Assert.ArgumentNotNull(dataContext, "dataContext");
            if (!IsTrackingViewState && !ViewStateDisabler.IsActive)
            {
                TrackViewState();
            }

            Item item;
            Item folder;
            Item[] itemArray;
            dataContext.GetState(out item, out folder, out itemArray);
            if (item != null)
            {
                SetViewStateString("Root", item.ID.ToString());
                Control node = control;
                if (ShowRoot)
                {
                    TreeNode treeNode = GetTreeNode(item, control);
                    #region modified part - method has been added to change Header in accordance with Display Name
                    ChangeTreeNodeHeader(treeNode, item);
                    #endregion
                    treeNode.Expanded = true;
                    treeNode.Selected = false;
                    node = treeNode;
                }
                string selectedIDs = GetSelectedIDs(itemArray);
                Populate(dataContext, node, item, folder, selectedIDs);
            }
            return control;
        }

        [NotNull]
        protected override void Populate(DataContext dataContext, Control control, Item root, Item folder, string selectedIDs)
        {
            Assert.ArgumentNotNull(dataContext, "dataContext");
            Assert.ArgumentNotNull(control, "control");
            Assert.ArgumentNotNull(root, "root");
            Assert.ArgumentNotNull(folder, "folder");
            Assert.ArgumentNotNull(selectedIDs, "selectedIDs");
            Sitecore.Context.ClientPage.ClientResponse.DisableOutput();
            try
            {
                Control node = null;
                Item item = null;
                foreach (Item child in dataContext.GetChildren(root))
                {
                    TreeNode treeNode = GetTreeNode(child, control);

                    #region modified part - method has been added to change Header in accordance with Display Name
                    ChangeTreeNodeHeader(treeNode, child);
                    #endregion

                    treeNode.Expandable = dataContext.HasChildren(child);
                    if (dataContext.IsAncestorOf(child, folder))
                    {
                        item = child;
                        node = treeNode;
                        treeNode.Selected = false;
                        treeNode.Expanded = !treeNode.Selected;
                    }
                    if (selectedIDs.Length > 0)
                    {
                        treeNode.Selected = selectedIDs.IndexOf(child.ID.ToString()) >= 0;
                    }
                }
                if ((item != null) && (item.ID != folder.ID))
                {
                    Populate(dataContext, node, item, folder, selectedIDs);
                }
            }
            finally
            {
                Sitecore.Context.ClientPage.ClientResponse.EnableOutput();
            }
        }

        [NotNull]
        private static string GetSelectedIDs(Item[] selected)
        {
            Assert.ArgumentNotNull(selected, "selected");
            string str = string.Empty;
            foreach (Item item in selected)
            {
                if (item != null)
                {
                    str = str + item.ID;
                }
            }
            return str;
        }

        #region modified part - method to change Header in accordance with Display Name
        private void ChangeTreeNodeHeader(TreeNode treeNode, Item item)
        {
            treeNode.Header = item.Name;
            try
            {
                Globalization.Language contextLanguage = Globalization.Language.Parse(Web.WebUtil.GetQueryString("la"));

                if (null != contextLanguage)
                {
                    Item tmpItem = null;

                    foreach (Globalization.Language language in item.Languages)
                    {
                        tmpItem = item.Database.GetItem(item.ID, language);

                        if ((string.Compare(language.Name, contextLanguage.Name, true) == 0) &&
                            (tmpItem.Versions.Count > 0))
                        {
                            treeNode.Header = tmpItem.DisplayName;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, this);
            }
        }
        #endregion

    }
}
