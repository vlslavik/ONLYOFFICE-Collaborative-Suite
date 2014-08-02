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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ASC.Collections;
using ASC.Common.Data;
using ASC.Common.Data.Sql;
using ASC.Common.Data.Sql.Expressions;
using ASC.Core.Tenants;
using ASC.Projects.Core.DataInterfaces;
using ASC.Projects.Core.Domain;

namespace ASC.Projects.Data.DAO
{
    internal class CachedSubtaskDao : SubtaskDao
    {
        private readonly HttpRequestDictionary<Subtask> _subtaskCache = new HttpRequestDictionary<Subtask>("subtask");

        public CachedSubtaskDao(string dbId, int tenantID)
            : base(dbId, tenantID)
        {
        }

        public override void Delete(int id)
        {
            ResetCache(id);
            base.Delete(id);
        }

        public override Subtask GetById(int id)
        {
            return _subtaskCache.Get(id.ToString(CultureInfo.InvariantCulture), () => GetBaseById(id));
        }

        private Subtask GetBaseById(int id)
        {
            return base.GetById(id);
        }

        public override Subtask Save(Subtask subtask)
        {
            if (subtask != null)
            {
                ResetCache(subtask.ID);
            }
            return base.Save(subtask);
        }

        private void ResetCache(int subtaskId)
        {
            _subtaskCache.Reset(subtaskId.ToString(CultureInfo.InvariantCulture));
        }
    }

    class SubtaskDao : BaseDao, ISubtaskDao
    {
        public SubtaskDao(string dbId, int tenantID)
            : base(dbId, tenantID)
        {
        }

        public List<Subtask> GetSubtasks(int taskid)
        {
            using (var db = new DbManager(DatabaseId))
            {
                return db.ExecuteList(CreateQuery().Where("task_id", taskid)).ConvertAll(ToSubTask);
            }
        }

        public void GetSubtasks(ref List<Task> tasks)
        {
            using (var db = new DbManager(DatabaseId))
            {
                var taskIds = tasks.Select(t => t.ID).ToArray();
                var subtasks = db.ExecuteList(CreateQuery().Where(Exp.In("task_id", taskIds)))//bug: there may be too large set of tasks
                    .ConvertAll(ToSubTask);

                tasks = tasks.GroupJoin(subtasks, task => task.ID, subtask => subtask.Task, (task, subtaskCol) =>
                            {
                                task.SubTasks.AddRange(subtaskCol.ToList());
                                return task;
                            }).ToList();
            }
        }

        public virtual Subtask GetById(int id)
        {
            using (var db = new DbManager(DatabaseId))
            {
                return db.ExecuteList(CreateQuery().Where("id", id)).ConvertAll(ToSubTask).SingleOrDefault();
            }
        }

        public List<Subtask> GetById(ICollection<int> ids)
        {
            using (var db = new DbManager(DatabaseId))
            {
                return db.ExecuteList(CreateQuery().Where(Exp.In("id", ids.ToArray()))).ConvertAll(ToSubTask);
            }
        }

        public List<Subtask> GetUpdates(DateTime from, DateTime to)
        {
            using (var db = new DbManager(DatabaseId))
            {
                return db.ExecuteList(CreateQuery().Select("status_changed")
                                                 .Where(Exp.Between("create_on", from, to) |
                                                        Exp.Between("last_modified_on", from, to) |
                                                        Exp.Between("status_changed", from, to)))
                                .ConvertAll(x =>
                                                {
                                                    var st = ToSubTask(x);
                                                    st.StatusChangedOn = Convert.ToDateTime(x.Last());
                                                    return st;
                                                }).ToList();
            }
        }

        public int GetSubtaskCount(int taskid, params TaskStatus[] statuses)
        {
            using (var db = new DbManager(DatabaseId))
            {
                var query = Query(SubtasksTable)
                    .SelectCount()
                    .Where("task_id", taskid);
                if (statuses != null && 0 < statuses.Length)
                {
                    query.Where(Exp.In("status", statuses));
                }
                return db.ExecuteScalar<int>(query);
            }
        }

               public virtual Subtask Save(Subtask subtask)
        {
            using (var db = new DbManager(DatabaseId))
            {
                using (var tr = db.BeginTransaction())
                {
                    var insert = Insert(SubtasksTable)
                        .InColumnValue("id", subtask.ID)
                        .InColumnValue("task_id", subtask.Task)
                        .InColumnValue("title", subtask.Title)
                        .InColumnValue("responsible_id", subtask.Responsible.ToString())
                        .InColumnValue("status", subtask.Status)
                        .InColumnValue("create_by", subtask.CreateBy.ToString())
                        .InColumnValue("create_on", TenantUtil.DateTimeToUtc(subtask.CreateOn))
                        .InColumnValue("last_modified_by", subtask.LastModifiedBy.ToString())
                        .InColumnValue("last_modified_on", TenantUtil.DateTimeToUtc(subtask.LastModifiedOn))
                        .InColumnValue("status_changed", TenantUtil.DateTimeToUtc(subtask.StatusChangedOn, DateTime.Now))
                        .Identity(1, 0, true);

                    subtask.ID = db.ExecuteScalar<int>(insert);

                    tr.Commit();


                    return subtask;

                }
            }
        }


        public void CloseAllSubtasks(Task task)
        {
            using (var db = new DbManager(DatabaseId))
            {
                db.ExecuteNonQuery(
                    Update(SubtasksTable)
                        .Set("status", TaskStatus.Closed)
                        .Set("last_modified_by", CurrentUserID)
                        .Set("last_modified_on", TenantUtil.DateTimeToUtc(TenantUtil.DateTimeNow()))
                        .Set("status_changed", TenantUtil.DateTimeToUtc(TenantUtil.DateTimeNow()))
                        .Where("status", TaskStatus.Open)
                        .Where("task_id", task.ID));
            }

        }

        public virtual void Delete(int id)
        {
            using (var db = new DbManager(DatabaseId))
            {
                db.ExecuteNonQuery(Delete(SubtasksTable).Where("id", id));
            }
        }

        private SqlQuery CreateQuery()
        {
            return new SqlQuery(SubtasksTable)
                .Select("id", "title", "responsible_id", "status", "create_by", "create_on", "last_modified_by", "last_modified_on", "task_id")
                .OrderBy("status", true)
                .OrderBy("(case status when 1 then create_on else status_changed end)", true)
                .Where("tenant_id", Tenant);
        }

        private static Subtask ToSubTask(IList<object> r)
        {
            return new Subtask
                       {
                           ID = Convert.ToInt32(r[0]),
                           Title = (string) r[1],
                           Responsible = ToGuid(r[2]),
                           Status = (TaskStatus) Convert.ToInt32(r[3]),
                           CreateBy = ToGuid(r[4]),
                           CreateOn = TenantUtil.DateTimeFromUtc(Convert.ToDateTime(r[5])),
                           LastModifiedBy = ToGuid(r[6]),
                           LastModifiedOn = TenantUtil.DateTimeFromUtc(Convert.ToDateTime(r[7])),
                           Task = Convert.ToInt32(r[8])
                       };
        }
    }
}
