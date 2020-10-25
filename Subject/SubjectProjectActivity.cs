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
    class SubjectProjectActivity : WebActivity
    {

        //class SubjectProjectDialog : Web.UIGridDialog
        //{

        //    protected override Hashtable GetHeader()
        //    {
        //        var header = new Header("Id", 25);
        //        header.AddField("Caption", "项目");
        //        return header.GetHeader();


        //    }
        //    public Guid UserId { get; set; }
        //    protected override Hashtable GetData(IDictionary paramsKey)
        //    {
        //        var start = UMC.Data.Utility.Parse((paramsKey["start"] ?? "0").ToString(), 0);
        //        var limit = UMC.Data.Utility.Parse((paramsKey["limit"] ?? "25").ToString(), 25);

        //        var scheduleEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Project>();

        //        scheduleEntity.Where.And().Equal(new Project { user_id = this.UserId });

        //        scheduleEntity.Order.Asc(new Project { Sequence = 0 });

        //        var list = new List<Project>();

        //        scheduleEntity.Query(new Project()
        //        {
        //            Id = Guid.Empty,
        //            Code = String.Empty,
        //            Caption = String.Empty
        //        }, start, limit, d => list.Add(d));

        //        var hash = new Hashtable();
        //        hash["data"] = list;
        //        hash["total"] = scheduleEntity.Count();
        //        return hash;
        //    }
        //}
        public override void ProcessActivity(WebRequest request, WebResponse response)
        {
            var user = Security.Identity.Current;
            var sid = Web.UIDialog.AsyncDialog("Id", d =>
            {
                this.Prompt("请输入Id");
                return this.DialogValue("none");
            });
            var cmdId = UMC.Data.Utility.Guid(sid) ?? Guid.Empty;

            var project = new Project();

            var objectEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Project>();
            project = objectEntity.Where.And().Equal(new Project
            {
                Id = cmdId
            }).Entities.Single() ?? project;

            if (project.Id.HasValue == false)
            {
                if (user.IsAuthenticated == false)
                {
                    response.Redirect(new WebMeta());
                }

                var userId = user.Id;
                var members = Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectMember>()
                   .Where.And().Equal(new Data.Entities.ProjectMember { user_id = userId })
                   .Entities.Count() + Utility.CMS.ObjectEntity<UMC.Data.Entities.Project>()
                   .Where.And().Equal(new Data.Entities.Project { user_id = userId })
                   .Entities.Count();


                var suject =
                Utility.CMS.ObjectEntity<Subject>()
                   .Where.And().Equal(new Subject { user_id = userId })
                   .And().GreaterEqual(new Subject { Visible = 0 })
                   .Entities.GroupBy().Sum(new Subject { Reply = 0 })
                   .Sum(new Subject { Look = 0 }).Count(new Subject { Seq = 0 }).Single();


                var dUser = Utility.CMS.ObjectEntity<Data.Entities.Account>().Where.And().Equal(new Data.Entities.Account { user_id = userId, Type = Security.Account.SIGNATURE_ACCOUNT_KEY }).Entities.Single() ?? new Account();
                var form = (request.SendValues ?? new UMC.Web.WebMeta()).GetDictionary();
                var webr2 = UMC.Data.WebResource.Instance();
                response.Redirect(new WebMeta().Put("Look", suject.Look ?? 0).Put("Reply", suject.Reply ?? 0).Put("Count", suject.Seq ?? 0)
                    .Put("Src", webr2.ImageResolve(userId.Value, "1", 4) + "?" + Utility.TimeSpan()).Put("Alias", user.Alias).Put("Signature", dUser.Name ?? "未有签名").Put("ProjectCount", members));

            }

            var cache = UMC.Configuration.ConfigurationManager.DataCache(project.Id.Value, "Data", 1000, (k, v, c) =>
            {
                var Hash = new Hashtable();

                var suject =
                Utility.CMS.ObjectEntity<Subject>()
                   .Where.And().Equal(new Subject { project_id = project.Id })
                   .And().GreaterEqual(new Subject { Visible = 0 })
                   .Entities.GroupBy().Sum(new Subject { Reply = 0 })
                   .Sum(new Subject { Look = 0 }).Count(new Subject { Seq = 0 }).Single();





                var count = Utility.CMS.ObjectEntity<ProjectMember>()
                   .Where.And().Equal(new ProjectMember { project_id = project.Id }).Entities.Count() + 1;
                var Comments = Utility.CMS.ObjectEntity<Comment>()
                   .Where.And().Equal(new Comment { project_id = project.Id }).Entities.Count();
                Hash["Reply"] = Comments;// suject.Reply ?? 0;
                Hash["Look"] = suject.Look ?? 0;
                Hash["Count"] = suject.Seq ?? 0;
                Hash["MemberCount"] = count;
                return Hash;

            });
            var webr = UMC.Data.WebResource.Instance();
            var hash = cache.CacheData;
            hash["Desc"] = project.Description ?? "未填写";
            if (project.user_id == user.Id)
            {
                hash["IsAuth"] = true;
                hash["Code"] = project.Code;
                hash["joinText"] = "创立者";

            }
            else
            {
                var IsOk =
                    Utility.CMS.ObjectEntity<ProjectMember>()
                       .Where.And().Equal(new ProjectMember { project_id = project.Id, user_id = user.Id }).Entities.Count() > 0;

                hash["joinText"] = IsOk ? "已加入项目" : "关注项目";
            }
            var projectSetting = Utility.CMS.ObjectEntity<ProjectSetting>()
                    .Where.And().Equal(new ProjectSetting
                    {
                        project_id = project.Id,
                        Type = 11
                    }).Entities.Count();
            hash["FollowSrc"] = Data.Utility.QRUrl(new Uri(request.Url, String.Format("{1}{0}/follow", project.Code, UMC.Data.WebResource.Instance().WebDomain())).AbsoluteUri);
            if (projectSetting > 0)
            {
                hash["Scans"] = "钉钉或天天录"; ;
            }
            else
            {
                hash["Scans"] = "天天录"; ;

            }
            hash["Src"] = webr.ImageResolve(project.Id.Value, "1", 4) + "?" + Utility.TimeSpan(project.ModifiedTime ?? DateTime.Now);
            hash["Name"] = project.Caption;
            if ((project.PublishTime ?? 0) + 3600 < Utility.TimeSpan())// DateTime.Now)
            {
                hash["releaseId"] = project.Id;//.ToString());
            }
            response.Redirect(cache.CacheData);

        }

    }
}