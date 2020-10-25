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
    class SubjectCategoryActivity : WebActivity
    {
        class SubjectCategoryDialog : Web.UIGridDialog
        {
            public Visibility? Visible { get; set; }
            public SubjectCategoryDialog()
            {
                this.Title = "图文栏位";
                this.IsAsyncData = true;
            }
            protected override Hashtable GetHeader()
            {
                var header = new Header("Id", 25);
                header.AddField("Caption", "栏位名称");
                header.AddField("Count", "图文数量");
                return header.GetHeader();


            }
            protected override Hashtable GetData(IDictionary paramsKey)
            {
                var start = UMC.Data.Utility.Parse((paramsKey["start"] ?? "0").ToString(), 0);
                var limit = UMC.Data.Utility.Parse((paramsKey["limit"] ?? "25").ToString(), 25);

                var scheduleEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Category>();
                if (Visible.HasValue)
                {
                    scheduleEntity.Where.And().Equal(new Category { Visible = Visible });
                }
                scheduleEntity.Order.Asc(new Category { Sequence = 0 });

                var list = new List<Category>();

                scheduleEntity.Query(start, limit, d => list.Add(d));
                var hash = new Hashtable();
                hash["data"] = list;
                hash["total"] = scheduleEntity.Count();
                return hash;
            }
        }
        public override void ProcessActivity(WebRequest request, WebResponse response)
        {
            var key = this.AsyncDialog("Key", g => this.DialogValue("Editer"));
            var sid = Web.UIDialog.AsyncDialog("Id", d =>
            {
                var grid = new SubjectCategoryDialog()
                {
                    IsPage = true,
                    CloseEvent = "UI.Event",
                    RefreshEvent = "Subject.Category"
                };
                if (!request.IsCashier)
                {
                    grid.Visible = Visibility.Visible;
                }
                if (request.IsMaster)
                {
                    grid.Menu("新建", request.Model, request.Command, new WebMeta("Key", "Editer", d, Guid.NewGuid().ToString()));
                }
                return grid;
            });
            var cmdId = UMC.Data.Utility.Guid(sid) ?? Guid.Empty;

            var category = new Category();

            var objectEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Category>();
            category = objectEntity.Where.And().Equal(new Category { Id = cmdId }).Entities.Single() ?? category;

            if (key == "Editer" && request.IsMaster)
            {

            }
            else
            {
                this.Context.Send(new WebMeta().UIEvent(key, new ListItem(category.Caption, category.Id.ToString())), true);
            }
            var Settings = Web.UIFormDialog.AsyncDialog("Settings", d =>
            {

                var fmdg = new Web.UIFormDialog();
                if (category.Id.HasValue == false)
                {
                    fmdg.Title = "新建栏位";
                    fmdg.AddText("栏位名称", "Caption", category.Caption);
                    fmdg.AddOption("版务人员", "user_id", (category.user_id ?? Guid.Empty).ToString(), "请选择版务人员")
                    .Put("placeholder", "请选择版务人员").Command("Settings", "SelectUser");
                    fmdg.AddNumber("显示顺序", "Sequence", category.Sequence);
                }
                else
                {
                    fmdg.Title = "编辑栏位";
                    fmdg.AddText("栏位名称", "Caption", category.Caption);
                    var value = (category.user_id ?? Guid.Empty);
                    var text = "请设置";
                    if (value != Guid.Empty)
                    {

                        var uAlias = UMC.Data.Database.Instance().ObjectEntity<User>()
                            .Where.And().Equal(new User { Id = value }).Entities.Single();
                        if (uAlias != null)
                        {
                            text = uAlias.Alias;
                        }
                    }
                    fmdg.AddOption("版务人员", "user_id", value.ToString(), text).Put("placeholder", "请选择版务人员")
                    .Command("Settings", "SelectUser");
                    fmdg.AddNumber("显示顺序", "Sequence", category.Sequence);
                    fmdg.AddRadio("可见状态", "Visible")
                    .Put("可见", Visibility.Visible.ToString(), category.Visible == Visibility.Visible)
                    .Put("隐藏", Visibility.Hidden.ToString(), category.Visible == Visibility.Hidden);
                }
                fmdg.Submit("确认提交", request, "Subject.Category");
                if (category.Id.HasValue)
                    fmdg.AddUI("专题主题设计", "去设计").Command("Design", "Page", category.Id.ToString());
                return fmdg;

            });
            UMC.Data.Reflection.SetProperty(category, Settings.GetDictionary());

            if (category.Id.HasValue == false)
            {
                category.Id = Guid.NewGuid();

                category.Count = 0;
                category.Attentions = 0;
                category.Visible = Visibility.Visible;

                objectEntity.Insert(category);
            }
            else
            {
                objectEntity.Update(category);
            }
            this.Prompt("修改成功", false);
            this.Context.Send(new UMC.Web.WebMeta().Put("type", "Subject.Category"), true);


        }

    }
}