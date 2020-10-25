using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UMC.Data;
using UMC.Data.Entities;
using UMC.Data.Sql;
using UMC.Net;

namespace UMC.Subs.Entities
{
    [Web.Mapping]
    class Initializer : UMC.Data.Sql.Initializer
    {
        public override string Caption => "知识录";
        public override string ProviderName => "defaultDbProvider";

        public override string Name => "Sub";
        public override int PageIndex => 10;


        public override void Menu(IDictionary hash, DbFactory factory)
        {

            factory.ObjectEntity<Data.Entities.Menu>()
                   .Insert(new Data.Entities.Menu()
                   {
                       Icon = "\uf02d",
                       Caption = "内容管理",
                       IsDisable = false,
                       ParentId = Guid.Empty,
                       Seq = 92,
                       Id = Guid.NewGuid(),
                       Url = "#subject/items"

                   });
        }

        protected override void Setup(IDictionary hash, DbFactory factory)
        {

            if (factory.ObjectEntity<UMC.Data.Entities.Project>()
                .Where.And().Equal(new Project { Code = "365lu" }).Entities.Count() == 0)
            {

                System.Net.Http.HttpClient httpClient = new System.Net.Http.HttpClient();
                var pdata = UMC.Data.JSON.Deserialize(httpClient.GetStringAsync("https://ali.365lu.cn/UMC/Export/Subject/Export/365lu")
                      .Result) as Hashtable;
                var team = new Project();
                team.ModifiedTime = DateTime.Now;

                var user = factory.ObjectEntity<User>().Where.And().Equal(new User { Username = "admin" }).Entities.Single();

                team.Id = user.Id;
                team.user_id = user.Id;
                team.Code = "365lu";
                team.CreationTime = DateTime.Now;
                team.Caption = pdata["title"] as string;
                team.Description = pdata["desc"] as string;
                team.Sequence = 0;
                var items = pdata["data"] as Array;

                factory.ObjectEntity<UMC.Data.Entities.Project>().Insert(team);
                Data.WebResource.Instance().Transfer(new Uri("https://oss.365lu.cn/UserResources/app/zhishi-icon.jpg"), team.Id.Value, 1);
                var its = new List<ProjectItem>();
                var pos = new List<Portfolio>();
                var subs = new List<Subject>();
                foreach (var s in items)
                {
                    var sd = s as Hashtable;
                    var p = new ProjectItem()
                    {
                        Id = Guid.NewGuid(),
                        Caption = sd["title"] as string,
                        Code = sd["code"] as string,
                        CreationTime = DateTime.Now,
                        project_id = team.Id,
                        Sequence = its.Count,
                        user_id = user.Id,
                    };
                    its.Add(p);
                    var podata = sd["data"] as Array;
                    foreach (var po in podata)
                    {
                        var pod = po as Hashtable;
                        var portfolio = new Portfolio()
                        {
                            Id = Guid.NewGuid(),
                            Caption = pod["title"] as string,
                            Count = 0,
                            CreationTime = DateTime.Now,
                            Sequence = pos.Count,
                            user_id = user.Id,
                            project_id = team.Id,
                            project_item_id = p.Id,
                        };
                        pos.Add(portfolio);
                        var sdata = pod["data"] as Array;
                        //  int seq=0
                        foreach (var sb in sdata)
                        {
                            var sbd = sb as Hashtable;
                            subs.Add(new Subject()
                            {
                                Id = Guid.NewGuid(),
                                Title = sbd["title"] as string,
                                CreationTime = DateTime.Now,
                                Seq = subs.Count,
                                user_id = user.Id,
                                portfolio_id = portfolio.Id,
                                Visible = 1,
                                Status = 1,
                                IsDraught = false,
                                ContentType = "text/html",
                                DataJSON = sbd["content"] as string,
                                Code = sbd["code"] as string,
                                ReleaseDate = DateTime.Now,
                                project_id = team.Id,
                                project_item_id = p.Id,
                            });
                        }
                    }

                }

                factory.ObjectEntity<UMC.Data.Entities.ProjectItem>().Insert(its.ToArray());
                factory.ObjectEntity<UMC.Data.Entities.Portfolio>().Insert(pos.ToArray());
                factory.ObjectEntity<UMC.Data.Entities.Subject>().Insert(subs.ToArray());

            }
        }
        public override bool Resource(NetContext context, String path)
        {
            var paths = new List<string>(path.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries));
            if (paths.Count == 0)
            {
                paths.Add("index.html");
            }

            var last = paths[paths.Count - 1];
            switch (last)
            {
                case "Sub.js":
                    {

                        using (System.IO.Stream stream = typeof(Initializer).Assembly
                                           .GetManifestResourceStream("UMC.Subs.Resources.sub.js"))
                        {
                            UMC.Data.Utility.Copy(stream, context.OutputStream);

                        }
                    }
                    return true;
                case "sub.html":
                case "index.html":
                    context.ContentType = "text/html";

                    using (System.IO.Stream stream = typeof(Initializer).Assembly
                                       .GetManifestResourceStream("UMC.Subs.Resources.sub.html"))
                    {
                        UMC.Data.Utility.Copy(stream, context.OutputStream);

                    }
                    return true;
                default:
                    if (last.IndexOf('.') > -1)
                    {
                        context.Redirect(WebResource.Instance().WebDomain());
                        return true;
                    }
                    break;
            }
            switch (paths[0].ToLower())
            {
                case "download":
                case "dashboard":
                case "explore":
                    break;
                default:
                    if (paths.Count > 3)
                    {
                        context.Redirect(WebResource.Instance().WebDomain());
                        return true;

                    }

                    var project = Utility.CMS.ObjectEntity<UMC.Data.Entities.Project>()
                        .Where.And().Equal(new Project { Code = paths[0] }).Entities.Single();
                    if (project == null)
                    {
                        if (paths.Count > 1)
                        {
                            context.Redirect(WebResource.Instance().WebDomain());
                            return true;
                        }
                        return false;
                    }
                    if (paths.Count > 1)
                    {
                        if (paths.Count == 2 && paths[1] == "follow")
                        {

                        }
                        else
                        {
                            var projectItem = Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectItem>()
                                 .Where.And().Equal(new ProjectItem { Code = paths[1], project_id = project.Id }).Entities.Single();
                            if (projectItem == null)
                            {

                                context.Redirect(WebResource.Instance().WebDomain() + project.Code);
                                //context.StatusCode = 404;
                                return true;
                            }

                            if (paths.Count > 2)
                            {
                                var memberCount = Utility.CMS.ObjectEntity<UMC.Data.Entities.Subject>()
                                     .Where.And().Equal(new Subject
                                     {
                                         Code = paths[2],
                                         project_id = project.Id,
                                         project_item_id = projectItem.Id
                                     }).Entities.Count();
                                if (memberCount == 0)
                                {

                                    context.Redirect(String.Format("{2}{0}/{1}", project.Code, projectItem.Code, WebResource.Instance().WebDomain()));

                                    return true;
                                }

                            }
                        }
                    }
                    break;
            }
            context.ContentType = "text/html";

            using (System.IO.Stream stream = typeof(Initializer).Assembly
                               .GetManifestResourceStream("UMC.Subs.Resources.sub.html"))
            {
                UMC.Data.Utility.Copy(stream, context.OutputStream);

            }
            return true;
        }
    }
}
