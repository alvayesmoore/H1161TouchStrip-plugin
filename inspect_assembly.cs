using System;
using System.Reflection;
using System.Linq;

class InspectAssembly {
    static void Main() {
        var path = "/home/chtrey/.nuget/packages/opentabletdriver.plugin/0.6.6.2/lib/net8.0/OpenTabletDriver.Plugin.dll";
        var assembly = Assembly.LoadFrom(path);
        
        var interfaces = assembly.GetTypes()
            .Where(t => t.IsInterface)
            .Where(t => t.Name.Contains("Pipeline") || t.Name.Contains("Filter"))
            .ToList();
            
        foreach (var iface in interfaces) {
            Console.WriteLine($"\n=== {iface.FullName} ===");
            
            var methods = iface.GetMethods();
            foreach (var m in methods) {
                var parameters = string.Join(", ", m.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                Console.WriteLine($"  {m.ReturnType.Name} {m.Name}({parameters})");
            }
            
            var props = iface.GetProperties();
            foreach (var p in props) {
                Console.WriteLine($"  property {p.PropertyType.Name} {p.Name}");
            }
        }
    }
}
