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



    class SubjectViewActivity : WebActivity
    {


        public override void ProcessActivity(WebRequest request, WebResponse response)
        {
            var Url = this.AsyncDialog("Url", g =>
            {
                this.Context.Send(new UMC.Web.WebMeta().UIEvent("UI.Submit", this.AsyncDialog("UI", "none"), new Web.UIClick(g, "Value")
                {
                    Command = request.Command,
                    Model = request.Model
                }), true);
                this.Prompt("请输入网址");
                return this.DialogValue("none");
            });
            var form = (request.SendValues ?? new UMC.Web.WebMeta()).GetDictionary();
            if (form.ContainsKey("start") == false)
            {
                var buider = new UISectionBuilder(request.Model, request.Command, request.Arguments);
                this.Context.Send(buider.Builder(), true);
            }
            var ui = UISection.Create(new UITitle("图文预览"));

            if (Url.StartsWith("https://") || Url.StartsWith("http://"))
            {
                var user = UMC.Security.Identity.Current;

                var content = System.Text.UTF8Encoding.UTF8.GetString(new UMC.Net.HttpClient().DownloadData(Url));
                if (Url.EndsWith(".md"))
                {
                    ui.NewSection().AddCells(UMC.Data.Markdown.Transform(content));
                    
                }
                else
                {
                    Array celss;
                    if (content.StartsWith("{") == false && content.StartsWith("[") == false)
                    {
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
                    ui.NewSection(celss);
                }
            }
            else
            {
                var subId = Utility.Guid(Url);
                if (subId.HasValue)
                {

                    var subEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Subject>();
                    var sub = subEntity.Where.And().Equal(new UMC.Data.Entities.Subject { Id = subId }).Entities.Single();


                    if (sub == null)
                    {
                        ui.Add("Desc", new UMC.Web.WebMeta().Put("desc", "此图文已删除").Put("icon", "\uF0E6"), new UMC.Web.WebMeta().Put("desc", "{icon}\n{desc}"), new UIStyle().Align(1).Color(0xaaa).Padding(20, 20).BgColor(0xfff).Size(12)
                            .Name("icon", new UIStyle().Font("wdk").Size(60)));

                    }
                    else
                    {


                        var celss = UMC.Data.JSON.Deserialize<WebMeta[]>((String.IsNullOrEmpty(sub.DataJSON) ? "[]" : sub.DataJSON)) ?? new UMC.Web.WebMeta[] { };
                        if (String.Equals("markdown", sub.ContentType, StringComparison.CurrentCultureIgnoreCase))
                        {
                            foreach (var pom in celss)
                            {
                                switch (pom["_CellName"])
                                {
                                    case "CMSImage":

                                        pom.Put("style", new UIStyle().Padding(0, 10));

                                        break;
                                }
                            }
                        }
                        else
                        {
                            foreach (var pom in celss)
                            {
                                switch (pom["_CellName"])
                                {
                                    case "CMSImage":
                                        var value = pom.GetDictionary()["value"] as Hashtable;
                                        if (value != null && value.ContainsKey("size"))
                                        {
                                            value.Remove("size");
                                        }

                                        pom.Put("style", new UIStyle().Padding(0, 10));

                                        break;
                                }
                            }

                        }
                        ui.AddCells(celss); ;
                    }

                }
                else
                {
                    ui.Add("Desc", new UMC.Web.WebMeta().Put("desc", "此图文已删除").Put("icon", "\uF0E6"), new UMC.Web.WebMeta().Put("desc", "{icon}\n{desc}"), new UIStyle().Align(1).Color(0xaaa).Padding(20, 20).BgColor(0xfff).Size(12).Name("icon", new UIStyle().Font("wdk").Size(60)));

                }

            }
            response.Redirect(ui);

        }
    }
}