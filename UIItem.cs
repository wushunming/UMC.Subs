using System;
using System.Collections.Generic;
using System.Text;
using UMC.Web;

namespace UMC.Subs
{

    public abstract class UIItem : UMC.Configuration.DataProvider
    {
        public abstract bool Header(UISection ui, UMC.Data.Entities.Subject sub);
        public abstract void Footer(UISection ui, UMC.Data.Entities.Subject sub);

    }
}
