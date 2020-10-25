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
    class SubjectSubmitActivity : WebActivity
    {
        String Save(Array cells)
        {

            var webr = UMC.Data.WebResource.Instance();
            var domain = webr.WebDomain();
            var list = new Dictionary<String, Uri>();
            //var mains = new List<String>();
            var sp = UMC.Data.Utility.TimeSpan();
            var isdomain = domain.IndexOf('.');
            var nsufer = isdomain == -1 ? "" : domain.Substring(isdomain).Trim('/'); ;

            foreach (var o in cells)
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
                        if (String.IsNullOrEmpty(nsufer) == false)
                        {
                            if (src.Host.EndsWith(nsufer) == false)
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

                            }
                        }
                        break;
                }
            }
            var appKey = UMC.Security.Principal.Current.AppKey ?? Guid.Empty;

            UMC.Data.Reflection.Start(() =>
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
            return Data.JSON.Serialize(cells);
        }

        public override void ProcessActivity(WebRequest request, WebResponse response)
        {
            var subValue = request.SendValues;
            var user = UMC.Security.Identity.Current;

            var sub = new Subject();
            if (subValue.ContainsKey("Markdown"))
            {
                UMC.Data.Reflection.SetProperty(sub, subValue.GetDictionary());
                String Markdown = subValue["Markdown"];
                sub.ContentType = "markdown";
                sub.Content = Markdown;
                if (sub.Content == "none")
                {
                    sub.Content = null;
                    sub.ContentType = null;
                }
                else
                {
                    var cells = Data.Markdown.Transform(Markdown);
                    var dlist = new ArrayList();
                    foreach (var d in cells)
                    {
                        dlist.Add(new WebMeta().Put("_CellName", d.Type).Put("value", d.Data).Put("format", d.Format).Put("style", d.Style).GetDictionary());

                    }

                    sub.DataJSON = this.Save(dlist.ToArray());
                }


            }
            else
            {

                UMC.Data.Reflection.SetProperty(sub, subValue.GetDictionary());
                sub.ContentType = "text/html";

                sub.DataJSON = this.Save(Data.JSON.Deserialize(sub.DataJSON.Replace((char)160, ' ')) as Array);
            }
            sub.IsPicture = sub.IsPicture ?? false;
            var entity = Utility.CMS.ObjectEntity<Subject>()
                    .Where.And().Equal(new Subject
                    {
                        Id = sub.Id.Value
                    }).Entities;
            var oldSub = entity.Single();
            if (oldSub != null)
            {

                if (oldSub.project_id.HasValue)
                {
                    var project = Utility.CMS.ObjectEntity<Project>().Where.And().Equal(new Project
                    {
                        Id = oldSub.project_id
                    }).Entities.Single();
                    if (project != null && project.user_id == user.Id)
                    {

                    }
                    else
                    {
                        var member = Utility.CMS.ObjectEntity<ProjectMember>().Where.And().Equal(new ProjectMember
                        {
                            project_id = oldSub.project_id,
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
                                    this.Prompt("您未有编辑此图文的权限");
                                    break;
                            }

                        }
                        else
                        {
                            this.Prompt("您未有编辑此图文的权限");
                        }
                    }

                    sub.last_user_id = user.Id;
                    sub.LastDate = DateTime.Now;
                    if (oldSub.last_user_id == sub.last_user_id && DateTime.Now.AddHours(-3) < oldSub.LastDate)
                    {
                        Utility.CMS.ObjectEntity<ProjectDynamic>().Where.And()
                            .Equal(new ProjectDynamic
                            {
                                Time = Utility.TimeSpan(oldSub.LastDate.Value),
                                user_id = user.Id
                            })
                            .Entities.Update(new ProjectDynamic
                            {
                                Time = Utility.TimeSpan(sub.LastDate.Value),
                                Explain = "更新了文档"
                            });

                    }
                    else
                    {
                        Utility.CMS.ObjectEntity<ProjectDynamic>()
                            .Insert(new ProjectDynamic
                            {
                                Time = Utility.TimeSpan(sub.LastDate.Value),
                                user_id = user.Id,
                                Explain = "更新了文档",
                                project_id = oldSub.project_id,
                                refer_id = oldSub.Id,
                                Title = oldSub.Title,
                                Type = DynamicType.Subject
                            });

                    }
                }
                entity.Update(sub);

            }
            else
            {
                if (String.IsNullOrEmpty(sub.Title) == false)
                {
                    var celss = UMC.Data.JSON.Deserialize(sub.DataJSON) as Array;
                    sub.Title = String.Format("{0:yyyy-MM-dd hh:mm}", DateTime.Now);
                    if (celss != null && celss.Length > 0)
                    {

                        var pom = celss.GetValue(0) as Hashtable;
                        var Cname = (pom["_CellName"] as string ?? "");
                        if (Cname == "CMSText")
                        {
                            var format = pom["format"] as Hashtable;
                            var value = pom["value"] as Hashtable;
                            if (value == null)
                            {
                                value = (pom["value"] as WebMeta).GetDictionary();
                            }

                            sub.Title = Utility.Format((format["text"] as string) ?? "{text}", value);
                        }

                    }
                }
                sub.Reply = 0;
                sub.Favs = 0;
                sub.Look = 0;
                sub.user_id = user.Id;
                sub.IsPicture = false;
                sub.IsDraught = true;
                sub.Poster = user.Alias;
                sub.Code = Utility.Parse36Encode(sub.Id.Value.GetHashCode());
                sub.LastDate = DateTime.Now;
                sub.last_user_id = user.Id;
                entity.Insert(sub);


            }
            this.Prompt("保存成功", false);
            this.Context.Send("subject", new WebMeta().Put("time", DateTime.Now.ToShortTimeString()), true);
        }



    }
}