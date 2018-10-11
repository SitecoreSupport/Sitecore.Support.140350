using System;
using System.Collections.Generic;
using Sitecore.Form.UI.Controls;
using Sitecore.Data.Items;
using Sitecore.Web.UI.HtmlControls;

namespace Sitecore.Support.Form.UI.Controls
{
    public class MultiTreeView : Control
    {
        public event EventHandler SelectedChange;
        public static readonly string ShortDescriptionField = "{9541E67D-CE8C-4225-803D-33F7F29F09EF}";
        #region Protected Methods

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (!Sitecore.Context.ClientPage.IsEvent)
            {
                foreach (KeyValuePair<string, string> root in Roots)
                {
                    DataContext dataContext = AddDataContext(root.Key);
                    Controls.Add(dataContext);

                    bool isEmpty = IsDataContextEmpty(dataContext, dataContext.CurrentItem);
                    if (IsFullPath && !isEmpty)
                    {
                        Controls.Add(AddTitle(dataContext, root.Key, root.Value));
                    }

                    Control control = AddTree(dataContext);
                    if (isEmpty)
                    {
                        control.Visible = false;
                    }
                    Controls.Add(control);
                }
            }
        }

        protected void DoubleClick(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                InvokeDbClick(new ItemDoubleClickedEventArgs(id));
            }
        }

        protected void Select(string id)
        {
            DataContext context = FindControl(id) as DataContext;
            if (context != null)
            {
                UnselectDataTreeview tree = FindControl(ID + context.ID + "_treeview") as UnselectDataTreeview;
                if (tree != null)
                {
                    if (tree.Selected != null && tree.Selected.Length > 0)
                    {
                        Selected = (tree.Selected[0] as DataTreeNode).ItemID;
                    }
                    else
                    {
                        Selected = null;
                    }
                }
            }
            else
            {
                Selected = null;
            }

            DataContext[] dataContexts = GetDataContexts(id);
            UnselectDataTreeview[] trees = GetTreeViews(dataContexts);

            ClearSelection(dataContexts, trees);

            RaiseSelectedChange();
        }

        #endregion

        #region Private methods

        private void RaiseSelectedChange()
        {
            if (SelectedChange != null)
            {
                SelectedChange(this, EventArgs.Empty);
            }
        }

        private DataContext[] GetDataContexts(string notIncludeContext)
        {
            List<DataContext> contexts = new List<DataContext>();

            foreach (KeyValuePair<string, string> root in Roots)
            {
                string controlID = ID + "dataContext" + root.Key;
                if (controlID != notIncludeContext)
                {
                    DataContext context = FindControl(controlID) as DataContext;
                    if (context != null)
                    {
                        contexts.Add(context);
                    }
                }
            }

            return contexts.ToArray();
        }

        private UnselectDataTreeview[] GetTreeViews(DataContext[] dataContexts)
        {
            List<UnselectDataTreeview> trees = new List<UnselectDataTreeview>();

            foreach (DataContext context in dataContexts)
            {
                UnselectDataTreeview tree = FindControl(ID + context.ID + "_treeview") as UnselectDataTreeview;
                if (tree != null)
                {
                    trees.Add(tree);
                }
            }

            return trees.ToArray();
        }

        private void ClearSelection(DataContext[] dataContexts, UnselectDataTreeview[] treeviews)
        {
            foreach (DataContext context in dataContexts)
            {
                context.ClearSelected();
            }

            foreach (UnselectDataTreeview tree in treeviews)
            {
                tree.ClearSelection();
            }
        }

        private DataTreeview AddTree(DataContext dataContext)
        {
            UnselectDataTreeview tree = new UnselectDataTreeview();
            tree.DataContext = dataContext.ID;
            tree.ID = ID + dataContext.ID + "_treeview";
            tree.AllowDragging = false;
            tree.Click = ID + ".Select(\"" + dataContext.ID + "\")";
            tree.DblClick = ID + ".DoubleClick(\"" + dataContext.ID + "\")";
            tree.AutoUpdateDataContext = false;
            tree.ShowRoot = true;
            return tree;
        }

        private DataContext AddDataContext(string root)
        {
            DataContext dataContext = new DataContext();
            dataContext.DataViewName = DataViewName;
            dataContext.Root = root;
            dataContext.Filter = Filter;
            dataContext.ID = ID + "dataContext" + root;

            return dataContext;
        }

        private static Border AddTitle(DataContext dataContext, string name, string desc)
        {
            Item item = dataContext.GetItem(name);

            Border border = new Border();
            border.Class = "scTitleTree";

            Literal title = new Literal();

            string description = (!string.IsNullOrEmpty(item.Fields[ShortDescriptionField].Value)) ?
                                   item.Fields[ShortDescriptionField].Value : (desc ?? string.Empty);

            if (string.IsNullOrEmpty(description))
            {
                title.Text = (item.Parent != null) ? item.Parent.Paths.FullPath : "/";
            }
            else
            {
                title.Text = description;
            }

            border.Controls.Add(title);
            return border;
        }

        private bool IsDataContextEmpty(DataContext context, Item item)
        {
            foreach (Item child in context.GetChildren(item))
            {
                if (child.TemplateID.ToString() == TemplateID || !IsDataContextEmpty(context, child))
                {
                    return false;
                }
            }
            return true;
        }

        private void InvokeDbClick(ItemDoubleClickedEventArgs e)
        {
            EventHandler<ItemDoubleClickedEventArgs> handler = DbClick;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        #endregion

        #region Public Properties

        public Dictionary<string, string> Roots
        {
            get
            {
                return ServerProperties["Roots"] as Dictionary<string, string>;
            }
            set
            {
                ServerProperties["Roots"] = value;
            }
        }

        public string Filter
        {
            get
            {
                return GetViewStateString("Filter");
            }
            set
            {
                SetViewStateString("Filter", value);
            }
        }

        public bool IsFullPath
        {
            get
            {
                return GetViewStateBool("IsFullPath");
            }
            set
            {
                SetViewStateBool("IsFullPath", value);
            }
        }

        public string TemplateID
        {
            get
            {
                return GetViewStateString("TemplateID");
            }
            set
            {
                SetViewStateString("TemplateID", value);
            }
        }

        public string DataViewName
        {
            get
            {
                return GetViewStateString("DataViewName");
            }
            set
            {
                SetViewStateString("DataViewName", value);
            }
        }

        public string Selected
        {
            get
            {
                return GetViewStateString("SelectedID", null);
            }
            set
            {
                SetViewStateString("SelectedID", value);
            }
        }

        #endregion

        #region Events

        public event EventHandler<ItemDoubleClickedEventArgs> DbClick;

        #endregion
    }
}
