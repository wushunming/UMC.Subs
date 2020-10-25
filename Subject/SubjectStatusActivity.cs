using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Collections;
using System.Reflection;
using UMC.Data.Entities;
using UMC.Data;
using UMC.Web;

namespace UMC.Subs.Activities
{
    class SubjectStatusActivity : WebActivity
    {

        public override void ProcessActivity(WebRequest request, WebResponse response)
        {
            if (request.IsCashier)
            {

                var user = UMC.Security.Identity.Current;
                var Id = UMC.Data.Utility.Guid(this.AsyncDialog("Id", g =>
                {
                    return this.DialogValue(Guid.NewGuid().ToString());
                })).Value;
                var entity = Utility.CMS.ObjectEntity<Subject>()
                        .Where.And().Equal(new Subject { Id = Id }).Entities;
                var sm = entity.Single();
                var Status = Data.Utility.IntParse(this.AsyncDialog("Status", g =>
                {
                    if (sm != null)
                    {

                        var p = new Web.UIRadioDialog() { Title = "发布确认" };
                        p.Options.Add("不发布", "-1");
                        p.Options.Add("发布", "1");
                        return p;
                    }
                    else
                    {

                        var p = new Web.UIRadioDialog() { Title = "评论隐藏" };
                        p.Options.Add("隐藏", "-1");
                        p.Options.Add("显示", "1");
                        return p;
                    }


                }), 0);

                if (sm != null)
                {
                    if (Status > 0)
                    {
                        entity.Update(new Subject { Status = Status, ReleaseDate = DateTime.Now, IsDraught = false });
                    }
                    else
                    {
                        entity.Update(new Subject { Status = Status });
                    }
                }
                else
                {
                    Utility.CMS.ObjectEntity<UMC.Data.Entities.Comment>()
                                 .Where.And().Equal(new Comment { Id = Id }).Entities.Update(new Comment { Visible = Status });
                }
                this.Prompt("设置成功");
            }
        }

    }
}