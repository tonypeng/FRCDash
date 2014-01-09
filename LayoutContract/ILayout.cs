using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using TomShane.Neoforce.Controls;

using Dashboard.Library;

namespace LayoutContract
{
    public interface ILayout
    {
        void SetupLayout(Manager manager, ContentLibrary contentLibrary);
    }
}
