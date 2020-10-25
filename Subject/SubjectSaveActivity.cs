using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UMC.Web.UI;
using UMC.Web;
using UMC.Data.Entities;
using System.Collections;

namespace UMC.Subs.Activities
{



    class SubjectSaveActivity : WebActivity
    {

        public static void Dashboard(UMC.Security.Identity user, Subject sub)
        {
            var project = Utility.CMS.ObjectEntity<UMC.Data.Entities.Project>()
                             .Where.And().Equal(new Project { Id = user.Id }).Entities.Single();
            if (project == null)
            {
                var team = new Project();
                team.ModifiedTime = DateTime.Now;


                team.Id = user.Id;
                team.user_id = user.Id;
                team.Code = Utility.Parse36Encode(team.Id.Value.GetHashCode());
                team.CreationTime = DateTime.Now;
                team.Caption = user.Alias;
                team.Sequence = 0;
                sub.project_id = team.Id;


                Utility.CMS.ObjectEntity<UMC.Data.Entities.Project>().Insert(team);
                Data.WebResource.Instance().Transfer(new Uri("https://oss.365lu.cn/UserResources/app/zhishi-icon.jpg"), team.Id.Value, 1);
                var p = new ProjectItem()
                {
                    Id = Guid.NewGuid(),
                    Caption = "天天录",
                    Code = "365lu",
                    CreationTime = DateTime.Now,
                    project_id = team.Id,
                    Hide = false,
                    Sequence = 0,
                    user_id = user.Id,
                };
                Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectItem>()
                                        .Insert(p);
                sub.project_item_id = p.Id;

                var portfolio2 = new Portfolio()
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
                                        .Insert(portfolio2);
                sub.portfolio_id = portfolio2.Id;
                return;
            }
            var projectItem = Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectItem>()
                  .Where.And().Equal(new ProjectItem { project_id = project.Id }).Entities.Order.Asc(new ProjectItem { Sequence = 0 })
                  .Entities.Single();
            sub.project_id = project.Id;
            sub.project_item_id = projectItem.Id;

            var portfolio = Utility.CMS.ObjectEntity<UMC.Data.Entities.Portfolio>()
                  .Where.And().Equal(new Portfolio { project_id = project.Id, project_item_id = projectItem.Id }).Entities.Order.Asc(new Portfolio { Sequence = 0 })
                  .Entities.Single();

            sub.portfolio_id = portfolio.Id;

        }

        public override void ProcessActivity(WebRequest request, WebResponse response)
        {
            var Id = UMC.Data.Utility.Guid(this.AsyncDialog("Id", g =>
            {
                return this.DialogValue("none");
            }), true);
            var ui = this.AsyncDialog("UI", "none");
            var Next = this.AsyncDialog("Next", "none");
            var Url = this.AsyncDialog("Url", g =>
            {
                this.Context.Send(new UMC.Web.WebMeta().UIEvent("UI.Submit", ui, new Web.UIClick("Id", Id.ToString(), "UI", ui, "Next", Next, g, "Value")
                {
                    Command = request.Command,
                    Model = request.Model
                }), true);

                return this.DialogValue("none");
            });

            var entity = Utility.CMS.ObjectEntity<Subject>()
                    .Where.And().Equal(new Subject
                    {
                        Id = Id
                    }).Entities;
            var sub = entity.Single();
            var user = UMC.Security.Identity.Current;
            if (user.IsAuthenticated == false)
            {
                if (request.IsApp)
                {
                    response.Redirect("Account", "Login");
                }
                else
                {
                    this.Prompt("请登录");
                }
            }


            var ContentType = "html";// Markdown
            Array celss;
            var content = System.Text.UTF8Encoding.UTF8.GetString(new UMC.Net.HttpClient().DownloadData(Url));
            if (content.StartsWith("{") == false && content.StartsWith("[") == false)
            {
                ContentType = "markdown";
                var cells = Data.Markdown.Transform(content);
                var dlist = new ArrayList();
                foreach (var d in cells)
                {
                    dlist.Add(new WebMeta().Put("_CellName", d.Type).Put("value", d.Data).Put("format", d.Format).Put("style", d.Style).GetDictionary());

                }
                celss = dlist.ToArray();
            }
            else
            {
                var conts = Data.JSON.Deserialize(content) as Array;
                content = null;
                var cont = conts.GetValue(1) as Hashtable;

                celss = cont["data"] as Array;
            }
            if (sub == null)
            {
                var Title = String.Format("{0:yyyy-MM-dd hh:mm}", DateTime.Now);
                if (celss.Length > 0)
                {

                    var pom = celss.GetValue(0) as Hashtable;
                    var Cname = (pom["_CellName"] as string ?? "");
                    if (Cname == "CMSText")
                    {
                        var format = pom["format"] as Hashtable;
                        if (format == null)
                        {
                            format = (pom["format"] as WebMeta).GetDictionary();
                        }
                        var value = pom["value"] as Hashtable;
                        if (value == null)
                        {
                            value = (pom["value"] as WebMeta).GetDictionary();
                        }
                        if (value == null)
                        {
                            value = (pom["value"] as WebMeta).GetDictionary();
                        }
                        if (format == null)
                        {
                            format = new Hashtable();
                        }
                        if (value == null)
                        {
                            Title = "未设置标题";
                        }
                        else
                        {
                            Title = Utility.Format((format["text"] as string) ?? "{text}", value);
                        }
                    }

                }

                sub = new Subject()
                {
                    Visible = 1,
                    Title = Title,
                    IsPicture = false,
                    Id = Id,
                    Url = Url,
                    IsDraught = true,
                    ReleaseDate = DateTime.Now,
                    LastDate = DateTime.Now,
                    last_user_id = user.Id,
                    CreationTime = DateTime.Now,
                    Poster = user.Alias,
                    user_id = user.Id,
                    Favs = 0,
                    Look = 0,
                    Reply = 0,
                    Score = 0,
                    Seq = 0,
                    IsComment = false,
                    Status = -1
                };
                sub.Code = Utility.Parse36Encode(sub.Id.Value.GetHashCode());
                Dashboard(user, sub);
                entity.Insert(sub);

                if (String.Equals(ContentType, "markdown"))
                {
                    Next = "Subject";
                }
            }
            var webr = UMC.Data.WebResource.Instance();
            var domain = webr.WebDomain();
            var list = new Dictionary<String, Uri>();
            var mains = new List<String>();
            var sp = UMC.Data.Utility.TimeSpan();
            foreach (var o in celss)
            {
                var pom = o as Hashtable;
                var Cname = (pom["_CellName"] as string ?? "");

                switch (Cname)
                {
                    case "CMSImage":
                        var value = pom["value"] as Hashtable;
                        if (value == null)
                        {
                            value = (pom["value"] as WebMeta).GetDictionary();
                        }
                        var src = new Uri(value["src"] as string);
                        if (domain.Contains(src.Host) == false)
                        {

                            var jpg = ".jpg";
                            var ex = src.AbsolutePath.LastIndexOf(".");

                            if (ex > -1)
                            {
                                jpg = src.AbsolutePath.Substring(ex);

                            }
                            var gk = UMC.Data.Utility.Guid(src.AbsoluteUri, true);
                            var srcKey = String.Format("UserResources/Subject/{1}{3}", domain, gk, list.Count, jpg, sp);
                            value["src"] = domain + srcKey;
                            list[srcKey] = src;
                            mains.Add(srcKey);
                        }
                        else
                        {
                            mains.Add(src.AbsolutePath.TrimStart('/'));
                        }
                        break;
                }
            }

            entity.Update(new Subject
            {
                ContentType = ContentType,
                Content = content,
                LastDate = DateTime.Now,
                DataJSON = Data.JSON.Serialize(celss)
            });
            var pictureEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Picture>();
            pictureEntity.Order.Asc(new Data.Entities.Picture { Seq = 0 });
            pictureEntity.Where.Reset().And().Equal(new Data.Entities.Picture { group_id = Id });
            var count = pictureEntity.Count();
            var images = new List<Picture>();
            if (count == 0)
            {
                if (mains.Count > 2)
                {
                    images.Add(new Data.Entities.Picture
                    {
                        group_id = Id,
                        Seq = 1,
                        UploadDate = DateTime.Now,
                        user_id = user.Id
                    }); images.Add(new Data.Entities.Picture
                    {
                        group_id = Id,
                        Seq = 2,
                        UploadDate = DateTime.Now,
                        user_id = user.Id
                    }); images.Add(new Data.Entities.Picture
                    {
                        group_id = Id,
                        Seq = 3,
                        UploadDate = DateTime.Now,
                        user_id = user.Id
                    });

                }
                else if (mains.Count > 0)
                {
                    images.Add(new Data.Entities.Picture
                    {
                        group_id = Id,
                        Seq = 1,
                        UploadDate = DateTime.Now,
                        user_id = user.Id
                    });
                }

            }
            if (count == 0)
            {
                if (images.Count > 2)
                {
                    webr.Transfer(new Uri(domain + mains[0]), Id.Value, 1);
                    webr.Transfer(new Uri(domain + mains[1]), Id.Value, 2);
                    webr.Transfer(new Uri(domain + mains[2]), Id.Value, 3);
                }
                else if (mains.Count > 0)
                {
                    webr.Transfer(new Uri(domain + mains[0]), Id.Value, 1);

                }
                if (images.Count > 0)
                    pictureEntity.Insert(images.ToArray());
            }
            var appKey = UMC.Security.Principal.Current.AppKey ?? Guid.Empty;

            Data.Reflection.Start(() =>
            {
                UMC.Security.Principal.Create(appKey);
                try
                {
                    var em = list.GetEnumerator();
                    while (em.MoveNext())
                    {
                        webr.Transfer(em.Current.Value, em.Current.Key);

                    }
                }
                catch (Exception ex)
                {
                    Utility.Error("Subject/image", ex.StackTrace);
                }
            });

            this.Context.Send(new UMC.Web.WebMeta().UIEvent("UI.Save", ui, new UMC.Web.WebMeta()), false);

            if (Next == "Subject")
            {
                this.Prompt("保存成功", false);
                this.Context.Send("Subject.Save", false);
                this.Context.Send(new UISectionBuilder(request.Model, "EditUI", new UMC.Web.WebMeta().Put("Id", Id))
                    .RefreshEvent("Subject.Save", "image", "Subject.Content")
                        .Builder(), true);
            }
            else if (Next == "View")
            {
                this.Context.Send(new UISectionBuilder(request.Model, "UIMin", new UMC.Web.WebMeta().Put("Id", sub.Id))
                        .Builder(), true);
            }
            else
            {
                this.Prompt("保存成功", false);
                this.Context.Send("Subject.Content", true);
            }
        }

    }
}