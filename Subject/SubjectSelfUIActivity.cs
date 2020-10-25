using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Collections;
using System.Reflection;
using UMC.Web;
using UMC.Data.Entities;
using UMC.Web.UI;

namespace UMC.Subs.Activities
{
    /// <summary>
    /// �����˻�
    /// </summary>
    public class SubjectSelfUIActivity : WebActivity
    {

        public override void ProcessActivity(WebRequest request, WebResponse response)
        {
            var identity = UMC.Security.Identity.Current;

            var userId = identity.Id;

            var form = (request.SendValues ?? new UMC.Web.WebMeta()).GetDictionary();
            var webr = UMC.Data.WebResource.Instance();
            if (form.ContainsKey("limit") == false)
            {

                var buider = new UISectionBuilder(request.Model, request.Command, request.Arguments);
                this.Context.Send(buider.Builder(), true);
            }


            var logoUrl = webr.ResolveUrl(userId.Value, "1", 4);

            var members = identity.IsAuthenticated ? Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectMember>()
                  .Where.And().Equal(new Data.Entities.ProjectMember { user_id = userId })
                  .Entities.Count() : 0;

            var suject = identity.IsAuthenticated ?
            Utility.CMS.ObjectEntity<Subject>()
               .Where.And().Equal(new Subject { user_id = userId })
               .Entities.GroupBy().Sum(new Subject { Reply = 0 })
               .Sum(new Subject { Look = 0 }).Count(new Subject { Seq = 0 }).Single() : new Subject() { Seq = 0, Reply = 0, Look = 0 };

            if (identity.IsAuthenticated)
            {
                members += Utility.CMS.ObjectEntity<UMC.Data.Entities.Project>()
                  .Where.And().Equal(new Data.Entities.Project { user_id = userId })
                  .Entities.Count();
            }

            var Discount = new UIHeader.Portrait(identity.IsAuthenticated ? logoUrl : "https://oss.365lu.cn/css/images/header_image.png");

            ;
            var user = identity;
            Discount.Value(user.IsAuthenticated ? user.Alias : "���¼");
            Discount.Click(new UIClick().Send("Account", "Self"));

            if (user.IsAuthenticated)
            {
                var sign = Data.Database.Instance().ObjectEntity<Data.Entities.Account>()
                    .Where.And().Equal(new Data.Entities.Account { user_id = userId, Type = Security.Account.SIGNATURE_ACCOUNT_KEY }).Entities.Single();
                //if (sign != null)
                Discount.Time(sign != null ? sign.Name : "��");// user.ActiveTime.ToString());
            }
            else
            {
                Discount.Time("��");
            }
            var color = 0x63b359;
            Discount.Gradient(color, color);
            var header = new UIHeader();
            var title = UITitle.Create();

            title.Title = "�ҵ�";
            header.AddPortrait(Discount);

            title.Style.BgColor(color);
            title.Style.Color(0xfff);

            var ui = UISection.Create(header, title);


            var uIIcon = new UIIconNameDesc(new UIIconNameDesc.Item('\uF19d', "������Ŀ", members + "��").Color(0x40c9c6));
            if (user.IsAuthenticated)
            {
                uIIcon.Button("�鿴", UIClick.Pager("Subject", "Account", new WebMeta().Put("selectIndex", 1)), 0x1890ff);
            }
            else
            {
                uIIcon.Button("���¼", new UIClick().Send("Account", "Login"), 0xb7babb);
            }
            ui.Add(uIIcon);
            uIIcon = new UIIconNameDesc(new UIIconNameDesc.Item('\uF02d', "֪ʶ����", suject.Seq + "ƪ").Color(0x36a3f7), new UIIconNameDesc.Item('\uf0e6', "������", suject.Reply + "��").Color(0x34bfa3));
            //new UIIconNameDesc.Item('\uf06e', "�����", suject.Look + "��").Color(0xf4516c),
            ui.Add(uIIcon);


            ui.NewSection()
                .AddCell('\uf198', "�½��ĵ�", "����Markdown��ʽ��д", new Web.UIClick("Markdown").Send("Subject", "Content"))
                .AddCell('\uf13b', "�½����ı��ĵ�", "���ø��ı���ʽ��д", new Web.UIClick("News").Send("Subject", "Content"))
                .AddCell('\uf0c5', "ץȡ�ĵ�", "��ճ�������ַ��ץȡ�ĵ�", new Web.UIClick() { Key = "CaseCMS" });

            ui.NewSection().AddCell('\uf2e1', "ɨһɨ", "", new Web.UIClick() { Key = "Scanning" });
            ui.NewSection()
                //.AddCell('\uf19c', "�л���ҵ", "", Web.UIClick.Pager("Platform", "Corp", true))
                .AddCell('\uf013', "����", "", Web.UIClick.Pager("UI", "Setting", true, "Close"));



            response.Redirect(ui);

        }
    }
}