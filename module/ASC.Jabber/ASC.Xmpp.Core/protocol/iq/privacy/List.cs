/*
(c) Copyright Ascensio System SIA 2010-2014

This program is a free software product.
You can redistribute it and/or modify it under the terms 
of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of 
any third-party rights.

This program is distributed WITHOUT ANY WARRANTY; without even the implied warranty 
of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see 
the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html

You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.

The  interactive user interfaces in modified source and object code versions of the Program must 
display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
 
Pursuant to Section 7(b) of the License you must retain the original Product logo when 
distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under 
trademark law for use of our trademarks.
 
All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode
*/

// // --------------------------------------------------------------------------------------------------------------------
// // <copyright company="Ascensio System Limited" file="List.cs">
// //   
// // </copyright>
// // <summary>
// //   (c) Copyright Ascensio System Limited 2008-2012
// // </summary>
// // --------------------------------------------------------------------------------------------------------------------

using ASC.Xmpp.Core.utils.Xml.Dom;

namespace ASC.Xmpp.Core.protocol.iq.privacy
{
    public class List : Element
    {
        public List()
        {
            TagName = "list";
            Namespace = Uri.IQ_PRIVACY;
        }

        public List(string name) : this()
        {
            Name = name;
        }

        public string Name
        {
            get { return GetAttribute("name"); }
            set { SetAttribute("name", value); }
        }

        /// <summary>
        ///   Gets all Rules (Items) when available
        /// </summary>
        /// <returns> </returns>
        public Item[] GetItems()
        {
            ElementList el = SelectElements(typeof (Item));
            int i = 0;
            var result = new Item[el.Count];
            foreach (Item itm in el)
            {
                result[i] = itm;
                i++;
            }
            return result;
        }

        /// <summary>
        ///   Adds a rule (item) to the list
        /// </summary>
        /// <param name="itm"> </param>
        public void AddItem(Item item)
        {
            AddChild(item);
        }

        public void AddItems(Item[] items)
        {
            foreach (Item item in items)
            {
                AddChild(item);
            }
        }

        /// <summary>
        ///   Remove all items/rules of this list
        /// </summary>
        public void RemoveAllItems()
        {
            RemoveTags(typeof (Item));
        }
    }
}