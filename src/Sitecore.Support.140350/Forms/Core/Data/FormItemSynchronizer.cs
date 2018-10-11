using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Form.Core.Data;
using Sitecore.Form.Core.Utility;
using Sitecore.Globalization;
using Sitecore.Forms.Core.Data;

namespace Sitecore.Support.Forms.Core.Data
{
    internal class FormItemSynchronizer
    {
        #region Constants and Fields

        /// <summary>
        /// The database
        /// </summary>
        private readonly Database database;

        /// <summary>
        /// The definition
        /// </summary>
        private readonly FormDefinition definition;

        /// <summary>
        /// The language
        /// </summary>
        private readonly Language language;

        /// <summary>
        /// The form item
        /// </summary>
        private Item formItem;
        #endregion

        #region Constructors and Destructors

        public FormItemSynchronizer([NotNull] Database database, [NotNull] Language language, [NotNull] FormDefinition definition)
        {
            Assert.ArgumentNotNull(database, "database");
            Assert.ArgumentNotNull(language, "language");
            Assert.ArgumentNotNull(definition, "definition");

            this.database = database;
            this.language = language;
            this.definition = definition;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the form.
        /// </summary>
        public Item Form
        {
            get
            {
                if (this.formItem == null && !string.IsNullOrEmpty(this.definition.FormID))
                {
                    this.formItem = this.database.GetItem(this.definition.FormID, this.language);
                }

                return this.formItem;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Finds the match.
        /// </summary>
        /// <param name="oldID">The old ID.</param>
        /// <param name="oldForm">The old form.</param>
        /// <param name="newForm">The new form.</param>
        /// <returns>
        /// The match.
        /// </returns>
        public static ID FindMatch(ID oldID, FormItem oldForm, FormItem newForm)
        {
            Assert.ArgumentNotNull(oldID, "oldID");

            Assert.ArgumentNotNull(oldForm, "oldForm");
            Assert.ArgumentNotNull(newForm, "newForm");

            Item item = oldForm.Database.GetItem(oldID);
            if (item != null)
            {
                if (item.Paths.LongID.Contains(oldForm.ID.ToString()))
                {
                    int index = -1;
                    if (item.ParentID == oldForm.ID)
                    {
                        index = oldForm.InnerItem.Children.IndexOf(item);
                        if (index > -1 && newForm.InnerItem.Children.Count() > index)
                        {
                            return newForm.InnerItem.Children[index].ID;
                        }
                    }

                    if (item.Parent.ParentID == oldForm.ID)
                    {
                        index = oldForm.InnerItem.Children.IndexOf(item.Parent);
                        int fieldIndex = item.Parent.Children.IndexOf(item);

                        if (index > -1 && fieldIndex > -1 && newForm.InnerItem.Children.Count() > index && newForm.InnerItem.Children[index].Children.Count() > fieldIndex)
                        {
                            return newForm.InnerItem.Children[index].Children[fieldIndex].ID;
                        }
                    }
                }
            }

            return ID.Null;
        }

        /// <summary>
        /// Updates the ID references.
        /// </summary>
        /// <param name="oldForm">The old form.</param>
        /// <param name="newForm">The new form.</param>
        public static void UpdateIDReferences(FormItem oldForm, FormItem newForm)
        {
            Assert.ArgumentNotNull(oldForm, "oldForm");
            Assert.ArgumentNotNull(newForm, "newForm");

            newForm.SaveActions = UpdateIDs(newForm.SaveActions, oldForm, newForm);
            newForm.CheckActions = UpdateIDs(newForm.CheckActions, oldForm, newForm);
        }

        /// <summary>
        /// Synchronizes this instance.
        /// </summary>
        public void Synchronize()
        {
            foreach (SectionDefinition section in this.definition.Sections)
            {
                Item sectionItem = null;
                if (!this.DeleteSectionIsEmpty(section))
                {
                    sectionItem = this.UpdateSection(section);
                }
                else
                {
                    if (!string.IsNullOrEmpty(section.SectionID))
                    {
                        sectionItem = section.UpdateSharedFields(this.database, null);
                    }
                }

                foreach (FieldDefinition field in section.Fields)
                {
                    this.SynchronizeField(sectionItem, field);
                }

                if (sectionItem != null && !sectionItem.HasChildren)
                {
                    sectionItem.Delete();
                }
            }
        }

        /// <summary>
        /// Deletes the section is empty.
        /// </summary>
        /// <param name="section">The section.</param>
        /// <returns>
        /// The section is empty.
        /// </returns>
        protected bool DeleteSectionIsEmpty(SectionDefinition section)
        {
            if (section != null)
            {
                bool deleteItem = section.Deleted == "1";

                if (string.IsNullOrEmpty(section.Name))
                {
                    if (section.IsHasOnlyEmptyField)
                    {
                        section.Deleted = "1";
                    }
                    else
                    {
                        section.Name = string.Empty;
                    }
                }

                if (section.Deleted == "1")
                {
                    Item sectionItem = this.database.GetItem(section.SectionID, this.language);
                    if (sectionItem != null)
                    {
                        if (deleteItem)
                        {
                            sectionItem.Delete();
                        }
                        else
                        {
                            Sitecore.Form.Core.Utility.Utils.RemoveVersionOrItem(sectionItem);
                        }
                    }

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Deletes the field is empty.
        /// </summary>
        /// <param name="field">The field.</param>
        /// <returns>
        /// The field is empty.
        /// </returns>
        protected bool DeleteFieldIsEmpty(FieldDefinition field)
        {
            if (field != null)
            {
                Item fieldItem = this.database.GetItem(field.FieldID, this.language);
                if (field.Deleted == "1")
                {
                    if (fieldItem != null)
                    {
                        fieldItem.Delete();
                    }

                    return true;
                }

                if (string.IsNullOrEmpty(field.Name))
                {
                    field.Deleted = "1";

                    if (fieldItem != null)
                    {
                        Sitecore.Form.Core.Utility.Utils.RemoveVersionOrItem(fieldItem);
                    }

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Updates the field.
        /// </summary>
        /// <param name="field">
        /// The field.
        /// </param>
        /// <param name="sectionItem">
        /// The section item.
        /// </param>
        protected void UpdateField(FieldDefinition field, Item sectionItem)
        {
            Assert.ArgumentNotNull(field, "field");

            field.CreateCorrespondingItem(sectionItem ?? this.Form, this.language);
        }

        /// <summary>
        /// Updates the section.
        /// </summary>
        /// <param name="section">The section.</param>
        /// <returns>
        /// The section.
        /// </returns>
        protected Item UpdateSection(SectionDefinition section)
        {
            if (section != null && this.Form != null && (!string.IsNullOrEmpty(section.SectionID) || this.definition.IsHasVisibleSection()))
            {
                return section.CreateCorrespondingItem(this.Form, this.language);
            }

            return null;
        }

        /// <summary>
        /// Updates the IDs.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="oldForm">The old form.</param>
        /// <param name="newForm">The new form.</param>
        /// <returns>
        /// The IDs.
        /// </returns>
        private static string UpdateIDs(string text, FormItem oldForm, FormItem newForm)
        {
            string newText = text;
            if (!string.IsNullOrEmpty(newText))
            {
                IEnumerable<ID> ids = IDUtil.GetIDs(newText);
                foreach (ID id in ids)
                {
                    ID newId = FindMatch(id, oldForm, newForm);

                    if (!ID.IsNullOrEmpty(newId))
                    {
                        newText = newText.Replace(id.ToString(), newId.ToString());
                    }
                }
            }

            return newText;
        }

        private void SynchronizeField(Item sectionItem, FieldDefinition field)
        {
            if (!this.DeleteFieldIsEmpty(field))
            {
                this.UpdateField(field, sectionItem);
            }
            else
            {
                field.UpdateSharedFields(sectionItem, null, this.database);
            }
        }

        #endregion
    }
}
