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
    class SubjectProjectItemActivity : WebActivity
    {
        public override void ProcessActivity(WebRequest request, WebResponse response)
        {
            var key = this.AsyncDialog("Key", g => this.DialogValue("EDITER"));
            var user = Security.Identity.Current;
            var sid = Web.UIDialog.AsyncDialog("Id", d =>
            {
                if (request.SendValues == null || request.SendValues.ContainsKey("start") == false)
                {
                    var buider = new UISectionBuilder(request.Model, request.Command, request.Arguments);
                    buider.CloseEvent("UI.Event");
                    buider.RefreshEvent("Subject.ProjectItem");
                    this.Context.Send(buider.Builder(), true);
                }
                UITitle title = UITitle.Create();

                title.Title = "我参与的项目";
                switch (key)
                {
                    case "EDITER":
                        break;
                    default:
                        title.Title = "选择项目";
                        break;
                }

                var ui = UISection.Create(title);

                Utility.CMS.ObjectEntity<Project>().Where.And().Equal(new Data.Entities.Project { user_id = user.Id })
                .And().In("Id", Utility.CMS.ObjectEntity<ProjectMember>().Where.And().Equal(new ProjectMember { user_id = user.Id })
                .And().In(new ProjectMember { AuthType = WebAuthType.Admin }, WebAuthType.User).Entities.Script(new ProjectMember { project_id = Guid.Empty })).Entities.Order.Asc(new Project { Sequence = 0 }).Entities.Query(dr =>
                {

                    ui.AddCell(dr.Caption, new UIClick(new WebMeta(request.Arguments).Put("Id", dr.Id)).Send(request.Model, request.Command));

                });
                if (ui.Length == 0)
                {

                    ui.Add("Desc", new UMC.Web.WebMeta().Put("desc", "你未有编辑权限的项目").Put("icon", "\uF016"), new UMC.Web.WebMeta().Put("desc", "{icon}\n{desc}"),

                        new UIStyle().Align(1).Color(0xaaa).Padding(20, 20).BgColor(0xfff).Size(12).Name("icon", new UIStyle().Font("wdk").Size(60)));


                }

                response.Redirect(ui);

                return this.DialogValue("none");
            });
            var cmdId = UMC.Data.Utility.Guid(sid) ?? Guid.Empty;

            var category = new ProjectItem();

            var objectEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectItem>();
            category = objectEntity.Where.And().Equal(new ProjectItem
            {
                Id = cmdId
            }).Entities.Single() ?? category;
            switch (key)
            {
                case "EDITER":
                    break;
                default:
                    this.Context.Send(new WebMeta().UIEvent(key, new ListItem(category.Caption, category.Id.ToString())), true);
                    break;
            }
            var Project = Utility.Guid(this.AsyncDialog("Project", g => this.DialogValue("Team"))).Value;

            var project = Utility.CMS.ObjectEntity<UMC.Data.Entities.Project>().Where.And().Equal(new Data.Entities.Project { Id = Project })
                .Entities.Single();
            if (project != null && project.user_id == user.Id)
            {

            }
            else
            {
                var member = Utility.CMS.ObjectEntity<ProjectMember>().Where.And().Equal(new ProjectMember
                {
                    project_id = Project,
                    user_id = user.Id
                }).Entities.Single();
                if (member != null)
                {
                    switch (member.AuthType)
                    {
                        case WebAuthType.Admin:
                            break;
                        default:
                            this.Prompt("您未有编辑栏位权限");
                            break;
                    }

                }
                else
                {
                    this.Prompt("您未有编辑栏位权限");
                }
            }

            var Caption = this.AsyncDialog("Caption", d =>
            {

                var fmdg = new Web.UIFormDialog();
                fmdg.Title = category.Id.HasValue ? "编辑栏位" : "新建栏位";
                fmdg.AddText("栏位名称", "Caption", category.Caption);
                if (category.Id.HasValue)
                {
                    fmdg.AddText("栏位简码", "Code", category.Code);
                    fmdg.AddCheckBox("", "Status", "NO").Put("隐藏", "Hide", category.Hide == true).Put("删除", "DEL");
                }
                fmdg.Submit("确认", request, "Subject.ProjectItem", "Subject.ProjectItem.Del");
                return fmdg;

            });
            var team = new ProjectItem();
            UMC.Data.Reflection.SetProperty(team, Caption.GetDictionary());

            if (category.Id.HasValue == false)
            {
                team.Id = Guid.NewGuid();
                team.project_id = Project;
                team.Code = Utility.Parse36Encode(team.Id.Value.GetHashCode());
                team.user_id = user.Id;
                team.CreationTime = DateTime.Now;
                team.Sequence = Utility.TimeSpan();
                team.Hide = false;

                objectEntity.Insert(team);

                var portfolio = new Portfolio()
                {
                    Id = Guid.NewGuid(),
                    Caption = "随笔",
                    Count = 0,
                    CreationTime = DateTime.Now,
                    Sequence = 0,
                    user_id = user.Id,
                    project_item_id = team.Id,
                    project_id = Project,
                };
                Utility.CMS.ObjectEntity<UMC.Data.Entities.Portfolio>()
                    .Insert(portfolio);

                Utility.CMS.ObjectEntity<ProjectDynamic>()
                          .Insert(new ProjectDynamic
                          {
                              Time = Utility.TimeSpan(),
                              user_id = user.Id,
                              Explain = "创建了栏位",
                              project_id = portfolio.project_id,
                              refer_id = portfolio.Id,
                              Title = portfolio.Caption,
                              Type = DynamicType.ProjectItem
                          });
                this.Context.Send(new UMC.Web.WebMeta().Put("type", "Subject.ProjectItem").Put("id", team.Id).Put("text", team.Caption)
                    .Put("path", String.Format("{0}/{1}", project.Code, team.Code))
                    , true);

            }
            else
            {
                var status = Caption["Status"] ?? "";
                if (status.Contains("DEL"))
                {
                    if (Utility.CMS.ObjectEntity<ProjectItem>().Where.And().Equal(new ProjectItem
                    {
                        project_id = category.project_id.Value
                    }).Entities.Count() == 1)
                    {
                        this.Prompt("最少需要一个栏位");
                    }
                    Utility.CMS.ObjectEntity<Portfolio>().Where.And().Equal(new Portfolio
                    {
                        project_item_id = category.Id
                    }).Entities.Delete();
                    Utility.CMS.ObjectEntity<Subject>().Where.And().Equal(new Subject
                    {
                        project_item_id = category.Id
                    }).Entities.Update(new Subject { last_user_id = user.Id, LastDate = DateTime.Now, Visible = -1 });
                    objectEntity.Delete();

                    Utility.CMS.ObjectEntity<ProjectDynamic>()
                              .Insert(new ProjectDynamic
                              {
                                  Time = Utility.TimeSpan(),//DateTime.Now,
                                  user_id = user.Id,
                                  Explain = "删除了栏位",
                                  project_id = category.project_id,
                                  refer_id = category.Id,
                                  Title = category.Caption,
                                  Type = DynamicType.ProjectItem
                              });

                    this.Context.Send(new UMC.Web.WebMeta().Put("type", "Subject.ProjectItem.Del").Put("id", category.Id).Put("text", team.Caption), true);
                }
                else
                {
                    if (String.Equals(team.Code, category.Code, StringComparison.CurrentCulture) == false)
                    {
                        if (Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectItem>()
                               .Where.And().Equal(new ProjectItem
                               {
                                   Code = team.Code,
                                   project_id = category.project_id
                               }).Entities.Count() > 0)
                        {
                            this.Prompt("存在相同的简码");
                        }

                    }
                    team.Hide = status.Contains("Hide");
                    objectEntity.Update(team);
                    Utility.CMS.ObjectEntity<ProjectDynamic>()
                            .Insert(new ProjectDynamic
                            {
                                Time = Utility.TimeSpan(),
                                user_id = user.Id,
                                Explain = "修改了栏位",
                                project_id = category.project_id,
                                refer_id = category.Id,
                                Title = category.Caption,
                                Type = DynamicType.ProjectItem
                            });
                    this.Context.Send(new UMC.Web.WebMeta().Put("type", "Subject.ProjectItem").Put("id", category.Id).Put("text", team.Caption), true);
                }
            }
            this.Prompt("修改成功");


        }


    }
}