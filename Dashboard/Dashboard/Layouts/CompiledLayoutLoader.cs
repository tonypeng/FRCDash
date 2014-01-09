using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using TomShane.Neoforce.Controls;

using LayoutContract;

using Dashboard.Library;

namespace Dashboard.Layouts
{
    public class CompiledLayoutLoader : ILayoutLoader
    {
        string _path;

        public CompiledLayoutLoader(string path)
        {
            _path = path;
        }

        public void LoadLayout(Manager manager, ContentLibrary contentLibrary)
        {
            if (!File.Exists(_path))
                throw new FileNotFoundException("Compiled layout file not found.");

            Assembly assembly;

            try
            {
                assembly = Assembly.LoadFile(_path);
            }
            catch (Exception e)
            {
                return;
            }

            if (assembly == null) throw new InvalidDataException("Invalid assembly.");

            Type baseType = typeof(ILayout);

            Type[] assemblyTypes = assembly.GetTypes();

            List<Type> layoutTypes = new List<Type>();

            foreach (Type type in assemblyTypes)
            {
                if (type.IsAbstract || type.IsInterface)
                    continue;

                if (type.GetInterface(baseType.FullName) != null)
                    layoutTypes.Add(type);
            }

#warning Hardcoded value (FIXME)

            ILayout layout = (ILayout)Activator.CreateInstance(layoutTypes[0]);

            layout.SetupLayout(manager, contentLibrary);
        }
    }
}
