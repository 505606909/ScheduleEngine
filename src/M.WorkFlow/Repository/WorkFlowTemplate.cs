﻿using System.Linq;
using FD.Simple.DB;
using FD.Simple.Utils.Agent;
using M.WorkFlow.Model;
using System;
using System.Collections.Generic;
using FD.Simple.Utils.Provider;

namespace M.WFDesigner.Repository
{

    [Autowired]
    public class WorkFlowTemplate : IWorkFlowTemplate
    {
        [Autowired]
        public DataAccess DataAccess { get; set; }
        [Autowired]
        public IDBContext DBContext { get; set; }

        public List<WFFlowEntity> GetFlowsByServiceId(string serviceId)
        {
            if (string.IsNullOrWhiteSpace(serviceId))
            {
                throw new ArgumentException($"{nameof(serviceId)} is not null");
            }

            var filter = TableFilter.New().Equals("id", serviceId);
            var serviceEntity = DataAccess.Query(WFServiceEntity.TableCode).FixField("*").Where(filter).QueryFirst<WFServiceEntity>();
            if (serviceEntity == null)
            {
                throw new Exception("没有找到流程业务");
            }

            filter = TableFilter.New().Equals("serviceId", serviceId).Equals("istemplate", 1);
            var query = DataAccess.Query(WFFlowEntity.TableCode).FixField("*").Sort("version desc").Where(filter);
            var list = query.QueryList<WFFlowEntity>().ToList();

            var currentFlow = list.Where(flow => flow.ID == serviceEntity.Currentflowid).FirstOrDefault();
            if (currentFlow != null)
            {
                currentFlow.CurrentTag = 1;
            }
            return list;
        }
        public List<WFServiceEntity> GetAllService()
        {
            var query = DataAccess.Query(WFServiceEntity.TableCode).FixField("id,name").WhereAll();
            var services = query.QueryList<WFServiceEntity>().ToList();
            if (services == null)
            {
                throw new Exception("没有找到场景信息业务");
            }
            return services;
        }
        public WFFlowEntity GetWFTemplate(string flowId)
        {
            if (string.IsNullOrWhiteSpace(flowId))
            {
                throw new ArgumentException($"{nameof(flowId)} is not null");
            }
            var filter = TableFilter.New().Equals("id", flowId);
            var query = DataAccess.Query(WFFlowEntity.TableCode).FixField("id").Where(filter);
            var flowEntity = query.QueryFirst<WFFlowEntity>();
            if (flowEntity != null)
            {
                filter = TableFilter.New().Equals("flowid", flowId);
                flowEntity.Links = DataAccess.Query(WFLinkEntity.TableCode).FixField("id,begintaskid,endtaskid,memo").Where(filter).QueryList<WFLinkEntity>().ToList();
                flowEntity.Tasks = DataAccess.Query(WFTaskEntity.TableCode).FixField("id,name,type,x,y").Where(filter).QueryList<WFTaskEntity>().ToList();
            }
            return flowEntity;
        }

        public CommonResult<int> ReleaseFlow(string flowId)
        {
            if (string.IsNullOrWhiteSpace(flowId))
            {
                return new WarnResult($"{nameof(flowId)} is not null");
            }
            var flowEntity = DataAccess.Query(WFFlowEntity.TableCode).FixField("*").Where(TableFilter.New().Equals("Id", flowId)).QueryFirst<WFFlowEntity>();

            if (flowEntity != null && flowEntity.Released == 0)
            {
                flowEntity.Released = 1;
                flowEntity.ReleaseDate = DateTime.Now;
                DataAccess.Update(flowEntity);
            }
            return 1;
        }

        public CommonResult<int> SetCurrentFow(string serviceId, string flowId)
        {
            if (string.IsNullOrWhiteSpace(flowId))
            {
                return new WarnResult($"{nameof(flowId)} is not null");
            }
            var flowEntity = DataAccess.Query(WFFlowEntity.TableCode).FixField("*").Where(TableFilter.New().Equals("Id", flowId)).QueryFirst<WFFlowEntity>();

            if (flowEntity == null || flowEntity.Released == 0)
            {
                return new WarnResult("必须发布后的流程才能设置为当前");
            }
            var serviceEntity = DataAccess.Query(WFServiceEntity.TableCode).FixField("*").Where(TableFilter.New().Equals("Id", serviceId)).QueryFirst<WFServiceEntity>();

            if (serviceEntity == null)
            {
                return new WarnResult("没有找到业务流程");
            }

            if (serviceEntity.Currentflowid != flowId)
            {
                serviceEntity.Currentflowid = flowId;
                DataAccess.Update(serviceEntity);
            }
            return 1;
        }

        public int UpdateTemplate(WFFlowEntity flowEntity)
        {
            if (null == flowEntity)
            {
                throw new ArgumentException($"{nameof(flowEntity)} is not null");
            }
            using (var trans = TransHelper.BeginTrans())
            {
                foreach (var task in flowEntity?.Tasks ?? new List<WFTaskEntity>())
                {
                    var taskEntity = new WFTaskEntity();
                    taskEntity.State = task.State;
                    if (task.State == EDBEntityState.Added)
                    {
                        taskEntity.Flowid = flowEntity.ID;
                        taskEntity.Name = task.Name;
                    }
                    taskEntity.ID = task.ID;
                    taskEntity.X = task.X;
                    taskEntity.Y = task.Y;
                    taskEntity.Type = task.Type;
                    taskEntity.Name = task.Name;
                    DataAccess.Update(taskEntity);
                }
                foreach (var link in flowEntity?.Links ?? new List<WFLinkEntity>())
                {
                    var linkEntity = new WFLinkEntity();
                    linkEntity.State = link.State;
                    if (link.State == EDBEntityState.Added)
                    {
                        linkEntity.Flowid = flowEntity.ID;
                    }
                    linkEntity.ID = link.ID;
                    linkEntity.Begintaskid = link.Begintaskid;
                    linkEntity.Endtaskid = link.Endtaskid;
                    var count = DataAccess.Update(linkEntity);
                }
                trans.Commit();
            }

            return 1;
        }
    }
}
