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
    class SubjectNavUIActivity : WebActivity
    {
        public override void ProcessActivity(WebRequest request, WebResponse response)
        {
            var teams = new List<Project>();

            var user = UMC.Security.Identity.Current;

            var sid = Utility.Guid(this.AsyncDialog("Id", d =>
            {
                this.Prompt("请输入项目");
                return this.DialogValue("none");
            })).Value;


            var Type = this.AsyncDialog("Type", "Items");
            var ui = UISection.Create();
            switch (Type)
            {
                case "Items":
                    {

                        var projects = new List<ProjectItem>();
                        var projectEntity = Utility.CMS.ObjectEntity<ProjectItem>();
                        projectEntity.Where.And().In(new ProjectItem { project_id = sid });

                        projectEntity.Order.Asc(new ProjectItem { Sequence = 0 });

                        projectEntity.Query(dr =>
                        {
                            ui.AddCell('\uf022', dr.Caption, String.Empty);
                        });// projects.Add(dr));
                    }
                    break;
                case "Portfolio":
                    {

                        var item = Utility.CMS.ObjectEntity<ProjectItem>().Where.And().In(new ProjectItem { Id = sid }).Entities.Single();
                        //     var projects = new List<Portfolio>();
                        Utility.CMS.ObjectEntity<ProjectItem>().Query(dr =>
                        {
                            ui.AddCell('\uf054', dr.Caption, String.Empty);
                        });



                    }
                    break;
                case "Subs":
                    {
                         
                        var portfolio = Utility.CMS.ObjectEntity<Portfolio>().Where.And().In(new Portfolio { Id = sid }).Entities.Single();
                        var ProjectItem = Utility.CMS.ObjectEntity<ProjectItem>().Where.And().In(new ProjectItem { Id = portfolio.project_item_id }).Entities.Single();

                        //if()
                        //     var projects = new List<Portfolio>();
                        //Utility.CMS.ObjectEntity<ProjectItem>().Query(dr =>
                        //{
                        //    ui.AddCell('\uf054', dr.Caption, String.Empty);
                        //});


                    }
                    break;
            }






        }

    }
}