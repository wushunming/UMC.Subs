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
    class SubjectMenuActivity : WebActivity
    {
        Project Dashboard(UMC.Security.Identity user)
        {
            var projts = Utility.CMS.ObjectEntity<UMC.Data.Entities.Project>()
                             .Where.And().Equal(new Project { user_id = user.Id }).Entities.Query(0, 5);
            if (projts.Length == 0)
            {
                var team = new Project();
                team.ModifiedTime = DateTime.Now;


                team.Id = user.Id;
                team.user_id = user.Id;
                team.Code = Utility.Parse36Encode(team.Id.Value.GetHashCode());
                team.CreationTime = DateTime.Now;
                team.Caption = user.Alias;
                team.Sequence = 0;

                var strt = UMC.Security.AccessToken.Current.Data["DingTalk-Setting"] as string;//, Utility.Guid(projectId)).Commit();
                if (String.IsNullOrEmpty(strt) == false)
                {
                    var userSetting = Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectUserSetting>()
                        .Where.And().Equal(new ProjectUserSetting { Id = Utility.Guid(strt, true) }).Entities.Single();

                    if (userSetting != null)
                    {
                        var setting2 = new ProjectSetting() { user_setting_id = userSetting.Id, project_id = team.Id, Type = 11 }; Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectSetting>().Insert(setting2);
                    }
                }

                Utility.CMS.ObjectEntity<UMC.Data.Entities.Project>().Insert(team);
                Data.WebResource.Instance().Transfer(new Uri("https://oss.365lu.cn/UserResources/app/zhishi-icon.jpg"), team.Id.Value, 1);
                var p = new ProjectItem()
                {
                    Id = Guid.NewGuid(),
                    Caption = "首页",
                    Code = "Home",
                    CreationTime = DateTime.Now,
                    project_id = team.Id,
                    Sequence = 0,
                    user_id = user.Id,
                };
                Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectItem>()
                                        .Insert(p);

                var portfolio = new Portfolio()
                {
                    Id = Guid.NewGuid(),
                    Caption = "随笔",
                    Count = 0,
                    CreationTime = DateTime.Now,
                    Sequence = 0,
                    user_id = user.Id,
                    project_id = team.Id,
                    project_item_id = p.Id,
                };
                Utility.CMS.ObjectEntity<UMC.Data.Entities.Portfolio>()
                                        .Insert(portfolio);
                return team;
            }



            return new List<Project>(projts).Find(d => d.Id == user.Id) ?? projts[0];


        }
        public override void ProcessActivity(WebRequest request, WebResponse response)
        {

            var user = UMC.Security.Identity.Current;


            var code = this.AsyncDialog("code", g =>
            {
                return this.DialogValue("none");
            });
            var redData = new WebMeta();
            var paths = new List<String>();
            paths.AddRange(code.Split('/'));
            Project team = null;
            switch (code)
            { 
                case "download":
                case "explore":
                    response.Redirect(redData.Put("type", code));
                    break;
                case "none":
                    response.Redirect(redData.Put("type", "index"));
                    break;
                case "dashboard":
                    if (user.IsAuthenticated)
                    {
                        var type = this.AsyncDialog("type", "project");
                        if (type != "project")
                        {
                            response.Redirect(redData.Put("type", "self"));
                        }
                        paths.Clear();
                        team = Dashboard(user);

                        paths.Add(team.Code);


                        break;
                    }
                    else
                    {
                        if (request.UserAgent.IndexOf("DingTalk") > 0)
                        {
                            if (request.UrlReferrer != null && String.IsNullOrEmpty(request.UrlReferrer.Query) == false)
                            {
                                var Key = request.UrlReferrer.Query.Substring(1);
                                if (String.IsNullOrEmpty(Key) == false)
                                {
                                    var project = Utility.CMS.ObjectEntity<UMC.Data.Entities.Project>()
                                          .Where.And().Equal(new Project { Code = Key }).Entities.Single();
                                    if (project != null)
                                    {
                                        var projectSetting = Utility.CMS.ObjectEntity<ProjectSetting>()
                                               .Where.And().Equal(new ProjectSetting
                                               {
                                                   project_id = project.Id,
                                                   Type = 11
                                               }).Entities.Single();

                                        if (projectSetting != null)
                                        {
                                            var uSetting = Utility.CMS.ObjectEntity<ProjectUserSetting>()
                                                   .Where.And().Equal(new ProjectUserSetting
                                                   {
                                                       Id = projectSetting.user_setting_id,
                                                       Type = 11
                                                   }).Entities.Single();
                                            if (uSetting != null)
                                            {

                                                redData.Put("id", project.Id);
                                                redData.Put("DingTalk", uSetting.CorpId);
                                            }

                                        }

                                    }

                                }

                            }
                        }
                        response.Redirect(redData.Put("type", "login"));
                    }
                    break;

            }


            if (String.Equals(code, "none"))
            {
                if (user.IsAuthenticated)
                {
                    response.Redirect(redData.Put("type", "index"));
                }
                else
                {
                    response.Redirect(redData.Put("type", "login"));
                }

            }
            if (team == null)
            {
                var scheduleEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Project>();
                scheduleEntity.Order.Asc(new Project { CreationTime = DateTime.MinValue });
                scheduleEntity.Where.And().Equal(new Project { Code = paths[0] });

                team = scheduleEntity.Single();
            }

            if (team == null)
            {
                response.Redirect(redData.Put("type", "index"));

            }


            var projects = new List<ProjectItem>();



            var projectEntity = Utility.CMS.ObjectEntity<ProjectItem>();
            projectEntity.Where.And().In(new ProjectItem { project_id = team.Id });

            projectEntity.Order.Asc(new ProjectItem { Sequence = 0 });

            projectEntity.Query(dr => projects.Add(dr));

            var clsName = "EditerAll";
            var editer = team.user_id == user.Id;
            int status = 1;
            if (editer == false)
            {
                clsName = "View";
                status = -1;
                var member = Utility.CMS.ObjectEntity<ProjectMember>().Where.And().Equal(new ProjectMember
                {
                    project_id = team.Id,
                    user_id = user.Id
                }).Entities.Single();
                if (member != null)
                {
                    switch (member.AuthType)
                    {
                        case WebAuthType.Admin:
                            clsName = "EditerItem";
                            status = 1;
                            break;
                        case WebAuthType.All:
                            status = -1;
                            break;
                        case WebAuthType.Guest:
                            status = -1;
                            break;
                        case WebAuthType.User:
                            clsName = "EditerDoc";
                            status = 1;
                            break;
                    }
                }


            }
            var data = new List<WebMeta>();
            int pindex = 0;
            ProjectItem projectItem = null;
            foreach (var p in projects)
            {
                var meta = new WebMeta();
                meta.Put("id", p.Id).Put("text", p.Caption).Put("path", String.Format("{0}/{1}", team.Code, p.Code));
                switch (clsName)
                {
                    case "EditerItem":
                    case "EditerAll":
                        break;
                    default:
                        if (p.Hide == true)
                        {
                            meta.Put("hide", true);
                        }
                        break;
                }
                if (paths.Count > 1 && String.Equals(paths[1], p.Code, StringComparison.CurrentCultureIgnoreCase))
                {
                    projectItem = p;//.Id.Value;
                    pindex = data.Count;
                }
                data.Add(meta);
            }
            var webr = Data.WebResource.Instance();

            var menu = redData.Put("menu", data).Put("selectIndex", pindex).Put("text", team.Caption).Put("code", team.Code).Put("id", team.Id).Put("src", webr.ImageResolve(team.Id.Value, "1", 4));

            menu["Auth"] = clsName;
            if (user.IsAuthenticated)
                menu["Authed"] = "OK";

            if (projects.Count > 0)
            {
                var pitem = projectItem == null ? projects[pindex] : projectItem;
                var subs = SubjectPortfolioSubActivity.Portfolio(team, pitem, status);
                menu.Put("subs", subs).Put("nav", String.Format("{0}/{1}", team.Code, pitem.Code));

            }
            if (paths.Count == 3 && projectItem != null)
            {
                var sub = Utility.CMS.ObjectEntity<Subject>().Where.And().Equal(new Subject
                {
                    project_id = team.Id,
                    project_item_id = projectItem.Id,
                    Code = paths[2]
                }).Entities.Single();
                if (sub != null)
                    menu.Put("spa", new WebMeta().Put("id", sub.Id).Put("path", String.Format("{0}/{1}/{2}", team.Code, projectItem.Code, sub.Code)));

            }
            if (request.UserAgent.IndexOf("DingTalk") > 0)
            {
                if (user.IsAuthenticated == false)
                {

                    var projectSetting = Utility.CMS.ObjectEntity<ProjectSetting>()
                           .Where.And().Equal(new ProjectSetting
                           {
                               project_id = team.Id,
                               Type = 11
                           }).Entities.Single();
                    if (projectSetting != null)
                    {
                        var uSetting = Utility.CMS.ObjectEntity<ProjectUserSetting>()
                                                      .Where.And().Equal(new ProjectUserSetting
                                                      {
                                                          Id = projectSetting.user_setting_id,
                                                          Type = 11
                                                      }).Entities.Single();
                        if (uSetting != null)
                        {

                            redData.Put("DingTalk", uSetting.CorpId);
                        }

                    }
                }
                else if (paths.Count == 2 && paths[1] == "follow")
                {
                    menu.Put("follow", true);
                }


            }
            else if (paths.Count == 2 && paths[1] == "follow")
            {
                menu.Put("follow", true);

            }
            response.Redirect(menu);


        }

    }
}