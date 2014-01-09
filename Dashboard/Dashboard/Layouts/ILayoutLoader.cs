using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using TomShane.Neoforce.Controls;

using Dashboard.Library;

namespace Dashboard.Layouts
{
    public interface ILayoutLoader
    {
        void LoadLayout(Manager manager, ContentLibrary contentLibrary);
    }
}
