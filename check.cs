using System;
using System.IO;
using System.Reflection;

class Program {
    static void Main() {
        try {
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string dllPath = Path.Combine(userProfile, @".nuget\packages\townofusmira\1.6.0-dev\lib\net6.0\TownOfUsMira.dll");

            var asm = Assembly.LoadFrom(dllPath);
            foreach (var type in asm.GetTypes()) {
                if (type.Name.Contains("BodyVitalsMode")) {
                    Console.WriteLine("Found: " + type.FullName);
                }
            }
        } catch (ReflectionTypeLoadException ex) {
            foreach (var type in ex.Types) {
                if (type != null && type.Name.Contains("BodyVitalsMode")) {
                    Console.WriteLine("Found in exception: " + type.FullName);
                }
            }
        }
    }
}