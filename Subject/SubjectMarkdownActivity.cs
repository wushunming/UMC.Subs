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



    class SubjectMarkdownActivity : WebActivity
    {



        public override void ProcessActivity(WebRequest request, WebResponse response)
        {
            var subEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Subject>();


            var webr = UMC.Data.WebResource.Instance();
            var value = this.AsyncDialog("Id", g => this.DialogValue("News"));

            switch (value)
            {
                case "News":
                    response.Redirect(new UMC.Data.Entities.Subject { Id = Guid.NewGuid() });
                    break;
            }
            var sid = UMC.Data.Utility.Guid(value, true);

            subEntity.Where.And().Equal(new UMC.Data.Entities.Subject { Id = sid });

            var su2bs = subEntity.Single() ?? new UMC.Data.Entities.Subject { Id = sid };

            if (String.Equals("markdown", su2bs.ContentType, StringComparison.CurrentCultureIgnoreCase))
            {
                response.Redirect(new UMC.Data.Entities.Subject { Id = sid, Content = su2bs.Content });
            }
            else
            {
                response.Redirect(new UMC.Data.Entities.Subject { Id = sid });
            }


        }

        //}
    }
}