using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Collections;
using System.Reflection;
using UMC.Data.Entities;
using UMC.Web.UI;
using UMC.Web;

namespace UMC.Subs.Activities
{
    class SubjectCommentsActivity : WebActivity
    {
        public override void ProcessActivity(WebRequest request, WebResponse response)
        {

            if (String.IsNullOrEmpty(request.SendValue) == false)
            {

                this.Context.Send(new UISectionBuilder(request.Model, request.Command, new UMC.Web.WebMeta().Put("Id", request.SendValue))

                        .Builder(), true);
            }
            var user = UMC.Security.Identity.Current;
            var refer_id = UMC.Data.Utility.Guid(this.AsyncDialog("Id", g => this.DialogValue(user.Id.ToString()))) ?? user.Id;
            var type = this.AsyncDialog("type", g => this.DialogValue("Desc"));
            var form = request.SendValues ?? new UMC.Web.WebMeta();

            var IsDel = false;
            int limit = UMC.Data.Utility.IntParse(form["limit"] as string, type == "Asc" ? 5 : 25);

            int start = UMC.Data.Utility.IntParse(form["start"] as string, 0);

            var entity = Utility.CMS.ObjectEntity<Comment>();
            entity.Where.And().Equal(new Comment { for_id = Guid.Empty });

            if (user.Id == refer_id)
            {
                entity.Where.And().Equal(new Comment { user_id = refer_id });
            }
            else
            {
                var subject = Utility.CMS.ObjectEntity<Subject>().Where.And().Equal(new Subject
                {
                    Id = refer_id
                }).Entities.Single();
                if (subject != null && subject.project_id.HasValue)
                {
                    var project = Utility.CMS.ObjectEntity<Project>().Where.And().Equal(new Project
                    {
                        Id = subject.project_id
                    }).Entities.Single();
                    if (project != null)
                    {
                        if (project.user_id == user.Id)
                        {

                            IsDel = true;
                        }
                        else
                        {
                            var member = Utility.CMS.ObjectEntity<ProjectMember>().Where.And().Equal(new ProjectMember
                            {
                                project_id = project.Id,
                                user_id = user.Id
                            }).Entities.Single();
                            if (member != null)
                            {
                                switch (member.AuthType)
                                {
                                    case WebAuthType.Admin:
                                    case WebAuthType.User:
                                        IsDel = true;
                                        break;
                                }
                            }
                        }
                    }
                }
                entity.Where.And().Equal(new Comment { ref_id = refer_id });
                entity.Where.And().GreaterEqual(new Comment { Visible = 0 });
            }

            if (type == "Asc")
            {
                entity.Order.Asc(new Comment { CommentDate = DateTime.Now });
            }
            else
            {
                entity.Order.Desc(new Comment { CommentDate = DateTime.Now });
            }
            entity.Where.And().Greater(new Comment { Visible = -1 });
            var count = entity.Count();
            var ui = UISection.Create();
            ui.Title = new UITitle("评论");
            var cells = Utility.Comments(entity, start, limit, request.Model);
            ui.Key = "Comments";
            if (count == 0)
            {
                ui.IsNext = false;
                ui.Add("Desc", new UMC.Web.WebMeta().Put("desc", "暂无评论").Put("icon", "\uF0E6"), new UMC.Web.WebMeta().Put("desc", "{icon}\n{desc}"),

                    new UIStyle().Align(1).Color(0xaaa).Padding(20, 20).BgColor(0xfff).Size(12).Name("icon", new UIStyle().Font("wdk").Size(60)));
            }
            else
            {

                if (request.IsCashier || IsDel)
                {
                    foreach (var cell in cells)
                    {

                        ui.Delete(cell, new UIEventText().Click(new UIClick(cell.Id) { Model = request.Model, Command = "Delete" }));
                    }
                }
                else
                {

                    ui.AddCells(cells.ToArray());
                }
                ui.Total = count;
                ui.IsNext = ui.Total > limit + start;
            }
            response.Redirect(ui);

        }



    }
}