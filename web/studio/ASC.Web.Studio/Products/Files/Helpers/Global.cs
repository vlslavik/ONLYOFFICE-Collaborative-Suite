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
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Configuration;
using ASC.Collections;
using ASC.Core.Users;
using ASC.Data.Storage;
using ASC.Data.Storage.S3;
using ASC.Files.Core;
using ASC.Files.Core.Data;
using ASC.Thrdparty.Configuration;
using ASC.Web.Core;
using ASC.Web.Files.Resources;
using ASC.Web.Files.Utils;
using ASC.Web.Studio.Core.Users;
using ASC.Web.Studio.Utility;
using Microsoft.Practices.ServiceLocation;
using Newtonsoft.Json.Linq;
using File = ASC.Files.Core.File;
using ASC.Files.Core.Security;
using ASC.Core;
using System.Collections.Generic;
using log4net;
using Constants = ASC.Core.Configuration.Constants;

namespace ASC.Web.Files.Classes
{
    public class Global
    {
        static Global()
        {
            const StringComparison cmp = StringComparison.InvariantCultureIgnoreCase;

            EnableUploadFilter = Boolean.TrueString.Equals(WebConfigurationManager.AppSettings["files.upload-filter"] ?? "false", cmp);

            EnableEmbedded = Boolean.TrueString.Equals(WebConfigurationManager.AppSettings["files.docservice.embedded"] ?? "true", cmp);

            BitlyUrl = KeyStorage.Get("bitly-url");
        }

        #region Property

        public const int MaxTitle = 170;

        public static readonly Regex InvalidTitleChars = new Regex("[@#$%&*\\+:;\"'<>?|\\\\/]");

        public static bool EnableUploadFilter { get; private set; }

        public static bool EnableEmbedded { get; private set; }

        public static string BitlyUrl { get; private set; }

        public static bool IsAdministrator
        {
            get { return FileSecurity.IsAdministrator(SecurityContext.CurrentAccount.ID); }
        }

        public static string GetDocDbKey()
        {
            const string dbKey = "UniqueDocument";
            var resultKey = CoreContext.Configuration.GetSetting(dbKey);

            if (!String.IsNullOrEmpty(resultKey)) return resultKey;

            resultKey = Guid.NewGuid().ToString();
            CoreContext.Configuration.SaveSetting(dbKey, resultKey);

            return resultKey;
        }

        #region GlobalFolderID

        private static readonly IDictionary<int, object> ProjectsRootFolderCache =
            new SynchronizedDictionary<int, object>(); /*Use SYNCHRONIZED for cross thread blocks*/

        public static object FolderProjects
        {
            get
            {
                if (CoreContext.Configuration.Personal) return null;

                if (WebItemManager.Instance[WebItemManager.ProjectsProductID].IsDisabled()) return null;

                using (var folderDao = DaoFactory.GetFolderDao())
                {
                    object result;
                    if (!ProjectsRootFolderCache.TryGetValue(TenantProvider.CurrentTenantID, out result))
                    {
                        result = folderDao.GetFolderIDProjects(true);

                        ProjectsRootFolderCache[TenantProvider.CurrentTenantID] = result;
                    }

                    return result;
                }
            }
        }

        private static readonly IDictionary<string, object> UserRootFolderCache =
            new SynchronizedDictionary<string, object>(); /*Use SYNCHRONIZED for cross thread blocks*/

        public static object FolderMy
        {
            get
            {
                if (SecurityContext.IsAuthenticated)
                {
                    if (CoreContext.UserManager.GetUsers(SecurityContext.CurrentAccount.ID).IsVisitor())
                        return 0;

                    var cacheKey = string.Format("my/{0}/{1}", TenantProvider.CurrentTenantID, SecurityContext.CurrentAccount.ID);

                    object myFolderId;
                    if (!UserRootFolderCache.TryGetValue(cacheKey, out myFolderId))
                    {
                        myFolderId = GetFolderIdAndProccessFirstVisit(true);
                        UserRootFolderCache[cacheKey] = myFolderId;
                    }
                    return myFolderId;
                }
                return 0;
            }
        }

        private static readonly IDictionary<int, object> CommonFolderCache =
            new SynchronizedDictionary<int, object>(); /*Use SYNCHRONIZED for cross thread blocks*/

        public static object FolderCommon
        {
            get
            {
                if (CoreContext.Configuration.Personal) return null;

                object commonFolderId;
                if (!CommonFolderCache.TryGetValue(TenantProvider.CurrentTenantID, out commonFolderId))
                {
                    commonFolderId = GetFolderIdAndProccessFirstVisit(false);
                    CommonFolderCache[TenantProvider.CurrentTenantID] = commonFolderId;
                }
                return commonFolderId;
            }
        }

        private static readonly IDictionary<int, object> ShareFolderCache =
            new SynchronizedDictionary<int, object>(); /*Use SYNCHRONIZED for cross thread blocks*/

        public static object FolderShare
        {
            get
            {
                if (CoreContext.Configuration.Personal) return null;

                object sharedFolderId;
                if (!ShareFolderCache.TryGetValue(TenantProvider.CurrentTenantID, out sharedFolderId))
                {
                    using (var folderDao = DaoFactory.GetFolderDao())
                    {
                        sharedFolderId = folderDao.GetFolderIDShare(true);
                    }

                    if (!sharedFolderId.Equals(0))
                        ShareFolderCache[TenantProvider.CurrentTenantID] = sharedFolderId;
                }

                return sharedFolderId;
            }
        }

        private static readonly IDictionary<string, object> TrashFolderCache =
            new SynchronizedDictionary<string, object>(); /*Use SYNCHRONIZED for cross thread blocks*/

        public static object FolderTrash
        {
            get
            {
                var cacheKey = string.Format("trash/{0}/{1}", TenantProvider.CurrentTenantID, SecurityContext.CurrentAccount.ID);

                object trashFolderId;
                if (!TrashFolderCache.TryGetValue(cacheKey, out trashFolderId))
                {
                    using (var folderDao = DaoFactory.GetFolderDao())
                        trashFolderId = SecurityContext.IsAuthenticated ? folderDao.GetFolderIDTrash(true) : 0;
                    TrashFolderCache[cacheKey] = trashFolderId;
                }
                return trashFolderId;
            }
        }

        #endregion

        #endregion

        public static ILog Logger
        {
            get { return LogManager.GetLogger("ASC.Files"); }
        }

        public static IDaoFactory DaoFactory
        {
            get
            {
                try
                {
                    return ServiceLocator.Current.GetInstance<IDaoFactory>();
                }
                catch (Exception error)
                {
                    Logger.Error("Could not resolve IDaoFactory instance. Using default DaoFactory instead.", error);
                    return new DaoFactory();
                }                
            }
        }

        public static IDataStore GetStore()
        {
            return StorageFactory.GetStorage(TenantProvider.CurrentTenantID.ToString(), FileConstant.StorageModule);
        }

        public static IDataStore GetStoreTemplate()
        {
            return StorageFactory.GetStorage(String.Empty, FileConstant.StorageDomainTemplate);
        }

        public static FileSecurity GetFilesSecurity()
        {
            return new FileSecurity(DaoFactory);
        }

        public static string ReplaceInvalidCharsAndTruncate(string title)
        {
            if (String.IsNullOrEmpty(title)) return title;
            title = title.Trim();
            if (MaxTitle < title.Length)
            {
                var pos = title.LastIndexOf('.');
                if (MaxTitle - 20 < pos)
                {
                    title = title.Substring(0, MaxTitle - (title.Length - pos)) + title.Substring(pos);
                }
                else
                {
                    title = title.Substring(0, MaxTitle);
                }
            }
            return InvalidTitleChars.Replace(title, "_");
        }

        public static string GetUserName(Guid userId)
        {
            if (userId.Equals(SecurityContext.CurrentAccount.ID)) return FilesCommonResource.Author_Me;
            if (userId.Equals(Constants.Guest.ID)) return FilesCommonResource.Guest;

            var userInfo = CoreContext.UserManager.GetUsers(userId);
            if (userInfo.Equals(ASC.Core.Users.Constants.LostUser)) return CustomNamingPeople.Substitute<FilesCommonResource>("ProfileRemoved");

            return userInfo.DisplayUserName(false);
        }

        #region Generate start documents

        private static object GetFolderIdAndProccessFirstVisit(bool my)
        {
            using (var folderDao = DaoFactory.GetFolderDao())
            using (var fileDao = DaoFactory.GetFileDao())
            {
                var id = my ? folderDao.GetFolderIDUser(false) : folderDao.GetFolderIDCommon(false);

                if (Equals(id, 0)) //TODO: think about 'null'
                {
                    id = my ? folderDao.GetFolderIDUser(true) : folderDao.GetFolderIDCommon(true);

                    //Copy start document
                    try
                    {
                        var path = string.Empty;
                        IDataStore storeTemp = null;
                        if (my)
                        {
                            var partner = CoreContext.PaymentManager.GetApprovedPartner();
                            if (partner != null)
                            {
                                storeTemp = StorageFactory.GetStorage(partner.Id, "startdocuments");
                                if (!storeTemp.IsDirectory(path)
                                    || storeTemp.ListFilesRelative("", path, "*", false).Length == 0)
                                {
                                    storeTemp = null;
                                }
                            }
                        }

                        if (storeTemp == null)
                        {
                            storeTemp = GetStoreTemplate();
                            var culture = my ? CoreContext.UserManager.GetUsers(SecurityContext.CurrentAccount.ID).GetCulture() : CoreContext.TenantManager.GetCurrentTenant().GetCulture();

                            path = FileConstant.StartDocPath + culture.TwoLetterISOLanguageName + "/";
                            if (!storeTemp.IsDirectory(path))
                                path = FileConstant.StartDocPath + "default/";
                            path += my ? "my/" : "corporate/";
                        }

                        SaveStartDocument(folderDao, fileDao, id, path, storeTemp);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex);
                    }
                }

                return id;
            }
        }

        private static void SaveStartDocument(IFolderDao folderDao, IFileDao fileDao, object folderId, string path, IDataStore storeTemp)
        {
            foreach (var file in storeTemp.ListFilesRelative("", path, "*", false))
            {
                SaveFile(fileDao, folderId, path + file, storeTemp);
            }

            if (storeTemp is S3Storage) return;

            foreach (var folderUri in storeTemp.List(path, false))
            {
                var folderName = Path.GetFileName(folderUri.ToString());

                var subFolder = folderDao.SaveFolder(new Folder
                    {
                        Title = folderName,
                        ParentFolderID = folderId
                    });

                SaveStartDocument(folderDao, fileDao, subFolder.ID, path + folderName + "/", storeTemp);
            }
        }

        private static void SaveFile(IFileDao fileDao, object folder, string filePath, IDataStore storeTemp)
        {
            using (var stream = storeTemp.IronReadStream("", filePath, 10))
            {
                var fileName = Path.GetFileName(filePath);
                var file = new File
                    {
                        Title = fileName,
                        ContentLength = stream.Length,
                        FolderID = folder,
                    };
                stream.Position = 0;
                try
                {
                    file = fileDao.SaveFile(file, stream);

                    FileMarker.MarkAsNew(file);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            }
        }

        #endregion

        public static IEnumerable<Guid> GetProjectTeam(FileEntry fileEntry)
        {
            using (var folderDao = DaoFactory.GetFolderDao())
            {
                var path = folderDao.GetBunchObjectID(fileEntry.RootFolderId);

                var projectID = path.Split('/').Last();

                if (String.IsNullOrEmpty(projectID)) return new List<Guid>();

                var apiUrl = String.Format("project/{0}/team.json", projectID);

                JToken responseApi;
                try
                {
                    responseApi = JObject.Parse(GetApiResponse(apiUrl))["response"];
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    return new List<Guid>();
                }

                if (!(responseApi is JArray)) return new List<Guid>();

                return responseApi.Children()
                                  .Where(x => x["canReadFiles"].Value<bool>())
                                  .Select(x => new Guid(x["id"].Value<String>()))
                                  .Where(id => id != SecurityContext.CurrentAccount.ID);
            }
        }

        private static string GetApiResponse(string apiUrl)
        {
            var requestUriBuilder = new UriBuilder(HttpContext.Current != null ? HttpContext.Current.Request.Url.Scheme : Uri.UriSchemeHttp,
                                                   CoreContext.TenantManager.GetCurrentTenant().TenantDomain);

            apiUrl = string.Format("{0}/{1}", WebConfigurationManager.AppSettings["api.url"].Trim('~', '/'), apiUrl.TrimStart('/'));

            if (CoreContext.TenantManager.GetCurrentTenant().TenantAlias == "localhost")
            {
                var virtualDir = WebConfigurationManager.AppSettings["core.virtual-dir"];

                if (string.IsNullOrEmpty(virtualDir))
                    virtualDir = "asc";

                apiUrl = virtualDir.Trim('/') + "/" + apiUrl;
            }

            requestUriBuilder.Path = apiUrl;

            var apiRequest = (HttpWebRequest)WebRequest.Create(requestUriBuilder.Uri);
            apiRequest.AllowAutoRedirect = true;
            apiRequest.CookieContainer = new CookieContainer();
            apiRequest.CookieContainer.Add(new Cookie("asc_auth_key", GetAuthCookie(), "/", CoreContext.TenantManager.GetCurrentTenant().TenantDomain));

            using (var apiResponse = (HttpWebResponse)apiRequest.GetResponse())
            using (var respStream = apiResponse.GetResponseStream())
            {
                return respStream != null ? new StreamReader(respStream).ReadToEnd() : null;
            }

        }

        private static string GetAuthCookie()
        {
            //fake user authentication to create authentication cookie
            return SecurityContext.AuthenticateMe(SecurityContext.CurrentAccount.ID);
        }
    }
}