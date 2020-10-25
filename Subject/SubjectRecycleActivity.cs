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
    class SubjectRecycleActivity : WebActivity
    {

        public override void ProcessActivity(WebRequest request, WebResponse response)
        {
            var subEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Subject>();
            var user = UMC.Security.Identity.Current;
            var Id = Utility.Guid(this.AsyncDialog("Id", g =>
            {
                var webr = UMC.Data.WebResource.Instance();
                var form = request.SendValues ?? new UMC.Web.WebMeta();

                int limit = UMC.Data.Utility.IntParse(form["limit"] as string, 25);
                int start = UMC.Data.Utility.IntParse(form["start"] as string, 0);

                string sort = form[("sort")] as string;
                string dir = form[("dir")] as string;


                Guid? Project = UMC.Data.Utility.Guid(form["Project"]);

                var Keyword = (form["Keyword"] as string ?? String.Empty);

                if (String.IsNullOrEmpty(Keyword) == false)
                {
                    subEntity.Where.And().Like(new Subject { Title = Keyword });
                }
                subEntity.Where.And().Equal(new Subject
                {
                    project_id = Project,
                    Visible = -1
                }).And().Contains().Or().Equal(new Subject { user_id = user.Id, last_user_id = user.Id });
                if (!String.IsNullOrEmpty(sort) && sort.StartsWith("_") == false)
                {
                    if (dir == "DESC")
                    {
                        subEntity.Order.Desc(sort);
                    }
                    else
                    {
                        subEntity.Order.Asc(sort);
                    }
                }
                else
                {
                    subEntity.Order.Desc(new Subject { LastDate = DateTime.Now });
                }
                var subs = new List<Subject>();
                var cateids = new List<Guid>();
                var search = UMC.Data.Reflection.CreateInstance<Subject>();
                search.DataJSON = null;
                search.Content = null;
                search.ConfigXml = null;
                subEntity.Query(search, start, limit, dr =>
                 {
                     subs.Add(dr);
                     cateids.Add(dr.user_id ?? Guid.Empty);
                     cateids.Add(dr.last_user_id ?? Guid.Empty);

                 });
                var cates = new List<User>();
                if (cateids.Count > 0)
                {
                    Utility.CMS.ObjectEntity<User>().Where.And().In(new User { Id = cateids[0] }, cateids.ToArray())
                            .Entities.Query(dr => cates.Add(dr));
                }
                var data = new System.Data.DataTable();
                data.Columns.Add("id");
                data.Columns.Add("title");
                data.Columns.Add("desc");
                data.Columns.Add("time");
                data.Columns.Add("reply");
                data.Columns.Add("look");
                data.Columns.Add("favs");
                data.Columns.Add("lastid");
                data.Columns.Add("lastTime");
                data.Columns.Add("lastName");
                data.Columns.Add("lastSrc");
                data.Columns.Add("postid");
                data.Columns.Add("poster");
                data.Columns.Add("postsrc");
                data.Columns.Add("project");
                data.Columns.Add("projectItem");
                foreach (var sub in subs)
                {
                    var user1 = cates.Find(d => d.Id == sub.user_id) ?? new User();

                    var user2 = cates.Find(d => d.Id == sub.last_user_id) ?? new User();
                    data.Rows.Add(sub.Id, sub.Title, sub.Description, sub.ReleaseDate.HasValue ? Utility.GetDate(sub.ReleaseDate) : "未发布"
                        , sub.Reply ?? 0, sub.Look ?? 0, sub.Favs ?? 0, sub.last_user_id, sub.LastDate, user2.Alias, webr.ResolveUrl(sub.last_user_id ?? Guid.Empty, "1", 4), sub.user_id, user1.Alias, webr.ResolveUrl(sub.user_id ?? Guid.Empty, "1", 4),
                        sub.project_id, sub.project_item_id);
                }
                var hashc = new System.Collections.Hashtable();
                hashc["data"] = data;
                var total = subEntity.Count(); ;
                hashc["total"] = total;// subEntity.Count();
                if (total == 0)
                {
                    hashc["msg"] = "回收站是空的";
                }
                response.Redirect(hashc);
                return this.DialogValue("none");
            })).Value;
            subEntity.Where.And().Equal(new Subject { Id = Id, Visible = -1 });
            var subt = subEntity.Single(new Subject { project_id = Guid.Empty, Id = Guid.Empty, Title = String.Empty, user_id = Guid.Empty });
            var project = Utility.CMS.ObjectEntity<Project>().Where.And().Equal(new Project
            {
                Id = subt.project_id.Value
            }).Entities.Single();
            if (project != null && project.user_id == user.Id)
            {

            }
            else
            {
                var member = Utility.CMS.ObjectEntity<ProjectMember>().Where.And().Equal(new ProjectMember
                {
                    project_id = subt.project_id.Value,
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
                            this.Prompt("您未有管理回收站的权限");
                            break;
                    }

                }
                else
                {
                    this.Prompt("您未有管理回收站的权限");
                }
            }

            var sid = UMC.Data.Utility.Guid(this.AsyncDialog("PortfolioId", request.Model, "Portfolio", new Web.WebMeta().Put("Project", subt.project_id))).Value;

            var Portfolio = Utility.CMS.ObjectEntity<UMC.Data.Entities.Portfolio>()
                .Where.And().Equal(new Portfolio { Id = sid }).Entities.Single();
            var projectItem = Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectItem>()
                 .Where.And().Equal(new ProjectItem { Id = Portfolio.project_item_id }).Entities.Single();

            var seq = (Utility.CMS.ObjectEntity<UMC.Data.Entities.Subject>().Where.And().Equal(new Subject { portfolio_id = sid })
                .Entities.Max(new Subject() { Seq = 0 }).Seq ?? 0) + 1;
            subEntity.Update(new Subject()
            {
                Visible = 1,
                LastDate = DateTime.Now,
                portfolio_id = sid,
                Seq = seq,
                project_id = projectItem.project_id,
                last_user_id = user.Id,
                user_id = user.Id,
                Status = -1
            });
            Utility.CMS.ObjectEntity<ProjectDynamic>()
                       .Insert(new ProjectDynamic
                       {
                           Time = Utility.TimeSpan(),
                           user_id = user.Id,
                           Explain = "还原了文档",
                           project_id = projectItem.project_id,
                           refer_id = Id,
                           Title = subt.Title,
                           Type = DynamicType.Subject
                       });

            this.Context.Send(new UMC.Web.WebMeta().Put("type", "Subject.Portfolio.Change").Put("Sub", Id).Put("Id", sid).Put("Item", new WebMeta().Put("id", subt.Id).Put("text", subt.Title)), true);

        }

    }
}