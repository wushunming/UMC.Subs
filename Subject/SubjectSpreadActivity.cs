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
    class SubjectSpreadActivity : WebActivity
    {
        public void Subject(WebRequest request, WebResponse response, String type)
        {
            var subEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Subject>();
            subEntity.Where.And().Equal(new Subject { Status = 1, IsDraught = false })
                .And().GreaterEqual(new Subject { Visible = 0 });
            var sev = request.SendValues ?? new UMC.Web.WebMeta();

            if (sev.ContainsKey("Id"))
            {
                var Category = sev["Id"];
                var cid = UMC.Data.Utility.Guid(Category);
                if (cid.HasValue)
                {
                    var sub = Utility.CMS.ObjectEntity<UMC.Data.Entities.Subject>().Where.And().Equal(new Subject { Id = cid })
                            .Entities.Single();
                    if (sub != null && sub.project_id.HasValue)
                    {

                        subEntity.Where.And().Equal(new Subject { project_id = sub.project_id })
                            .And().Unequal(new Subject { Id = sub.Id });
                    }

                }

            }
            if (String.Equals(type, "View"))
            {

                subEntity.Order.Desc(new Subject { Look = 0 });
            }
            else
            {
                subEntity.Order.Desc(new Subject { Reply = 0 });

            }

            var data = new System.Data.DataTable();
            data.Columns.Add("id");
            data.Columns.Add("title");
            data.Columns.Add("desc");
            data.Columns.Add("src");
            data.Columns.Add("reply");
            data.Columns.Add("look");
            data.Columns.Add("last");

            var webr = UMC.Data.WebResource.Instance();
            subEntity.Query(0, 6, (Subject dr) =>
            {
                data.Rows.Add(dr.Id, dr.Title, dr.Description, webr.ResolveUrl(dr.Id.Value, 1, "0") + "!cms3", dr.Reply, dr.Look, UMC.Data.Utility.GetDate(dr.LastDate));
            });
            response.Redirect(data);
        }
        public override void ProcessActivity(WebRequest request, WebResponse response)
        {
            var type = this.AsyncDialog("Type", "View");
            switch (type)
            {
                case "View":
                case "Reply":
                    this.Subject(request, response, type);
                    break;
                case "Favs":
                    {
                        var category = Utility.Guid(this.AsyncDialog("Project", "Favs"));



                        var cache = UMC.Configuration.ConfigurationManager.DataCache(category ?? Utility.Guid("Favs", true).Value, "Favs", 1000, (k, v, c) =>
                           {


                               var id2s = new List<String>();

                               var pentity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Proposal>()
                                           .Where.And().Equal(new Proposal { Type = 1 })
                                           .And().GreaterEqual(new Proposal
                                           {
                                               CreationDate = DateTime.Now.AddDays(-1)
                                           }).Entities;
                               if (category.HasValue)
                               {
                                   pentity.Where.And().In("ref_id", Utility.CMS.ObjectEntity<Subject>().Where.And().Equal(new Data.Entities.Subject { project_id = category }).Entities.Script(new Data.Entities.Subject { Id = Guid.Empty }));
                               }

                               pentity.GroupBy(new Proposal { ref_id = Guid.Empty })
                                           .Count(new Proposal { Type = 0 })
                                           .Order.Desc(new Proposal { Type = 0 }).Entities.Query(dr => id2s.Add(dr.ref_id.ToString()));
                               if (id2s.Count > 0)
                               {
                                   if (c != null)
                                   {
                                       var cds = c["ids"] as Array;
                                       foreach (var o in cds)
                                       {
                                           if (id2s.Count > 20)
                                           {
                                               break;
                                           }
                                           id2s.Add(o as string);
                                       }
                                   }
                                   var hash = new Hashtable();
                                   hash["ids"] = id2s.ToArray();
                                   return hash;
                               }
                               return null;

                           });
                        var TimeSpan = Utility.TimeSpan();
                        var ids = new List<Guid>();
                        if (cache.CacheData != null)
                        {
                            var ars = cache.CacheData["ids"] as Array;
                            // cache.
                            foreach (var i in ars)
                            {
                                ids.Add(Utility.Guid(i.ToString()).Value);
                            }
                        }
                        var data = new System.Data.DataTable();
                        data.Columns.Add("id");
                        data.Columns.Add("title");
                        data.Columns.Add("desc");
                        data.Columns.Add("src");
                        data.Columns.Add("path");
                        data.Columns.Add("ppath");
                        data.Columns.Add("project");
                        data.Columns.Add("last");
                        data.Columns.Add("poster");
                        data.Columns.Add("uid");
                        if (ids.Count > 0)
                        {
                            var subEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Subject>();
                            subEntity.Where.And().In(new Subject { Id = ids[0] }, ids.ToArray()).And().Equal(new Data.Entities.Subject { Status = 1 })
                                .And().GreaterEqual(new Subject { Visible = 0 });
                            ;
                            var subs = new List<Subject>();

                            var pris = new List<Guid>();
                            var itemids = new List<Guid>();
                            var uids = new List<Guid>();

                            var webr = UMC.Data.WebResource.Instance();
                            subEntity.Query(new Subject
                            {
                                Id = Guid.Empty,
                                Title = String.Empty,
                                Description = String.Empty,
                                Reply = 0,
                                Favs = 0,
                                Poster = String.Empty,
                                user_id = Guid.Empty,
                                project_id = Guid.Empty,
                                project_item_id = Guid.Empty,
                                Code = String.Empty,
                                Look = 0,
                                ReleaseDate = DateTime.MinValue
                            }, 0, 5, dr =>
                            {
                                subs.Add(dr);
                                pris.Add(dr.project_id.Value);
                                itemids.Add(dr.project_item_id.Value);
                                uids.Add(dr.user_id.Value);
                            });
                            var cates = new List<Project>();
                            var pitems = new List<Data.Entities.ProjectItem>();
                            var users = new List<Data.Entities.User>();


                            if (itemids.Count > 0)
                            {
                                Utility.CMS.ObjectEntity<ProjectItem>().Where.And().In(new ProjectItem
                                {
                                    Id = itemids[0]
                                }, itemids.ToArray())
                                         .Entities.Query(dr => pitems.Add(dr));
                            }
                            if (pris.Count > 0)
                            {
                                Utility.CMS.ObjectEntity<Project>().Where.And().In(new Project { Id = pris[0] }, pris.ToArray())
                                    .Entities.Query(dr => cates.Add(dr));


                            }
                            foreach (var sub in subs)
                            {
                                var u = users.Find(d => d.Id == sub.user_id) ?? new User { Alias = sub.Poster };
                                var cate = cates.Find(g => g.Id == sub.project_id);
                                var pitem = pitems.Find(g => g.Id == sub.project_item_id);
                                if (cate != null && pitem != null)
                                {
                                    data.Rows.Add(sub.Id, sub.Title, sub.Description, webr.ResolveUrl(sub.Id.Value, "1", "0"), String.Format("{0}/{1}/{2}", cate.Code, pitem.Code, sub.Code), cate.Code, cate.Caption, Utility.GetDate(sub.ReleaseDate), u.Alias, sub.user_id);

                                }
                            }

                        }
                        if (Utility.TimeSpan(cache.BuildDate.Value) == TimeSpan)
                        {
                            response.Redirect(new WebMeta().Put("data", data).Put("isPublish", Utility.TimeSpan(cache.BuildDate.Value) == TimeSpan));
                        }
                        else
                        {

                            response.Redirect(new WebMeta().Put("data", data));
                        }



                    }

                    break;
                case "NewProject":
                case "Project":
                    {
                        var webr = UMC.Data.WebResource.Instance();
                        var ids = new List<Guid>();
                        var subMebs = new List<ProjectMember>();


                        var projects = new List<Project>();
                        var pids = new List<Guid>();
                        var order = type == "NewProject" ? new Project { CreationTime = DateTime.MinValue } : new Project { Sequence = 0 };

                        Utility.CMS.ObjectEntity<Project>()
                              .Order.Desc(order).Entities.Query(0, 10, dr =>
                              {
                                  projects.Add(dr);
                                  pids.Add(dr.Id.Value);
                              });

                        if (projects.Count > 0)
                        {
                            var subs = new List<Subject>();
                            Utility.CMS.ObjectEntity<UMC.Data.Entities.Subject>()
                                   .Where.And().In(new Subject { project_id = pids[0] }, pids.ToArray())
                                   .Entities.GroupBy(new Subject { project_id = Guid.Empty }).Count(new Subject { Seq = 0 }).Query(dr => subs.Add(dr));

                            Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectMember>()
                                .Where.And().In(new ProjectMember { project_id = pids[0] }, pids.ToArray())
                                   .Entities.GroupBy(new ProjectMember { project_id = Guid.Empty }).Count(new ProjectMember { AuthType = 0 }).Query(dr => subMebs.Add(dr));



                            var data = new System.Data.DataTable();
                            data.Columns.Add("id");
                            data.Columns.Add("title");
                            data.Columns.Add("desc");
                            data.Columns.Add("src");
                            data.Columns.Add("path");
                            data.Columns.Add("subs");
                            data.Columns.Add("members");

                            foreach (var p in projects)
                            {


                                var sub = subs.Find(s => s.project_id == p.Id) ?? new Data.Entities.Subject() { Seq = 0 };
                                var subMember = subMebs.Find(s => s.project_id == p.Id) ?? new Data.Entities.ProjectMember() { AuthType = 0 };

                                data.Rows.Add(p.Id, p.Caption, p.Description, webr.ResolveUrl(p.Id.Value, "1", "4"), p.Code, sub.Seq, ((int)subMember.AuthType.Value) + 1);
                            }
                            response.Redirect(data);
                        }
                    }
                    break;
                case "Cognate":
                    {

                        var projectId = Utility.Guid(this.AsyncDialog("Project", "Favs"));

                        var project = Utility.CMS.ObjectEntity<UMC.Data.Entities.Project>()
                               .Where.And().Equal(new Project { Id = projectId }).Entities.Single();

                        var webr = UMC.Data.WebResource.Instance();
                        var ids = new List<Guid>();
                        var subMebs = new List<ProjectMember>();


                        var projects = new List<Project>();
                        var pids = new List<Guid>();


                        Utility.CMS.ObjectEntity<UMC.Data.Entities.Project>()
                                .Where.And().Equal(new Project { user_id = project.user_id })
                                .Entities.Order.Desc(new Project { CreationTime = DateTime.MaxValue }).Entities
                                .Query(dr => pids.Add(dr.Id.Value));
                        if (pids.Count > 0)
                        {

                            Utility.CMS.ObjectEntity<Project>().Where.And().In(new Project
                            {
                                Id = pids[0]
                            }, pids.ToArray()).Entities.Query(dr => projects.Add(dr));

                        }
                        else
                        {
                            projects.Add(project);
                            pids.Add(project.Id.Value);
                        }

                        if (projects.Count > 0)
                        {
                            var subs = new List<Subject>();
                            Utility.CMS.ObjectEntity<UMC.Data.Entities.Subject>()
                                   .Where.And().In(new Subject { project_id = pids[0] }, pids.ToArray())
                                   .Entities.GroupBy(new Subject { project_id = Guid.Empty }).Count(new Subject { Seq = 0 }).Query(dr => subs.Add(dr));

                            Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectMember>()
                                .Where.And().In(new ProjectMember { project_id = pids[0] }, pids.ToArray())
                                   .Entities.GroupBy(new ProjectMember { project_id = Guid.Empty }).Count(new ProjectMember { AuthType = 0 }).Query(dr => subMebs.Add(dr));



                            var data = new System.Data.DataTable();
                            data.Columns.Add("id");
                            data.Columns.Add("title");
                            data.Columns.Add("desc");
                            data.Columns.Add("src");
                            data.Columns.Add("path");
                            data.Columns.Add("subs");
                            data.Columns.Add("members");

                            foreach (var p in projects)
                            {
                                var sub = subs.Find(s => s.project_id == p.Id) ?? new Data.Entities.Subject() { Seq = 0 };
                                var subMember = subMebs.Find(s => s.project_id == p.Id) ?? new Data.Entities.ProjectMember() { AuthType = 0 };

                                data.Rows.Add(p.Id, p.Caption, p.Description, webr.ResolveUrl(p.Id.Value, "1", "4"), p.Code, sub.Seq, ((int)subMember.AuthType.Value) + 1);
                            }
                            response.Redirect(data);

                        }
                    }
                    break;

                case "CognateFavs":
                    {
                        var category = Utility.Guid(this.AsyncDialog("Project", "Favs"), true);



                        var cache = UMC.Configuration.ConfigurationManager.DataCache(category ?? Utility.Guid("Cognate", true).Value, "Cognate", 1000, (k, v, c) =>
                        {


                            var project = Utility.CMS.ObjectEntity<UMC.Data.Entities.Project>().Where.And().Equal(new Project { Id = category })
                            .Entities.Single();

                            var id2s = new List<String>();

                            var pentity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Proposal>()
                                        .Where.And().Equal(new Proposal { Type = 1 })
                                        .And().GreaterEqual(new Proposal
                                        {
                                            CreationDate = DateTime.Now.AddDays(-1)
                                        }).Entities;
                            if (category.HasValue)
                            {
                                pentity.Where.And().In("ref_id", Utility.CMS.ObjectEntity<Subject>().Where.And().In("project_id", Utility.CMS.ObjectEntity<Project>().Where.And().Equal(new Project { user_id = project.user_id }).Entities.Script(new Project { Id = Guid.Empty })).Entities.Script(new Data.Entities.Subject { Id = Guid.Empty }));
                            }

                            pentity.GroupBy(new Proposal { ref_id = Guid.Empty })
                                        .Count(new Proposal { Type = 0 })
                                        .Order.Desc(new Proposal { Type = 0 }).Entities.Query(dr => id2s.Add(dr.ref_id.ToString()));
                            if (id2s.Count > 0)
                            {
                                if (c != null)
                                {
                                    var cds = c["ids"] as Array;
                                    foreach (var o in cds)
                                    {
                                        if (id2s.Count > 20)
                                        {
                                            break;
                                        }
                                        id2s.Add(o as string);
                                    }
                                }
                                var hash = new Hashtable();
                                hash["ids"] = id2s.ToArray();
                                return hash;
                            }
                            return null;

                        });
                        var ids = new List<Guid>();
                        if (cache.CacheData != null)
                        {
                            var ars = cache.CacheData["ids"] as Array;
                            foreach (var i in ars)
                            {
                                ids.Add(Utility.Guid(i.ToString()).Value);
                            }
                        }
                        var data = new System.Data.DataTable();
                        data.Columns.Add("id");
                        data.Columns.Add("title");
                        data.Columns.Add("desc");
                        data.Columns.Add("src");
                        data.Columns.Add("path");
                        data.Columns.Add("ppath");
                        data.Columns.Add("project");
                        data.Columns.Add("last");
                        data.Columns.Add("poster");
                        data.Columns.Add("uid");
                        if (ids.Count > 0)
                        {
                            var subEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Subject>();
                            subEntity.Where.And().In(new Subject { Id = ids[0] }, ids.ToArray()).And().Equal(new Data.Entities.Subject { Status = 1 })
                                .And().GreaterEqual(new Subject { Visible = 0 });
                            ;
                            var subs = new List<Subject>();

                            var pris = new List<Guid>();
                            var itemids = new List<Guid>();
                            var uids = new List<Guid>();

                            var webr = UMC.Data.WebResource.Instance();
                            subEntity.Query(new Subject
                            {
                                Id = Guid.Empty,
                                Title = String.Empty,
                                Description = String.Empty,
                                Reply = 0,
                                Favs = 0,
                                Poster = String.Empty,
                                user_id = Guid.Empty,
                                project_id = Guid.Empty,
                                project_item_id = Guid.Empty,
                                Code = String.Empty,
                                Look = 0,
                                ReleaseDate = DateTime.MinValue
                            }, 0, 5, dr =>
                            {
                                subs.Add(dr);
                                pris.Add(dr.project_id.Value);
                                itemids.Add(dr.project_item_id.Value);
                                uids.Add(dr.user_id.Value);
                            });
                            var cates = new List<Project>();
                            var pitems = new List<Data.Entities.ProjectItem>();
                            var users = new List<Data.Entities.User>();


                            if (itemids.Count > 0)
                            {
                                Utility.CMS.ObjectEntity<ProjectItem>().Where.And().In(new ProjectItem
                                {
                                    Id = itemids[0]
                                }, itemids.ToArray())
                                         .Entities.Query(dr => pitems.Add(dr));
                            }
                            if (pris.Count > 0)
                            {
                                Utility.CMS.ObjectEntity<Project>().Where.And().In(new Project { Id = pris[0] }, pris.ToArray())
                                    .Entities.Query(dr => cates.Add(dr));


                            }
                            foreach (var sub in subs)
                            {
                                var u = users.Find(d => d.Id == sub.user_id) ?? new User { Alias = sub.Poster };
                                var cate = cates.Find(g => g.Id == sub.project_id);
                                var pitem = pitems.Find(g => g.Id == sub.project_item_id);
                                if (cate != null && pitem != null)
                                {
                                    data.Rows.Add(sub.Id, sub.Title, sub.Description, webr.ResolveUrl(sub.Id.Value, "1", "0"), String.Format("{0}/{1}/{2}", cate.Code, pitem.Code, sub.Code), cate.Code, cate.Caption, Utility.GetDate(sub.ReleaseDate), u.Alias, sub.user_id);

                                }
                            }

                        }
                        response.Redirect(data);





                    }
                    break;
                default:
                    break;
            }
        }

    }
}