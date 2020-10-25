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
    class SubjectBestActivity : WebActivity
    {
        public static UIButton BSSArea(Subject su2bs, String model, bool isApp)
        {
            var best = new UIEventText('\uf087', su2bs.Favs > 0 ? String.Format("({0})", su2bs.Favs) : "赞").Format("{icon}{text}")
           .Style(new UIStyle().Radius(20).BorderColor(0x999).Color(0x999).Name("icon", new UIStyle().Font("wdk")).Size(16));

            best.Click(Web.UIClick.Click(new UIClick("Key", su2bs.Id.ToString()) { Model = model, Command = "Best" }));
            var footer = new UIButton();
            footer.Style.AlignCenter().Height(40).Padding(10, 20).Name("min-width", 80);//.Radius(20);
            if (su2bs.IsComment == false)
            {
                if (isApp)
                {


                    footer.Button(best,
                        new UIEventText("分享").Style(new UIStyle().Radius(20).Color(0x999).BorderColor(0x999).Size(16))
                        .Click(Web.UIClick.Click(new UIClick(su2bs.Id.ToString()) { Model = model, Command = "Share" })));
                }
                else
                {
                    footer.Button(best);

                }
            }
            else
            {
                if (isApp)
                {
                    footer.Button(best, new UIEventText("评论").Style(new UIStyle().Radius(20).Color(0x999).BorderColor(0x999).Size(16))
                    .Click(Web.UIClick.Click(new UIClick("Refer", su2bs.Id.ToString()) { Model = model, Command = "Comment" })),
                    new UIEventText("分享").Style(new UIStyle().Radius(20).Color(0x999).BorderColor(0x999).Size(16))
                    .Click(Web.UIClick.Click(new UIClick(su2bs.Id.ToString()) { Model = model, Command = "Share" })));
                }
                else
                {

                    footer.Button(best, new UIEventText("评论").Style(new UIStyle().Radius(20).Color(0x999).BorderColor(0x999).Size(16))
                    .Click(Web.UIClick.Click(new UIClick("Refer", su2bs.Id.ToString()) { Model = model, Command = "Comment" })));
                }
            }
            return footer;
        }
        void UIEvent(String key, Subject cmt, bool isbest, String mode)
        {
            var section = Utility.IntParse(this.AsyncDialog("section", g => this.DialogValue("1")), 0);
            var row = Utility.IntParse(this.AsyncDialog("row", g => this.DialogValue("1")), 0);
            var ui = this.AsyncDialog("UI", g => this.DialogValue("none"));

            //var vale = new UMC.Web.WebMeta().Put("section", section).Put("row", row).Put("method", "PUT").Put("reloadSinle", true);


            new UISection.Editer(section, row).Put(BSSArea(cmt, mode, this.Context.Request.IsApp), true).Builder(this.Context, ui, true);

            //vale.Put("value", );
            //this.Context.Send(new UMC.Web.WebMeta().UIEvent("UI.Edit", ui, vale), true);

        }
        public override void ProcessActivity(WebRequest request, WebResponse response)
        {

            var Key = this.AsyncDialog("Key", g =>
             {
                 return new Web.UITextDialog() { Title = "评论的主题" };
             });
            var refer_id = UMC.Data.Utility.Guid(Key).Value;
            //if (request.IsCashier)
            //{
            //    response.Redirect(request.Model, "Status", refer_id.ToString());
            //}

            var user = UMC.Security.Identity.Current;
            if (user.IsAuthenticated == false)
            {
                this.Prompt("不支持匿名点赞，请登录", false);
                response.Redirect("Account", "Login");

            }
            var entity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Proposal>()
                     .Where.And().Equal(new Proposal { user_id = user.Id, ref_id = refer_id }).Entities;
            var auto = this.AsyncDialog("type", g => this.DialogValue("auto"));
            if (auto == "Check")
            {
                var cmt = Utility.CMS.ObjectEntity<UMC.Data.Entities.Subject>()
                          .Where.And().Equal(new Subject { Id = refer_id })
                         .Entities.Single();

                UIEvent(Key, cmt, entity.Count() > 0, request.Model);

            };

            entity.IFF((Data.Sql.IObjectEntity<Proposal> e) =>
                    {
                        if (e.Delete() > 0)
                        {
                            ;

                            var cmdEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Subject>()
                                      .Where.And().Equal(new Subject { Id = refer_id })
                                     .Entities;
                            cmdEntity.Update("{0}+{1}", new Subject { Favs = -1 });
                            var cmt = cmdEntity.Single();
                            if (cmt == null)
                            {
                                cmt = new Subject()
                                {
                                    Favs = e.Where.Remove(new Proposal { user_id = Guid.Empty }).Entities.Count(),
                                    Id = refer_id
                                };
                            }
                            UIEvent(Key, cmt, false, request.Model);

                            return false;
                        }
                        return true;
                    }
                , (Data.Sql.IObjectEntity<Proposal> e) =>
                    {
                        e.Insert(new UMC.Data.Entities.Proposal
                        {
                            ref_id = refer_id,
                            user_id = user.Id,
                            Type = 1,
                            Poster = user.Alias,
                            CreationDate = DateTime.Now
                        });
                        var cmdEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Subject>()
                                  .Where.And().Equal(new Subject { Id = refer_id })
                                 .Entities;
                        var cmt = cmdEntity.Single();
                        if (cmt != null)
                        {
                            if (cmt.Favs.HasValue)
                            {
                                cmt.Favs += 1;

                                cmdEntity.Update("{0}+{1}", new Subject { Favs = 1 });
                            }
                            else
                            {
                                cmt.Favs = 1;
                                cmdEntity.Update(new Subject { Favs = 1 });
                            }

                            UIEvent(Key, cmt, true, request.Model);

                        }
                        else
                        {

                            cmt = new Subject()
                            {
                                Favs = e.Where.Remove(new Proposal { user_id = Guid.Empty }).Entities.Count(),
                                Id = refer_id
                            };


                        }
                        UIEvent(Key, cmt, true, request.Model);

                    });

        }

    }
}