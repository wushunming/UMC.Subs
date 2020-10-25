using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Collections;
using System.Reflection;
using UMC.Web.UI;
using UMC.Web;
using UMC.Data.Entities;

namespace UMC.Subs.Activities
{
    class SubjectImageActivity : WebActivity
    {
        public override void ProcessActivity(WebRequest request, WebResponse response)
        {

            var strId = this.AsyncDialog("Id", g =>
             {
                 return new Web.UITextDialog() { Title = "主题" };
             });
            var form = request.SendValues ?? new UMC.Web.WebMeta();

            var sid = UMC.Data.Utility.Guid(strId, true);
            var seq = Utility.IntParse(this.AsyncDialog("Seq", g =>
            {
                return this.DialogValue("0");
            }), 0);
            var webr = UMC.Data.WebResource.Instance();
            var Url = this.AsyncDialog("Url", gK =>
            {
                if (form.ContainsKey("limit") == false)
                {

                    this.Context.Send(new UISectionBuilder(request.Model, request.Command, request.Arguments)

                        .CloseEvent("image")
                            .Builder(), true);
                }

                var args = request.Arguments.GetDictionary();

                int limit = UMC.Data.Utility.IntParse(form["limit"] as string, 25);

                int start = UMC.Data.Utility.IntParse(form["start"] as string, 0);
                var ui = UISection.Create();
                var subEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Subject>();
                subEntity.Where.And().Equal(new UMC.Data.Entities.Subject { Id = sid });

                var title = new UITitle("正文图片");
                //title.Float();
                ui.Title = title;

                var items = new List<WebMeta>();
                var domain = webr.WebDomain();


                var su2bs = subEntity.Single() ?? new UMC.Data.Entities.Subject { Id = sid };

                var celss = UMC.Data.JSON.Deserialize<WebMeta[]>((String.IsNullOrEmpty(su2bs.DataJSON) ? "[]" : su2bs.DataJSON)) ?? new UMC.Web.WebMeta[] { };
                foreach (var pom in celss)
                {
                    switch (pom["_CellName"])
                    {
                        case "CMSImage":
                            var value = pom.GetMeta("value");
                            var src = value["src"];
                            if (String.IsNullOrEmpty(src) == false)
                            {
                                if (src.StartsWith(domain) || src.StartsWith("http://www.365lu.cn") || src.StartsWith("https://www.365lu.cn") || src.StartsWith("https://oss.") || src.StartsWith("http://oss.")
                                )
                                {
                                    var ind = src.IndexOf("!");
                                    if (ind > 0)
                                    {
                                        src = src.Substring(0, ind);
                                    }
                                    items.Add(new UMC.Web.WebMeta().Put("src", src + "!200").Put("click", new Web.UIClick(new UMC.Web.WebMeta(args).Put(gK, src))
                                    {
                                        Model = request.Model,
                                        Command = request.Command
                                    }));
                                }

                            }

                            break;
                    }
                }
                if (items.Count > 0)
                {

                    var nine = new UMC.Web.WebMeta().Put("images", items);
                    ui.NewSection().Add(UICell.Create("NineImage", nine));
                }
                else
                {


                    ui.Add("Desc", new UMC.Web.WebMeta().Put("desc", "图文未有图片或图片非本站").Put("icon", "\uF016"), new UMC.Web.WebMeta().Put("desc", "{icon}\n{desc}"),

                      new UIStyle().Align(1).Color(0xaaa).Padding(20, 20).BgColor(0xfff).Size(12).Name("icon", new UIStyle().Font("wdk").Size(60)));
                }
                //ui.AddCells(celss); ;

                response.Redirect(ui);
                return this.DialogValue("none");
            });

            var entity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Picture>();

            entity.Where.And().Equal(new UMC.Data.Entities.Picture { Seq = seq, group_id = sid });
            var pic = entity.Single();
            var user = UMC.Security.Identity.Current;
            if (pic == null)
            {
                pic = entity.Where.Reset().And().Equal(new Picture { group_id = sid }).Entities.Max(new Picture { Seq = 0 });//.Seq + 1;
                pic.Seq = (pic.Seq ?? 0) + 1;
                var photo = new UMC.Data.Entities.Picture
                {
                    group_id = sid,
                    Seq = pic.Seq,
                    user_id = user.Id,
                    UploadDate = DateTime.Now
                };
                entity.Insert(photo);
            }
            else
            {
                entity.Update(new UMC.Data.Entities.Picture
                {
                    Location = request.UserHostAddress,
                    UploadDate = DateTime.Now,
                    user_id = user.Id
                });

            }
            webr.Transfer(new Uri(Url), sid.Value, pic.Seq ?? 1);
            this.Context.Send("image", true);

        }

    }
}