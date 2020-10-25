using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Collections;
using System.Reflection;
using UMC.Data.Entities;
using UMC.Web.UI;
using UMC.Web;
using System.IO;

namespace UMC.Subs.Activities
{



    class SubjectSectionsActivity : WebActivity
    {


        public override void ProcessActivity(WebRequest request, WebResponse response)
        {


            var list = new List<WebMeta>();
            var stitle = "天天录";
            UITitle title = new UITitle(stitle);
            var items = new List<UIClick>();

            var entity = Data.Database.Instance().ObjectEntity<SearchKeyword>()
                             .Where
                             .And().In(new SearchKeyword { user_id = Guid.Empty })
                             .Entities.Order.Desc(new SearchKeyword { Time = 0 }).Entities;
            entity.Query(0, 20, dr => items.Add(new UIClick(new WebMeta().Put("cmd", "UI", "model", "Subject", "text", dr.Keyword)) { Key = "Search", Text = dr.Keyword }));

            if (items.Exists(g => String.Equals(g.Text, stitle, StringComparison.CurrentCultureIgnoreCase)) == false)
            {
                items.Insert(0, new UIClick(new WebMeta().Put("cmd", "UI", "model", "Subject", "text", stitle)) { Key = "Search", Text = stitle });
            }
            title.Items(items.ToArray());
            title.Name("icon", "\uea0e");

            title.Right(new UIEventText().Icon('\uf2e1').Click(UIClick.Scanning()));
            list.Add(new WebMeta().Put("model", "Subject", "cmd", "UI", "text", "知识推荐").Put("RefreshEvent", "Subject.Save"));
            list.Add(new WebMeta().Put("model", "Subject", "cmd", "Follow", "text", "我的关注").Put("RefreshEvent", "Subject.Save"));
            list.Add(new WebMeta().Put("model", "Subject", "cmd", "Self", "text", "我的文档").Put("RefreshEvent", "Subject.Save"));
            
            response.Redirect(new WebMeta().Put("title", title).Put("sections", list));

        }

    }
}