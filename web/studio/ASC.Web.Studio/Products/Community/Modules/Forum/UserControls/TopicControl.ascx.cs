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

using System;
using System.Text;
using System.Web;
using System.Web.UI;
using ASC.Web.Studio.Utility.HtmlUtility;
using AjaxPro;
using ASC.Core;
using ASC.Forum;
using ASC.Web.Core.Utility.Skins;
using ASC.Web.Studio.Utility;
using ASC.Web.UserControls.Forum.Common;
using System.Collections.Generic;

namespace ASC.Web.UserControls.Forum
{
    [AjaxNamespace("TopicManager")]
    public partial class TopicControl : UserControl
    {
        public Topic Topic { get; set; }

        public bool IsShowThreadName { get; set; }

        public string TopicCSSClass { get; set; }

        protected string _imageURL { get; set; }

        public Guid SettingsID { get; set; }

        public bool IsEven { get; set; }

        protected Settings _settings;

        private ForumManager _forumManager;

        protected void Page_Load(object sender, EventArgs e)
        {
            Utility.RegisterTypeForAjax(GetType());

            _settings = ForumManager.GetSettings(SettingsID);
            _forumManager = _settings.ForumManager;

            TopicCSSClass = IsEven ? "tintMedium" : "";

            if (!Topic.IsApproved
                && _forumManager.ValidateAccessSecurityAction(ForumAction.ApprovePost, Topic))
                TopicCSSClass = "tintDangerous";

            _imageURL = _forumManager.GetTopicImage(Topic);

            _tagsPanel.Visible = (Topic.Tags != null && Topic.Tags.Count > 0);
            tagRepeater.DataSource = Topic.Tags;
            tagRepeater.DataBind();
        }

        protected string RenderModeratorFunctions()
        {
            var sb = new StringBuilder();
            var bitMask = 0;
            // 1 - aprroved
            // 2 - delete
            // 3 - stiky
            // 4 - close
            // 5 - move
            // 6 - edit

            if (!Topic.IsApproved && _forumManager.ValidateAccessSecurityAction(ForumAction.ApprovePost, Topic))
                bitMask += 1;

            if (_forumManager.ValidateAccessSecurityAction(ForumAction.TopicDelete, Topic))
                bitMask += 2;

            if (_forumManager.ValidateAccessSecurityAction(ForumAction.TopicSticky, Topic))
                bitMask += 4;

            if (_forumManager.ValidateAccessSecurityAction(ForumAction.TopicClose, Topic))
                bitMask += 8;

            //if (userAuthLevel >= m_topic.Levels.DeleteLevel && userAuthLevel >= m_topic.Levels.PostCreateLevel)
            //{
            //    bitMask += 16;
            //}
            if (_forumManager.ValidateAccessSecurityAction(ForumAction.TopicEdit, Topic))
                bitMask += 32;

            if (bitMask > 0)
            {
                sb.Append("<span id=\"forum_mf_" + Topic.ID + "\" style=\"margin-left:5px;\">");
                sb.Append("<a class=\"link\" href=\"javascript:ForumManager.ShowTopicModeratorFunctions('" + Topic.ID + "'," + bitMask + "," + (int)Topic.Status + ",'" + _settings.LinkProvider.EditTopic(Topic.ID) + "');\"><img alt='#' border=0 align='absmiddle' src='" + WebImageSupplier.GetAbsoluteWebPath("down.png", _settings.ImageItemID) + "' title='" + Resources.ForumUCResource.ModeratorFunctions + "' /></a>");
                sb.Append("</span>");
            }
            return sb.ToString();

        }

        protected string RenderPages()
        {
            var sb = new StringBuilder();
            var amount = Convert.ToInt32(Math.Ceiling(Topic.PostCount/(_settings.PostCountOnPage*1.0)));
            if (amount > 1)
            {
                sb.Append(" <span class=\"text-medium-describe\">(");
                for (var j = 1; j <= amount; j++)
                {
                    if (j == 4 && j != amount)
                    {
                        sb.Append("... ");
                        sb.Append("<a class='link' href=\"posts.aspx?&t=" + Topic.ID + "&p=" + amount + "\">" + amount + "</a>");
                        break;
                    }
                    sb.Append("<a class='link' href=\"posts.aspx?&t=" + Topic.ID + "&p=" + j + "\">" + j + "</a>");

                    if (j != amount)
                        sb.Append(" ");

                }
                sb.Append(")</span>");
            }
            return sb.ToString();
        }

        protected string RenderLastUpdates()
        {
            if (Topic.RecentPostID == 0)
                return "";

            var recentPostURL = _settings.LinkProvider.RecentPost(Topic.RecentPostID, Topic.ID, Topic.PostCount);

            var sb = new StringBuilder();

            var fullText = HtmlUtility.GetText(Topic.RecentPostText);
            var text = HtmlUtility.GetText(Topic.RecentPostText, 20);

            sb.Append("<div style='margin-bottom:5px;'><a class = 'link' title=\"" + HttpUtility.HtmlEncode(fullText) + "\" href=\"" + recentPostURL + "\">" + HttpUtility.HtmlEncode(text) + "</a></div>");

            sb.Append("<div class = 'link' style='overflow: hidden; max-width: 180px;'>" + ASC.Core.Users.StudioUserInfoExtension.RenderCustomProfileLink(CoreContext.UserManager.GetUsers(Topic.RecentPostAuthorID), _settings.ProductID, "describe-text", "link gray") + "</div>");
            sb.Append("<div style='margin-top:5px;'>");
            sb.Append(DateTimeService.DateTime2StringTopicStyle(Topic.RecentPostCreateDate));
            sb.Append("<a href=\"" + recentPostURL + "\"><img hspace=\"5\" align=\"absmiddle\" alt=\"&raquo;\" title=\"&raquo;\" border=\"0\" src=\"" + WebImageSupplier.GetAbsoluteWebPath("goto.png", _settings.ImageItemID) + "\"/></a>");
            sb.Append("</div>");

            return sb.ToString();
        }

        [AjaxMethod(HttpSessionStateRequirement.ReadWrite)]
        public AjaxResponse DoApprovedTopic(int idTopic, Guid settingsID)
        {
            _forumManager = ForumManager.GetForumManager(settingsID);
            var resp = new AjaxResponse { rs2 = idTopic.ToString() };

            var topic = ForumDataProvider.GetTopicByID(TenantProvider.CurrentTenantID, idTopic);
            if (topic == null)
            {
                resp.rs1 = "0";
                resp.rs3 = Resources.ForumUCResource.ErrorAccessDenied;
                return resp;
            }

            if (!_forumManager.ValidateAccessSecurityAction(ForumAction.ApprovePost, topic))
            {
                resp.rs1 = "0";
                resp.rs3 = Resources.ForumUCResource.ErrorAccessDenied;
                return resp;
            }

            try
            {
                ForumDataProvider.ApproveTopic(TenantProvider.CurrentTenantID, topic.ID);
                resp.rs1 = "1";
            }
            catch (Exception e)
            {
                resp.rs1 = "0";
                resp.rs3 = HttpUtility.HtmlEncode(e.Message);
            }

            return resp;
        }

        [AjaxMethod(HttpSessionStateRequirement.ReadWrite)]
        public AjaxResponse DoCloseTopic(int idTopic, Guid settingsID)
        {
            _forumManager = ForumManager.GetForumManager(settingsID);
            var resp = new AjaxResponse { rs2 = idTopic.ToString() };

            var topic = ForumDataProvider.GetTopicByID(TenantProvider.CurrentTenantID, idTopic);
            if (topic == null)
            {
                resp.rs1 = "0";
                resp.rs3 = Resources.ForumUCResource.ErrorAccessDenied;
                return resp;
            }

            if (!_forumManager.ValidateAccessSecurityAction(ForumAction.TopicClose, topic))
            {
                resp.rs1 = "0";
                resp.rs3 = Resources.ForumUCResource.ErrorAccessDenied;
                return resp;
            }

            topic.Closed = !topic.Closed;
            try
            {
                ForumDataProvider.UpdateTopic(TenantProvider.CurrentTenantID, topic.ID, topic.Title, topic.Sticky, topic.Closed);

                resp.rs1 = "1";
                resp.rs3 = topic.Closed ? Resources.ForumUCResource.SuccessfullyCloseTopicMessage : Resources.ForumUCResource.SuccessfullyOpenTopicMessage;
                resp.rs4 = topic.Closed ? Resources.ForumUCResource.OpenTopicButton : Resources.ForumUCResource.CloseTopicButton;
                resp.status = topic.Closed ? "close" : "open";
            }
            catch (Exception e)
            {
                resp.rs1 = "0";
                resp.rs3 = HttpUtility.HtmlEncode(e.Message);
            }

            return resp;
        }

        [AjaxMethod(HttpSessionStateRequirement.ReadWrite)]
        public AjaxResponse DoStickyTopic(int idTopic, Guid settingsID)
        {
            _forumManager = ForumManager.GetForumManager(settingsID);
            var resp = new AjaxResponse { rs2 = idTopic.ToString() };

            var topic = ForumDataProvider.GetTopicByID(TenantProvider.CurrentTenantID, idTopic);
            if (topic == null)
            {
                resp.rs1 = "0";
                resp.rs3 = Resources.ForumUCResource.ErrorAccessDenied;
                return resp;
            }

            if (!_forumManager.ValidateAccessSecurityAction(ForumAction.TopicSticky, topic))
            {
                resp.rs1 = "0";
                resp.rs3 = Resources.ForumUCResource.ErrorAccessDenied;
                return resp;
            }

            topic.Sticky = !topic.Sticky;
            try
            {
                ForumDataProvider.UpdateTopic(TenantProvider.CurrentTenantID, topic.ID, topic.Title, topic.Sticky, topic.Closed);

                resp.rs1 = "1";
                if (topic.Sticky)
                {

                    resp.rs3 = Resources.ForumUCResource.SuccessfullyStickyTopicMessage;
                    resp.rs4 = Resources.ForumUCResource.ClearStickyTopicButton;
                }
                else
                {
                    resp.rs3 = Resources.ForumUCResource.SuccessfullyClearStickyTopicMessage;
                    resp.rs4 = Resources.ForumUCResource.StickyTopicButton;
                }
            }
            catch (Exception e)
            {
                resp.rs1 = "0";
                resp.rs3 = HttpUtility.HtmlEncode(e.Message);
            }
            return resp;
        }

        [AjaxMethod(HttpSessionStateRequirement.ReadWrite)]
        public AjaxResponse DoDeleteTopic(int idTopic, Guid settingsID)
        {
            _forumManager = ForumManager.GetForumManager(settingsID);
            var resp = new AjaxResponse { rs2 = idTopic.ToString() };

            var topic = ForumDataProvider.GetTopicByID(TenantProvider.CurrentTenantID, idTopic);
            if (topic == null)
            {
                resp.rs1 = "0";
                resp.rs3 = Resources.ForumUCResource.ErrorAccessDenied;
                return resp;
            }

            if (!_forumManager.ValidateAccessSecurityAction(ForumAction.TopicDelete, topic))
            {
                resp.rs1 = "0";
                resp.rs3 = Resources.ForumUCResource.ErrorAccessDenied;
                return resp;
            }

            try
            {
                List<int> removedPostIDs;
                var attachmantOffsetPhysicalPaths = ForumDataProvider.RemoveTopic(TenantProvider.CurrentTenantID, topic.ID, out removedPostIDs);

                resp.rs1 = "1";
                resp.rs3 = Resources.ForumUCResource.SuccessfullyDeleteTopicMessage;
                resp.rs4 = topic.ThreadID.ToString();
                _forumManager.RemoveAttachments(attachmantOffsetPhysicalPaths.ToArray());

                removedPostIDs.ForEach(idPost => CommonControlsConfigurer.FCKUploadsRemoveForItem(_forumManager.Settings.FileStoreModuleID, idPost.ToString()));
            }
            catch (Exception ex)
            {
                resp.rs1 = "0";
                resp.rs3 = ex.Message.HtmlEncode();
            }

            return resp;
        }
    }
}