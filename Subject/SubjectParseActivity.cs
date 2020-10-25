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



    class SubjectParseActivity : WebActivity
    {



        public override void ProcessActivity(WebRequest request, WebResponse response)
        {
            var Url = this.AsyncDialog("Url", g =>
            {
                this.Prompt("请输入URL");
                return this.DialogValue("none");
            });

            var url = this.AsyncDialog("Key", g =>
            {
                return this.DialogValue("none");
            });

            var ussr = UMC.Security.Identity.Current;

            var entity = Utility.CMS.ObjectEntity<Subject>()
                    .Where.And().Equal(new Subject
                    {
                        user_id = ussr.Id,
                        Url = Url
                    }).Entities;
            var sub = entity.Single();
            if (sub != null)
            {
                var cfm = this.AsyncDialog("Confim", g =>
                  {
                      var sdg = new Web.UISelectDialog() { Title = "此内容已经转过码" };
                      sdg.Options.Add("我要重新转码", "1");
                      sdg.Options.Add("查看现有内容", "0");
                      return sdg;
                  });
                if (cfm == "0")
                {
                    response.Redirect(request.Model, "EditUI", sub.Id.ToString());
                }
                entity.Where.And().Equal(new Subject { Id = sub.Id });
            }

            if (url.StartsWith("none") == false)
            {
                var content = System.Text.UTF8Encoding.UTF8.GetString(new UMC.Net.HttpClient().DownloadData(
                 url.StartsWith("http") ? url : String.Format("http://oss.365lu.cn/TEMP/{0}", url)));
                content = content.Replace((char)160, ' ');
                var cont = Data.JSON.Deserialize<Hashtable>(content);

                if (cont != null)
                {
                    if (cont.ContainsKey("markdown"))
                    {

                        var markdown = cont["markdown"] as string;

                        if (String.IsNullOrEmpty(markdown) == false)
                        {
                            var cells = Data.Markdown.Transform(markdown);
                            var dlist = new ArrayList();
                            foreach (var d in cells)
                            {
                                dlist.Add(new WebMeta().Put("_CellName", d.Type).Put("value", d.Data).Put("format", d.Format).Put("style", d.Style).GetDictionary());

                            }
                            if (sub != null)
                            {
                                var sub2 = new Subject
                                {
                                    LastDate = DateTime.Now,
                                    Title = (cont["title"] as string ?? "").Trim(),
                                    DataJSON = Data.JSON.Serialize(dlist),
                                    ContentType = "markdown",
                                    Content = markdown,
                                    Visible = 1,
                                    Status = -1,
                                    last_user_id = ussr.Id,
                                };
                                if (sub.project_id.HasValue == false)
                                {
                                    SubjectSaveActivity.Dashboard(ussr, sub2);
                                }
                                entity.Update(sub2);
                            }
                            else
                            {
                                sub = new Subject()
                                {
                                    Visible = 1,
                                    Title = (cont["title"] as string ?? "").Trim(),
                                    DataJSON = Data.JSON.Serialize(dlist),
                                    ContentType = "markdown",
                                    Content = markdown,
                                    IsComment = false,
                                    IsPicture = false,
                                    Id = Guid.NewGuid(),
                                    Url = Url,
                                    LastDate = DateTime.Now,
                                    Poster = ussr.Alias,
                                    user_id = ussr.Id,
                                    Status = -1
                                };
                                SubjectSaveActivity.Dashboard(ussr, sub);
                                sub.Code = Utility.Parse36Encode(sub.Id.Value.GetHashCode());

                                entity.Insert(sub);
                            }
                            this.Context.Send("Markdown", new WebMeta().Put("Id", sub.Id), true);
                        }
                    }
                    else
                    {
                        var data = cont["content"] as Array;

                        if (data != null)
                        {
                            if (sub != null)
                            {
                                var sub2 = new Subject
                                {
                                    Visible = 1,
                                    LastDate = DateTime.Now,
                                    Title = (cont["title"] as string ?? "").Trim(),
                                    DataJSON = Data.JSON.Serialize(data),
                                    ContentType = "text/html",
                                    Status = -1,
                                    last_user_id = ussr.Id,
                                };
                                if (sub.project_id.HasValue == false)
                                {
                                    SubjectSaveActivity.Dashboard(ussr, sub2);
                                }
                                entity.Update(sub2);
                            }
                            else
                            {
                                sub = new Subject()
                                {
                                    Visible = 1,
                                    Title = (cont["title"] as string ?? "").Trim(),
                                    DataJSON = Data.JSON.Serialize(data),
                                    ContentType = "text/html",
                                    IsPicture = false,
                                    Id = Guid.NewGuid(),
                                    Url = Url,
                                    LastDate = DateTime.Now,
                                    IsComment = false,
                                    Poster = ussr.Alias,
                                    user_id = ussr.Id,
                                    Status = -1
                                };
                                SubjectSaveActivity.Dashboard(ussr, sub);
                                sub.Code = Utility.Parse36Encode(sub.Id.Value.GetHashCode());
                                entity.Insert(sub);
                            }
                        }
                    }


                }
            }
            this.Context.Send(new UISectionBuilder(request.Model, "Content", new UMC.Web.WebMeta().Put("Id", sub.Id).Put("Next", "Subject"))
                .CloseEvent("Subject.Save")
                    .Builder().Put("IsEditer", true), true);

        }
    }
}