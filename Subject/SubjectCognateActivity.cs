//using System;
//using System.Collections.Generic;
//using System.Text;
//using System.Threading;
//using System.Collections;
//using System.Reflection;
//using UMC.Data.Entities;
//using UMC.Web;
//using UMC.Web.UI;

//namespace UMC.Subs.Activities
//{
//    class SubjectCognateActivity : WebActivity
//    {
//        public override void ProcessActivity(WebRequest request, WebResponse response)
//        {

//            var projectId = UMC.Data.Utility.Guid(this.AsyncDialog("Project", g =>
//           {
//               return new Web.UITextDialog() { Title = "项目" };
//           })).Value;

//            var user = Security.Identity.Current;
//            var project = Utility.CMS.ObjectEntity<UMC.Data.Entities.Project>()
//                   .Where.And().Equal(new Project { Id = projectId }).Entities.Single();

//            var referId = UMC.Data.Utility.Guid(this.AsyncDialog("Refer", g =>
//            {
//                var paramsKey = request.SendValues ?? request.Arguments;
//                if (paramsKey.ContainsKey("start") == false)
//                {
//                    var buider = new UISectionBuilder(request.Model, request.Command, request.Arguments);
//                    buider.CloseEvent("Cognate");
//                    this.Context.Send(buider.Builder(), true);
//                }
//                var start = UMC.Data.Utility.Parse((paramsKey["start"] ?? "0").ToString(), 0);
//                var limit = UMC.Data.Utility.Parse((paramsKey["limit"] ?? "25").ToString(), 25);
//                UITitle title = UITitle.Create();

//                title.Title = "关联项目";

//                var ui = UISection.Create(new UIHeader().Search("请搜索"), title);

//                var Keyword = paramsKey.Get("Keyword");
//                if (String.IsNullOrEmpty(Keyword))
//                {
//                    ui.Add("Desc", new UMC.Web.WebMeta().Put("desc", "请输入项目关键字搜索").Put("icon", "\uF002"), new UMC.Web.WebMeta().Put("desc", "{icon}\n{desc}"),

//                        new UIStyle().Align(1).Color(0xaaa).Padding(20, 20).BgColor(0xfff).Size(12).Name("icon", new UIStyle().Font("wdk").Size(60)));
//                    response.Redirect(ui);
//                }
//                var ids = new List<Guid>();
//                var pros = new List<Project>();
//                Utility.CMS.ObjectEntity<Project>().Where.Or().Like(new Project { Caption = Keyword, Code = Keyword }).Entities.Query(start, limit, dr =>
//                {
//                    pros.Add(dr);
//                    ids.Add(dr.Id.Value);
//                });

//                if (ids.Count > 0)
//                {
//                    var webr = UMC.Data.WebResource.Instance();
//                    var subs = new List<Subject>();
//                    Utility.CMS.ObjectEntity<UMC.Data.Entities.Subject>()
//                               .Where.And().In(new Subject { project_id = ids[0] }, ids.ToArray())
//                               .Entities.GroupBy(new Subject { project_id = Guid.Empty }).Count(new Subject { Seq = 0 }).Query(dr => subs.Add(dr));

//                    foreach (var p in pros)
//                    {
//                        var sub = subs.Find(s => s.project_id == p.Id);
//                        var desc = new UIIconNameDesc(new UIIconNameDesc.Item(webr.ResolveUrl(p.Id.Value, "1", "4"), p.Caption,
//                            String.Format("知识{0}篇", sub == null ? 0 : sub.Seq))
//                            .Click(Web.UIClick.Query(new WebMeta().Put("Project", p.Id))));


//                        if (p.user_id == user.Id)
//                        {
//                            desc.Button("我的", null, 0x25b864);
//                        }
//                        ui.Add(desc);

//                    }
//                }
//                else
//                {
//                    ui.Add("Desc", new UMC.Web.WebMeta().Put("desc", String.Format("未搜索到“{0}”关联项目", Keyword)).Put("icon", "\uF016"), new UMC.Web.WebMeta().Put("desc", "{icon}\n{desc}"),

//                        new UIStyle().Align(1).Color(0xaaa).Padding(20, 20).BgColor(0xfff).Size(12).Name("icon", new UIStyle().Font("wdk").Size(60)));

//                }


//                response.Redirect(ui);

//                return this.DialogValue("Project");

//            })).Value;
//            if (projectId == referId)
//            {
//                this.Prompt("不能关联自己");
//            }

//            var caEntity = Utility.CMS.ObjectEntity<ProjectCognate>().Where.And().Equal(new ProjectCognate
//            {
//                project_id = projectId,
//                refer_project_id = referId
//            }).Entities;
//            if (caEntity.Count() > 0)
//            {
//                caEntity.Delete();
//            }
//            else
//            {
//                caEntity.Insert(new ProjectCognate { CreationTime = DateTime.Now, project_id = project.Id, refer_project_id = referId });
//            }

//            this.Context.Send("Cognate", true);

//        }

//    }
//}