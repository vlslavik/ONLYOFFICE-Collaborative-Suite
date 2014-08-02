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

#region Import

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Caching;
using System.Web.Configuration;
using ASC.Common.Data;
using ASC.Common.Data.Sql;
using ASC.Common.Data.Sql.Expressions;
using ASC.Core.Caching;
using log4net;

#endregion

namespace ASC.CRM.Core.Dao
{
    public class AbstractDao
    {
        private readonly string dbid;

        protected readonly List<EntityType> _supportedEntityType = new List<EntityType>();
        protected readonly ILog _log = LogManager.GetLogger("ASC.CRM");

        protected readonly Cache _cache = HttpRuntime.Cache;

        protected readonly String _contactCacheKey;
        protected readonly String _dealCacheKey;
        protected readonly String _caseCacheKey;
        protected readonly String _taskCacheKey;

        protected readonly String _invoiceCacheKey;
        protected readonly String _invoiceItemCacheKey;
        protected readonly String _invoiceTaxCacheKey;
        protected readonly String _invoiceLineCacheKey;

        protected AbstractDao(int tenantID, String storageKey)
        {
            TenantID = tenantID;
            dbid = storageKey;

            _supportedEntityType.Add(EntityType.Company);
            _supportedEntityType.Add(EntityType.Person);
            _supportedEntityType.Add(EntityType.Contact);
            _supportedEntityType.Add(EntityType.Opportunity);
            _supportedEntityType.Add(EntityType.Case);
            
            _contactCacheKey = String.Concat(TenantID, "/contact");
            _dealCacheKey = String.Concat(TenantID, "/deal");
            _caseCacheKey = String.Concat(TenantID, "/case");
            _taskCacheKey = String.Concat(TenantID, "/task");

            _invoiceCacheKey = String.Concat(TenantID, "/invoice");
            _invoiceItemCacheKey = String.Concat(TenantID, "/invoiceitem");
            _invoiceTaxCacheKey = String.Concat(TenantID, "/invoicetax");
            _invoiceLineCacheKey = String.Concat(TenantID, "/invoiceline");

            if (_cache.Get(_contactCacheKey) == null)
            {
                _cache.Insert(_contactCacheKey, String.Empty);
            }
            if (_cache.Get(_dealCacheKey) == null)
            {
                _cache.Insert(_dealCacheKey, String.Empty);
            }
            if (_cache.Get(_caseCacheKey) == null)
            {
                _cache.Insert(_caseCacheKey, String.Empty);
            }
            if (_cache.Get(_taskCacheKey) == null)
            {
                _cache.Insert(_taskCacheKey, String.Empty);
            }

            if (_cache.Get(_invoiceCacheKey) == null)
            {
                _cache.Insert(_invoiceCacheKey, String.Empty);
            }
            if (_cache.Get(_invoiceItemCacheKey) == null)
            {
                _cache.Insert(_invoiceItemCacheKey, String.Empty);
            }
            if (_cache.Get(_invoiceTaxCacheKey) == null)
            {
                _cache.Insert(_invoiceTaxCacheKey, String.Empty);
            }
            if (_cache.Get(_invoiceLineCacheKey) == null)
            {
                _cache.Insert(_invoiceLineCacheKey, String.Empty);
            }
        }

        protected DbManager GetDb()
        {
            return new DbManager(dbid);
        }

        protected int TenantID
        {
            get;
            private set;
        }

        protected List<int> SearchByTags(EntityType entityType, int[] exceptIDs, IEnumerable<String> tags)
        {
            if (tags == null || !tags.Any())
                throw new ArgumentException();

            var tagIDs = new List<int>();
            using (var db = GetDb())
            {
                foreach (var tag in tags)
                    tagIDs.Add(db.ExecuteScalar<int>(Query("crm_tag")
                          .Select("id")
                          .Where(Exp.Eq("entity_type", (int)entityType) & Exp.Eq("title", tag))));

                var sqlQuery = new SqlQuery("crm_entity_tag")
                    .Select("entity_id")
                    .Select("count(*) as count")

                    .GroupBy("entity_id")
                    .Having(Exp.Eq("count", tags.Count()));

                if (exceptIDs != null && exceptIDs.Length > 0)
                    sqlQuery.Where(Exp.In("entity_id", exceptIDs) & Exp.Eq("entity_type", (int)entityType));
                else
                    sqlQuery.Where(Exp.Eq("entity_type", (int)entityType));

                sqlQuery.Where(Exp.In("tag_id", tagIDs));

                return db.ExecuteList(sqlQuery).ConvertAll(row => Convert.ToInt32(row[0]));
            }
        }

        protected Dictionary<int, int[]> GetRelativeToEntity(int[] contactID, EntityType entityType, int[] entityID)
        {
            var sqlQuery = new SqlQuery("crm_entity_contact");

            if (contactID != null && contactID.Length > 0 && (entityID == null || entityID.Length == 0))
                sqlQuery.Select("entity_id", "contact_id").Where(Exp.Eq("entity_type", entityType) & Exp.In("contact_id", contactID));
            else if (entityID != null && entityID.Length > 0 && (contactID == null || contactID.Length == 0))
                sqlQuery.Select("entity_id", "contact_id").Where(Exp.Eq("entity_type", entityType) & Exp.In("entity_id", entityID));

            using (var db = GetDb())
            {
                var sqlResult = db.ExecuteList(sqlQuery);

                return sqlResult.GroupBy(item => item[0])
                       .ToDictionary(item => Convert.ToInt32(item.Key),
                                    item => item.Select(x => Convert.ToInt32(x[1])).ToArray());
            }
        }

        protected int[] GetRelativeToEntity(int? contactID, EntityType entityType, int? entityID)
        {
            using (var db = GetDb())
            {
                return GetRelativeToEntity(contactID, entityType, entityID, db);
            }
        }

        protected int[] GetRelativeToEntity(int? contactID, EntityType entityType, int? entityID, DbManager db)
        {
            var sqlQuery = new SqlQuery("crm_entity_contact");

            if (contactID.HasValue && !entityID.HasValue)
                sqlQuery.Select("entity_id").Where(Exp.Eq("entity_type", entityType) & Exp.Eq("contact_id", contactID.Value));
            else if (!contactID.HasValue && entityID.HasValue)
                sqlQuery.Select("contact_id").Where(Exp.Eq("entity_type", entityType) & Exp.Eq("entity_id", entityID.Value));

            return db.ExecuteList(sqlQuery).Select(row => Convert.ToInt32(row[0])).ToArray();
        }

        protected void SetRelative(int[] contactID, EntityType entityType, int entityID)
        {
            using (var db = GetDb())
            using (var tx = db.BeginTransaction())
            {

                var sqlDelete = new SqlDelete("crm_entity_contact");

                if (entityID == 0)
                    throw new ArgumentException();

                sqlDelete.Where(Exp.Eq("entity_type", entityType) & Exp.Eq("entity_id", entityID));

                db.ExecuteNonQuery(sqlDelete);

                if (!(contactID == null || contactID.Length == 0))
                    foreach (var id in contactID)
                        SetRelative(id, entityType, entityID, db);

                tx.Commit();
            }
        }

        protected void SetRelative(int contactID, EntityType entityType, int entityID, DbManager db)
        {
            db.ExecuteNonQuery(new SqlInsert("crm_entity_contact", true)
                                       .InColumnValue("entity_id", entityID)
                                       .InColumnValue("entity_type", (int)entityType)
                                       .InColumnValue("contact_id", contactID)
                                    );
        }

        protected void RemoveRelative(int[] contactID, EntityType entityType, int[] entityID, DbManager db)
        {

            if (contactID != null && contactID.Length == 0 && entityID != null && entityID.Length == 0)
                throw new ArgumentException();

            var sqlQuery = new SqlDelete("crm_entity_contact");

            if (contactID != null && contactID.Length > 0)
                sqlQuery.Where(Exp.In("contact_id", contactID));

            if (entityID != null && entityID.Length > 0)
                sqlQuery.Where(Exp.In("entity_id", entityID) & Exp.Eq("entity_type", (int)entityType));

            db.ExecuteNonQuery(sqlQuery);
        }

        protected void RemoveRelative(int contactID, EntityType entityType, int entityID, DbManager db)
        {
            int[] contactIDs = null;
            int[] entityIDs = null;


            if (contactID > 0)
                contactIDs = new[] { contactID };

            if (entityID > 0)
                entityIDs = new[] { entityID };


            RemoveRelative(contactIDs, entityType, entityIDs, db);
        }


        public int SaveOrganisationLogo(byte[] bytes)
        {
            var logo_id = 0;
            using (var db = GetDb())
            {
                logo_id = db.ExecuteScalar<int>(
                                          Insert("crm_organisation_logo")
                                         .InColumnValue("id", 0)
                                         .InColumnValue("content", Convert.ToBase64String(bytes))
                                         .InColumnValue("create_on", DateTime.UtcNow)
                                         .InColumnValue("create_by", ASC.Core.SecurityContext.CurrentAccount.ID)
                                         .Identity(1, 0, true));
            }
            return logo_id;
        }

        public string GetOrganisationLogoBase64(int logo_id)
        {
            var content = "";
            if (logo_id <= 0) throw new ArgumentException();

            using (var db = GetDb())
            {
                content = db.ExecuteList(
                                        new SqlQuery("crm_organisation_logo")
                                        .Select("content")
                                        .Where(Exp.Eq("id", logo_id))).Select(row => Convert.ToString(row[0])).FirstOrDefault();
            }
            return content;
        }

        #region HasCRMActivity

        public bool HasActivity()
        {
            var q = new SqlExp(string.Format(@"select 1 where exists(select 1 from crm_case where tenant_id = {0}) or " +
                "exists(select 1 from crm_deal where tenant_id = {0}) or exists(select 1 from crm_task where tenant_id = {0}) or " +
                "exists(select 1 from crm_contact where tenant_id = {0})", TenantID));
            
            using (var db = GetDb())
            {
                return db.ExecuteScalar<bool>(q);
            }
        }

        #endregion

        protected SqlQuery Query(string table)
        {
            return new SqlQuery(table).Where(GetTenantColumnName(table), TenantID);
        }

        protected SqlDelete Delete(string table)
        {
            return new SqlDelete(table).Where(GetTenantColumnName(table), TenantID);
        }

        protected SqlInsert Insert(string table)
        {
            return new SqlInsert(table, true).InColumns(GetTenantColumnName(table)).Values(TenantID);
        }

        protected SqlUpdate Update(string table)
        {
            return new SqlUpdate(table).Where(GetTenantColumnName(table), TenantID);
        }

        protected string GetTenantColumnName(string table)
        {
            var tenant = "tenant_id";
            if (!table.Contains(" ")) return tenant;
            return table.Substring(table.IndexOf(" ")).Trim() + "." + tenant;
        }


        protected static Guid ToGuid(object guid)
        {
            var str = guid as string;
            return !string.IsNullOrEmpty(str) ? new Guid(str) : Guid.Empty;
        }

        protected Exp BuildLike(string[] columns, string[] keywords)
        {
            return BuildLike(columns, keywords, true);
        }

        protected Exp BuildLike(string[] columns, string[] keywords, bool startWith)
        {
            var like = Exp.Empty;
            foreach (var keyword in keywords)
            {
                var keywordLike = Exp.Empty;
                foreach (string column in columns)
                {
                    keywordLike = keywordLike |
                                  Exp.Like(column, keyword, startWith ? SqlLike.StartWith : SqlLike.EndWith) |
                                  Exp.Like(column, ' ' + keyword);
                }
                like = like & keywordLike;
            }
            return like;
        }

    }
}
