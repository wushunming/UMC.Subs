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
    class SubjectTeamActivity : WebActivity
    {

        public override void ProcessActivity(WebRequest request, WebResponse response)
        {
            Guid Team = UMC.Data.Utility.Guid(this.AsyncDialog("Project", "auto")).Value;
            var project = Utility.CMS.ObjectEntity<UMC.Data.Entities.Project>()
                .Where.And().Equal(new Project { Id = Team }).Entities.Single();

            var subEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectMember>();
            subEntity.Where.And().Equal(new ProjectMember { project_id = project.Id });
            var sId = this.AsyncDialog("Id", g =>
             {
                 var webr = UMC.Data.WebResource.Instance();
                 var form = request.SendValues ?? new UMC.Web.WebMeta();

                 int limit = UMC.Data.Utility.IntParse(form["limit"] as string, 25);
                 int start = UMC.Data.Utility.IntParse(form["start"] as string, 0);

                 string sort = form[("sort")] as string;
                 string dir = form[("dir")] as string;



                 var Keyword = (form["Keyword"] as string ?? String.Empty);

                 if (!String.IsNullOrEmpty(sort) && sort.StartsWith("_") == false)
                 {
                     if (String.IsNullOrEmpty(Keyword))
                     {
                         switch (sort)
                         {
                             case "Admin":
                                 subEntity.Where.And().Equal(new ProjectMember { AuthType = Web.WebAuthType.Admin });
                                 break;
                             case "Write":
                                 subEntity.Where.And().Equal(new ProjectMember { AuthType = Web.WebAuthType.User });
                                 break;
                             case "Read":
                                 subEntity.Where.And().Equal(new ProjectMember { AuthType = Web.WebAuthType.Guest });
                                 break;
                             default:
                                 if (dir == "DESC")
                                 {
                                     subEntity.Order.Desc(sort);
                                 }
                                 else
                                 {
                                     subEntity.Order.Asc(sort);
                                 }
                                 break;
                         }
                     }
                 }
                 else
                 {
                     subEntity.Order.Desc(new ProjectMember { CreationTime = DateTime.MaxValue });
                 }
                 if (String.IsNullOrEmpty(Keyword) == false)
                 {
                     subEntity.Where.And().In("user_id", Utility.CMS.ObjectEntity<User>()
                         .Where.And().Like(new User { Alias = Keyword }).Entities.Script(new User { Id = Guid.Empty }));
                 }
                 var subs = new List<ProjectMember>();
                 var uids = new List<Guid>();
                 subEntity.Query(start, limit, dr =>
                 {
                     subs.Add(dr);
                     uids.Add(dr.user_id ?? Guid.Empty);

                 });
                 var cates = new List<User>();
                 if (uids.Count > 0)
                 {
                     Utility.CMS.ObjectEntity<User>().Where.And().In(new User { Id = uids[0] }, uids.ToArray())
                         .Entities.Query(dr => cates.Add(dr));
                 }
                 var data = new System.Data.DataTable();
                 data.Columns.Add("project_id");
                 data.Columns.Add("user_id");
                 data.Columns.Add("alias");
                 data.Columns.Add("auth");
                 data.Columns.Add("time");
                 data.Columns.Add("src");
                 foreach (var sub in subs)
                 {

                     var s = "互联网用户";
                     switch (sub.AuthType)
                     {
                         case Web.WebAuthType.All:
                             break;
                         case Web.WebAuthType.Guest:
                             s = "读者";
                             break;
                         case Web.WebAuthType.User:
                             s = "专栏作家";
                             break;
                         case Web.WebAuthType.Admin:
                             s = "管理员";
                             break;
                     }
                     var user2 = cates.Find(d => d.Id == sub.user_id) ?? new User() { Alias = sub.Alias };
                     data.Rows.Add(sub.project_id, sub.user_id, user2.Alias, s, sub.CreationTime, webr.ResolveUrl(sub.user_id ?? Guid.Empty, "1", 4));
                 }
                 var hashc = new System.Collections.Hashtable();
                 hashc["data"] = data;
                 var total = subEntity.Count(); ;
                 hashc["total"] = total;// subEntity.Count();
                 if (total == 0)
                 {
                     switch (sort)
                     {
                         case "Admin":
                             hashc["msg"] = "未有管理员成员";
                             break;
                         case "Write":
                             hashc["msg"] = "未有专栏作家成员";
                             break;
                         case "Read":
                             hashc["msg"] = "未有读者成员";
                             break;
                         default:
                             if (String.IsNullOrEmpty(Keyword) == false)
                             {
                                 hashc["msg"] = String.Format("未搜索到“{0}”对应的团队成员", Keyword);
                             }
                             else
                             {

                                 hashc["msg"] = "现在还未有团队成员";
                             }
                             break;
                     }
                 }
                 response.Redirect(hashc);
                 return this.DialogValue("none");
             });
            var Id = Utility.Guid(sId) ?? Guid.Empty;
            var user = UMC.Security.Identity.Current;
            if (Id == Guid.Empty)
            {
                if (user.IsAuthenticated == false)
                {
                    response.Redirect("Account", "Login");
                }
                if (project.user_id == user.Id)
                {
                    this.Prompt("你是创立者，拥有最大权限");
                }

                switch (sId)
                {
                    case "follow":
                        subEntity.Where.And().Equal(new ProjectMember { user_id = user.Id });
                        if (subEntity.Count() > 0)
                        {
                            this.Prompt("你已经加入此项目");
                        }
                        else
                        {
                            this.AsyncDialog("Confirm", g => new UIConfirmDialog("你确认加入此项目吗"));
                        }
                        break;
                    case "Self":
                        break;
                    default:

                        UMC.Web.UIFormDialog.AsyncDialog("From", d =>
                        {
                            var key = Utility.Scanning(new UIClick(new WebMeta().Put("Project", Team).Put("Id", "Self")).Send(request.Model, request.Command), 0);
                            var url = new Uri(String.Format("{1}Click/{0}", Utility.Parse62Encode(key), UMC.Data.WebResource.Instance().WebDomain()));
                            var form = new UMC.Web.UIFormDialog() { Title = "扫码加入" };

                            form.AddImage(new Uri(Data.Utility.QRUrl(url.AbsoluteUri)));
                            form.AddTextValue()
                            .Put("使用方式", "使用微信或者钉钉“扫一扫”");

                            form.HideSubmit();


                            return form;
                        });
                        break;
                }
                subEntity.Where.And().Equal(new ProjectMember { user_id = user.Id });

                if (subEntity.Count() == 0)
                {

                    subEntity.Insert(new ProjectMember
                    {
                        CreationTime = DateTime.Now,
                        AuthType = Web.WebAuthType.Guest,
                        project_id = Team,
                        user_id = user.Id,
                        Alias = user.Alias
                    });

                    Utility.CMS.ObjectEntity<ProjectDynamic>()
                               .Insert(new ProjectDynamic
                               {
                                   Time = Utility.TimeSpan(DateTime.Now),
                                   user_id = user.Id,
                                   Explain = String.Format("{0} 加入了项目", user.Alias),
                                   project_id = Team,
                                   refer_id = user.Id,
                                   Title = "项目成员",
                                   Type = DynamicType.Member
                               });
                }
                else
                {
                    this.AsyncDialog("Confirm", d => new UIConfirmDialog("你需要退出此项目组吗"));
                    subEntity.Delete();
                }
                UMC.Configuration.ConfigurationManager.ClearCache(Team, "Data");

                this.Context.Send("Subject.Member", true);
            }
            else
            {
                if (project != null && project.user_id == user.Id)
                {

                }
                else
                {
                    var team = Utility.CMS.ObjectEntity<ProjectMember>().Where.And().Equal(new ProjectMember
                    {
                        project_id = project.Id,
                        user_id = user.Id
                    }).Entities.Single();
                    if (team != null)
                    {
                        switch (team.AuthType)
                        {
                            case WebAuthType.Admin:
                                break;
                            default:
                                this.Prompt("只有管理员，才能管理团队成员");
                                break;
                        }

                    }
                    else
                    {
                        this.Prompt("只有管理员，才能管理团队成员");
                    }
                }

                var member = subEntity.Where.And().Equal(new ProjectMember { user_id = Id }).Entities.Single();
                var arg = request.Arguments;
                var Type = this.AsyncDialog("Type", g =>
                {
                    var optls = new Web.UISheetDialog();
                    optls.Title = "成员操作"; ;
                    optls.Options.Add(new UIClick(new WebMeta(arg).Put(g, "Del"))
                    {
                        Command = request.Command,
                        Model = request.Model,
                        Text = "移除此成员"
                    });
                    optls.Options.Add(new UIClick(new WebMeta(arg).Put(g, "Admin"))
                    {
                        Command = request.Command,
                        Model = request.Model,
                        Text = "设置为管理员"
                    });
                    optls.Options.Add(new UIClick(new WebMeta(arg).Put(g, "Write"))
                    {
                        Command = request.Command,
                        Model = request.Model,
                        Text = "设置专栏作家"
                    });
                    optls.Options.Add(new UIClick(new WebMeta(arg).Put(g, "Read"))
                    {
                        Command = request.Command,
                        Model = request.Model,
                        Text = "设置读者"
                    });




                    return optls;
                });

                switch (Type)
                {
                    case "Del":
                        subEntity.Delete();

                        Utility.CMS.ObjectEntity<ProjectDynamic>()
                                   .Insert(new ProjectDynamic
                                   {
                                       Time = Utility.TimeSpan(),//DateTime.Now,
                                       user_id = user.Id,
                                       Explain = "移除了项目成员",
                                       project_id = Team,
                                       refer_id = member.user_id,
                                       Title = "项目成员",
                                       Type = DynamicType.Member
                                   });
                        break;
                    case "Admin":
                        subEntity.Update(new ProjectMember { AuthType = WebAuthType.Admin });
                        Utility.CMS.ObjectEntity<ProjectDynamic>()
                                   .Insert(new ProjectDynamic
                                   {
                                       Time = Utility.TimeSpan(),//DateTime.Now,
                                       user_id = user.Id,
                                       Explain = "设置成员为管理员",
                                       project_id = Team,
                                       refer_id = member.user_id,
                                       Title = "项目成员",
                                       Type = DynamicType.Member
                                   });
                        break;
                    case "Write":
                        subEntity.Update(new ProjectMember { AuthType = WebAuthType.User });
                        Utility.CMS.ObjectEntity<ProjectDynamic>()
                                   .Insert(new ProjectDynamic
                                   {
                                       Time = Utility.TimeSpan(DateTime.Now),
                                       user_id = user.Id,
                                       Explain = "设置成员为专栏作家",
                                       project_id = Team,
                                       refer_id = member.user_id,
                                       Title = "项目成员",
                                       Type = DynamicType.Member
                                   });
                        break;
                    case "Read":
                        subEntity.Update(new ProjectMember { AuthType = WebAuthType.Guest });
                        Utility.CMS.ObjectEntity<ProjectDynamic>()
                                   .Insert(new ProjectDynamic
                                   {
                                       Time = Utility.TimeSpan(DateTime.Now),
                                       user_id = user.Id,
                                       Explain = "设置成员为读者",
                                       project_id = Team,
                                       refer_id = member.user_id,
                                       Title = "项目成员",
                                       Type = DynamicType.Member
                                   });
                        break;
                }
                UMC.Configuration.ConfigurationManager.ClearCache(Team, "Data");
                this.Context.Send("Subject.Team", true);


            }



        }

    }
}