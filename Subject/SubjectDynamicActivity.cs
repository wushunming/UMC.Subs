using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Collections;
using System.Reflection;
using UMC.Data.Entities;
using UMC.Web;

namespace UMC.Subs.Activities
{
    class SubjectDynamicActivity : WebActivity
    {

        public override void ProcessActivity(WebRequest request, WebResponse response)
        {
            var subEntity = Utility.CMS.ObjectEntity<ProjectDynamic>();
            var user = UMC.Security.Identity.Current;

            Guid? projectId = UMC.Data.Utility.Guid(this.AsyncDialog("Id", "auto"));//.Value;


            var webr = UMC.Data.WebResource.Instance();
            var time = Utility.IntParse(this.AsyncDialog("Time", g =>
             {
                 var form = request.SendValues ?? new UMC.Web.WebMeta();

                 int limit = UMC.Data.Utility.IntParse(form["limit"] as string, 25);
                 int start = UMC.Data.Utility.IntParse(form["start"] as string, 0);

                 string sort = form[("sort")] as string;
                 string dir = form[("dir")] as string;



                 var Keyword = (form["Keyword"] as string ?? String.Empty);

                 if (String.IsNullOrEmpty(Keyword) == false)
                 {
                     subEntity.Where.And().Like(new ProjectDynamic { Title = Keyword });
                 }
                 if (projectId.HasValue)
                 {
                     subEntity.Where.And().Equal(new ProjectDynamic
                     {
                         project_id = projectId

                     }).Entities.Order.Desc(new ProjectDynamic { Time = 0 });

                 }
                 else
                 {
                     subEntity.Where.And().Equal(new ProjectDynamic
                     {
                         user_id = user.Id.Value

                     }).Entities.Order.Desc(new ProjectDynamic { Time = 0 });
                 }
                 var subs = new List<ProjectDynamic>();
                 var cateids = new List<Guid>();
                 subEntity.Query(start, limit, dr =>
                 {
                     subs.Add(dr);
                     if (projectId.HasValue)
                     {
                         cateids.Add(dr.user_id ?? Guid.Empty);
                     }
                     else if (dr.project_id.HasValue)
                     {
                         cateids.Add(dr.project_id.Value);
                     }

                 });
                 var users = new List<User>();
                 var projects = new List<Project>();
                 if (projectId.HasValue)
                 {
                     if (cateids.Count > 0)
                     {
                         Utility.CMS.ObjectEntity<User>().Where.And().In(new User { Id = cateids[0] }, cateids.ToArray())
                                 .Entities.Query(dr => users.Add(dr));
                     }
                 }
                 else
                 {

                     if (cateids.Count > 0)
                     {
                         Utility.CMS.ObjectEntity<Project>().Where.And().In(new Project { Id = cateids[0] }, cateids.ToArray())
                                     .Entities.Query(dr => projects.Add(dr));
                     }
                 }
                 var data = new System.Data.DataTable();
                 data.Columns.Add("id");
                 data.Columns.Add("title");
                 data.Columns.Add("tid");
                 data.Columns.Add("desc");
                 data.Columns.Add("time");
                 data.Columns.Add("type");
                 data.Columns.Add("src");

                 if (projectId.HasValue)
                 {
                     data.Columns.Add("alias");
                 }
                 else
                 {
                     data.Columns.Add("name");
                 }
                 foreach (var sub in subs)
                 {
                     var sType = "成员动态";
                     switch (sub.Type)
                     {
                         case DynamicType.Member:
                             break;
                         case DynamicType.Portfolio:
                             sType = "文集动态";
                             break;
                         case DynamicType.Project:
                             sType = "项目动态";
                             break;
                         case DynamicType.Subject:
                             sType = "文档动态";
                             break;
                         case DynamicType.ProjectItem:
                             sType = "栏位动态";
                             break;
                     }

                     if (projectId.HasValue)
                     {

                         var user2 = users.Find(d => d.Id == sub.user_id) ?? new User();
                         data.Rows.Add(sub.user_id, sub.Title, sub.Time, sub.Explain, Utility.TimeSpan(sub.Time ?? 0), sType, webr.ResolveUrl(sub.user_id ?? Guid.Empty, "1", 5),
                            user2.Alias);
                     }
                     else
                     {
                         var pro = projects.Find(d => d.Id == sub.project_id) ?? new Project();
                         data.Rows.Add(sub.user_id, sub.Title, sub.Time, sub.Explain, Utility.TimeSpan(sub.Time ?? 0), sType, webr.ResolveUrl(sub.project_id ?? Guid.Empty, "1", 5),
                            pro.Caption);
                     }
                 }
                 var hashc = new System.Collections.Hashtable();
                 hashc["data"] = data;
                 var total = subEntity.Count(); ;
                 hashc["total"] = total;// subEntity.Count();
                 if (total == 0)
                 {
                     if (projectId.HasValue)
                     {
                         hashc["msg"] = "未有项目动态";
                     }
                     else
                     {
                         hashc["msg"] = "未有动态";

                     }
                 }
                 response.Redirect(hashc);
                 return this.DialogValue("none");
             }), 0);

            var dynamic = Utility.CMS.ObjectEntity<ProjectDynamic>()
                .Where.And().Equal(new ProjectDynamic { user_id = projectId, Time = time }).Entities.Single();
            if (dynamic != null)
            {
                switch (dynamic.Type)
                {
                    case DynamicType.Member:
                        response.Redirect(request.Model, "Account", dynamic.refer_id.ToString());
                        break;
                    case DynamicType.Project:
                        var pr = Utility.CMS.ObjectEntity<Project>()
                  .Where.And().Equal(new Project { Id = dynamic.refer_id }).Entities.Single();
                        if (pr != null)
                        {
                            response.Redirect(request.Model, "ProjectUI", dynamic.refer_id.ToString());
                        }
                        else
                        {
                            this.Prompt("动态关联已经错误");
                        }
                        break;
                    case DynamicType.Subject:
                        var pr3 = Utility.CMS.ObjectEntity<Subject>()
                  .Where.And().Equal(new Subject { Id = dynamic.refer_id }).Entities.Single();
                        if (pr3 != null)
                        {

                            if (request.Url.Query.Contains("_v=Sub") && String.IsNullOrEmpty(pr3.Code) == false)
                            {
                                var sPro = Utility.CMS.ObjectEntity<Project>()
                           .Where.And().Equal(new Project { Id = pr3.project_id.Value }).Entities.Single();
                                var iItem = Utility.CMS.ObjectEntity<ProjectItem>()
                       .Where.And().Equal(new ProjectItem { Id = pr3.project_item_id.Value }).Entities.Single();
                                if (sPro != null && iItem != null)
                                {
                                    if (pr3.Visible > 0)
                                    {
                                        if (pr3.Status < 1)
                                        {
                                            if (user.IsAuthenticated == false)
                                            {
                                                this.Prompt("此文档未发布，未有权限查看");
                                            }
                                            if (sPro.user_id == user.Id)
                                            {

                                            }
                                            else
                                            {
                                                var member = Utility.CMS.ObjectEntity<ProjectMember>().Where.And().Equal(new ProjectMember
                                                {
                                                    project_id = sPro.Id.Value,
                                                    user_id = user.Id
                                                }).Entities.Single();
                                                if (member != null)
                                                {
                                                    switch (member.AuthType)
                                                    {
                                                        case WebAuthType.Admin:
                                                        case WebAuthType.User:
                                                            break;
                                                        default:
                                                            this.Prompt("此文档未发布");
                                                            break;
                                                    }

                                                }
                                                else
                                                {
                                                    this.Prompt("此文档未发布");
                                                }
                                            }
                                        }

                                        this.Context.Send("Subject.Path", new WebMeta().Put("Path", String.Format("{0}/{1}/{2}", sPro.Code, iItem.Code, pr3.Code)), true);
                                    }
                                    else
                                    {
                                        this.Prompt("此文档已经删除");
                                    }


                                }
                                else
                                {

                                    response.Redirect(request.Model, "UIData", dynamic.refer_id.ToString());
                                }
                            }
                            else
                            {
                                response.Redirect(request.Model, "UIData", dynamic.refer_id.ToString());
                            }

                        }
                        else
                        {
                            this.Prompt("此动态为信息动态");
                        }
                        break;
                    default:
                        this.Prompt("此动态为信息动态");
                        break;
                }
            }
            //var Project = projectId.Value;
            //var project = Utility.CMS.ObjectEntity<Project>()
            //   .Where.And().Equal(new Project { Id = Project }).Entities.Single();


        }

    }
}