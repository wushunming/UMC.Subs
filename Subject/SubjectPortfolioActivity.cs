using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Collections;
using System.Reflection;
using UMC.Data.Entities;
using UMC.Web;
using UMC.Web.UI;

namespace UMC.Subs.Activities
{
    class SubjectPortfolioActivity : WebActivity
    {
        public override void ProcessActivity(WebRequest request, WebResponse response)
        {
            var key = this.AsyncDialog("Key", g => this.DialogValue("EDITER"));
            var user = Security.Identity.Current;
            var sid = Web.UIDialog.AsyncDialog("Id", d =>
            {
                var ProjectId = Utility.Guid(this.AsyncDialog("Project", g =>
                {
                    if (request.SendValues == null || request.SendValues.ContainsKey("start") == false)
                    {
                        var buider = new UISectionBuilder(request.Model, request.Command, request.Arguments);
                        buider.CloseEvent("UI.Event").RefreshEvent("Subject.Project");
                        this.Context.Send(buider.Builder(), true);
                    }
                    UITitle title = UITitle.Create();

                    title.Title = "我的专栏项目";

                    var ui = UISection.Create(title);
                    var ids = new List<Guid>();
                    var pros = new List<Project>();
                    Utility.CMS.ObjectEntity<Project>().Where.And().Equal(new Data.Entities.Project { user_id = user.Id })
                        .Or().In("Id", Utility.CMS.ObjectEntity<ProjectMember>().Where.And().Equal(new ProjectMember { user_id = user.Id })
                        .And().In(new ProjectMember { AuthType = WebAuthType.Admin }, WebAuthType.User).Entities.Script(new ProjectMember { project_id = Guid.Empty })).Entities.Order.Asc(new Project { Sequence = 0 }).Entities.Query(dr =>
                        {
                            pros.Add(dr); ids.Add(dr.Id.Value);
                        });

                    if (ids.Count > 0)
                    {
                        var webr = UMC.Data.WebResource.Instance();
                        var subs = new List<Subject>();
                        Utility.CMS.ObjectEntity<UMC.Data.Entities.Subject>()
                                   .Where.And().In(new Subject { project_id = ids[0] }, ids.ToArray())
                                   .Entities.GroupBy(new Subject { project_id = Guid.Empty }).Count(new Subject { Seq = 0 }).Query(dr => subs.Add(dr));

                        foreach (var p in pros)
                        {
                            var sub = subs.Find(s => s.project_id == p.Id);
                            var desc = new UIIconNameDesc(new UIIconNameDesc.Item(webr.ResolveUrl(p.Id.Value, "1", "4"), p.Caption,
                                String.Format("知识{0}篇", sub == null ? 0 : sub.Seq))
                                .Click(Web.UIClick.Query(new WebMeta().Put("Project", p.Id))));

                            //if(desc.Button())
                            if (p.user_id == user.Id)
                            {
                                desc.Button("我的", null, 0x25b864);
                            }
                            ui.Add(desc);

                        }
                    }
                    else
                    {
                        ui.Add("Desc", new UMC.Web.WebMeta().Put("desc", "你未有专栏的项目").Put("icon", "\uF016"), new UMC.Web.WebMeta().Put("desc", "{icon}\n{desc}"),

                            new UIStyle().Align(1).Color(0xaaa).Padding(20, 20).BgColor(0xfff).Size(12).Name("icon", new UIStyle().Font("wdk").Size(60)));

                        ui.NewSection().AddCell('\uf19d', "创立我的项目"
                            , "", new UIClick("News").Send(request.Model, "ProjectUI"));
                    }

                    response.Redirect(ui);

                    return this.DialogValue("Project");
                })) ?? Guid.Empty;
                if (ProjectId == Guid.Empty)
                {
                    this.Prompt("未传入项目");
                }
                var projectItemId = Utility.Guid(this.AsyncDialog("Item", g => this.DialogValue("Item"))) ?? Guid.Empty;

                if (request.SendValues == null || request.SendValues.ContainsKey("start") == false)
                {
                    var buider = new UISectionBuilder(request.Model, request.Command, request.Arguments);
                    buider.CloseEvent("UI.Event");
                    this.Context.Send(buider.Builder(), true);
                }

                UITitle uITItle = UITitle.Create();
                if (projectItemId == Guid.Empty)
                {
                    uITItle.Title = "选择栏位";
                }
                else
                {

                    uITItle.Title = "选择目录";
                }
                var sestion = UISection.Create(uITItle);
                if (projectItemId == Guid.Empty)
                {
                    var team = Utility.CMS.ObjectEntity<Project>().Where.And().Equal(new Data.Entities.Project { Id = ProjectId }).Entities.Single();
                    sestion.AddCell("所属项目", team.Caption);
                    var ui2 = sestion.NewSection();
                    Utility.CMS.ObjectEntity<ProjectItem>().Where.And().Equal(new Data.Entities.ProjectItem { project_id = ProjectId }).Entities.Order.Asc(new ProjectItem { Sequence = 0 }).Entities.Query(dr =>
                    {

                        ui2.AddCell(dr.Caption, Web.UIClick.Query(new WebMeta().Put("Item", dr.Id)));

                    });
                }
                else
                {
                    var team = Utility.CMS.ObjectEntity<Project>().Where.And().Equal(new Data.Entities.Project
                    {
                        Id = ProjectId
                    }).Entities.Single();
                    sestion.AddCell("所属项目", team.Caption);

                    var project = Utility.CMS.ObjectEntity<ProjectItem>().Where.And().Equal(new Data.Entities.ProjectItem
                    {
                        Id = projectItemId
                    }).Entities.Single();
                    sestion.AddCell("所属栏位", project.Caption);
                    var ui2 = sestion.NewSection();

                    Utility.CMS.ObjectEntity<Portfolio>().Where.And().Equal(new Data.Entities.Portfolio { project_item_id = project.Id })
                    .Entities.Order.Asc(new Portfolio { Sequence = 0 }).Entities.Query(dr =>
                    {
                        ui2.AddCell(dr.Caption, new Web.UIClick(new WebMeta(request.Arguments).Put(d, dr.Id)).Send(request.Model, request.Command));
                    });
                }
                response.Redirect(sestion);
                return this.DialogValue("none");
            });
            var cmdId = UMC.Data.Utility.Guid(sid) ?? Guid.Empty;

            var category = new Portfolio();

            var objectEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Portfolio>();
            category = objectEntity.Where.And().Equal(new Portfolio
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

            var Caption = Web.UIDialog.AsyncDialog("Caption", d =>
            {

                var fmdg = new Web.UIFormDialog();
                fmdg.Title = category.Id.HasValue ? "编辑" : "新建";
                fmdg.AddText("文集名称", "Caption", category.Caption);
                fmdg.Submit("确认提交", request, "Subject.Portfolio", "Subject.Portfolio.Add");
                return fmdg;

            });
            category.Caption = Caption;

            if (category.Id.HasValue == false)
            {
                var ItemId = Utility.Guid(this.AsyncDialog("ItemId", g => this.DialogValue("Item"))).Value;

                var project = Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectItem>().Where.And().Equal(new ProjectItem
                {
                    Id = ItemId
                }).Entities.Single();
                category.Id = Guid.NewGuid();
                category.project_id = project.project_id;
                category.project_item_id = project.Id;

                category.user_id = user.Id;
                category.CreationTime = DateTime.Now;
                category.Count = 0;
                category.Sequence = Utility.TimeSpan();


                objectEntity.Insert(category);

                this.Context.Send(new UMC.Web.WebMeta().Put("type", "Subject.Portfolio.Add").Put("Id", category.Id).Put("Text", category.Caption), true);
            }
            else
            {
                objectEntity.Update(new Portfolio { Caption = Caption });
                this.Context.Send(new UMC.Web.WebMeta().Put("type", "Subject.Portfolio").Put("Id", category.Id), true);
            }


        }

    }
}