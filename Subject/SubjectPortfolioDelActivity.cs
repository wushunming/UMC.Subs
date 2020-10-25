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
    class SubjectPortfolioDelActivity : WebActivity
    {
        public override void ProcessActivity(WebRequest request, WebResponse response)
        {
            var sid = UMC.Data.Utility.Guid(this.AsyncDialog("Id", request.Model, "Portfolio"));

            var user = UMC.Security.Identity.Current;
            var Portfolios = new List<Portfolio>();
            var ids = new List<Guid>();

            var scheduleEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Portfolio>();

            scheduleEntity.Where.And().Equal(new Portfolio { user_id = user.Id, Id = sid });

            var objectEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Subject>()
                .Where.And().Equal(new Subject { portfolio_id = sid, Visible = 1 }).Entities;
            if (objectEntity.Count() == 0)
            {
                scheduleEntity.Delete();
                this.Context.Send(new UMC.Web.WebMeta().Put("type", "Subject.Portfolio.Del").Put("Id", sid), false);
                this.Prompt("移除成功");
            }
            else
            {
                this.Prompt("存在文档，不可删除");
            }


        }

    }
}